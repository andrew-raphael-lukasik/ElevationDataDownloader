using System.Collections.Generic;
using IO = System.IO;
using Pngcs.Chunks;

namespace Pngcs
{
	/// <summary> Reads a PNG image, line by line </summary>
	/// <remarks>
	/// The typical reading sequence is as follows:
	/// 
	/// 1. At construction time, the header and IHDR chunk are read (basic image info)
	/// 
	/// 2  (Optional) you can set some global options: UnpackedMode CrcCheckDisabled
	/// 
	/// 3. (Optional) If you call GetMetadata() or or GetChunksLisk() before reading the pixels, the chunks before IDAT are automatically loaded and available
	/// 
	/// 4a. The rows are read, one by one, with the <tt>ReadRowXXX</tt> methods: (ReadRowInt() , ReadRowByte(), etc)
	/// in order, from 0 to imageRows-1 (you can skip or repeat rows, but not go backwards)
	/// 
	/// 4b. Alternatively, you can read all rows, or a subset, in a single call: see ReadRowsInt(), ReadRowsByte()
	/// In general this consumes more memory, but for interlaced images this is equally efficient, and more so if reading a small subset of rows.
	///
	/// 5. Read of the last row automatically loads the trailing chunks, and ends the reader.
	/// 
	/// 6. End() forcibly finishes/aborts the reading and closes the stream
	/// </remarks>
	public class PngReader
		: System.IDisposable
	{
		
		/// <summary>
		/// Basic image info, inmutable
		/// </summary>
		public ImageInfo ImgInfo {get;private set;}

		/// <summary>
		/// filename, or description - merely informative, can be empty
		/// </summary>
		protected readonly string filename;

		/// <summary>
		/// Strategy for chunk loading. Default: LOAD_CHUNK_ALWAYS
		/// </summary>
		public ChunkLoadBehaviour ChunkLoadBehaviour { get; set; }

		/// <summary>
		/// Should close the underlying Input Stream when ends?
		/// </summary>
		public bool ShouldCloseStream { get; set; }

		/// <summary>
		/// Maximum amount of bytes from ancillary chunks to load in memory 
		/// </summary>
		/// <remarks>
		///  Default: 5MB. 0: unlimited. If exceeded, chunks will be skipped
		/// </remarks>
		public long MaxBytesMetadata { get; set; }

		/// <summary>
		/// Maximum total bytes to read from stream 
		/// </summary>
		/// <remarks>
		///  Default: 200MB. 0: Unlimited. If exceeded, an exception will be thrown
		/// </remarks>
		public long MaxTotalBytesRead { get; set; }


		/// <summary>
		/// Maximum ancillary chunk size
		/// </summary>
		/// <remarks>
		///  Default: 2MB, 0: unlimited. Chunks exceeding this size will be skipped (nor even CRC checked)
		/// </remarks>
		public int SkipChunkMaxSize { get; set; }

		/// <summary>
		/// Ancillary chunks to skip
		/// </summary>
		/// <remarks>
		///  Default: { "fdAT" }. chunks with these ids will be skipped (nor even CRC checked)
		/// </remarks>
		public string[] SkipChunkIds { get; set; }

		Dictionary<string, int> skipChunkIdsSet = null; // lazily created

		/// <summary>
		/// A high level wrapper of a ChunksList : list of read chunks
		/// </summary>
		readonly PngMetadata metadata;
		/// <summary>
		/// Read chunks
		/// </summary>
		readonly ChunksList chunksList;

		/// <summary>
		/// buffer: last read line
		/// </summary>
		protected ImageLine imgLine;

		/// <summary>
		/// raw current row, as array of bytes,counting from 1 (index 0 is reserved for filter type)
		/// </summary>
		protected byte[] rowb;
		/// <summary>
		/// previuos raw row
		/// </summary>
		protected byte[] rowbprev; // rowb previous
		/// <summary>
		/// raw current row, after unfiltered
		/// </summary>
		protected byte[] rowbfilter;


		// only set for interlaced PNG
		public readonly bool interlaced;
		readonly PngDeinterlacer deinterlacer;

		bool crcEnabled = true;

		// this only influences the 1-2-4 bitdepth format
		bool unpackedMode = false;

		/// <summary> number of chunk group (0-6) last read, or currently reading </summary>
		/// <remarks>see ChunksList.CHUNK_GROUP_NNN</remarks>
		public int CurrentChunkGroup { get; private set; }
		
		/// <summary> Last read row number </summary>
		protected int currentImageRow = -1;

		long offset = 0;  // offset in InputStream = bytes read
		int bytesChunksLoaded = 0; // bytes loaded from anciallary chunks

		readonly IO.Stream inputStream;
		internal Zlib.AZlibInputStream idatIstream;
		internal PngIDatChunkInputStream iIdatCstream;

		protected Zlib.Adler32 crctest; // If set to non null, it gets a CRC of the unfiltered bytes, to check for images equality

		/// <summary>
		/// Constructs a PngReader from a Stream, with no filename information
		/// </summary>
		/// <param name="inputStream"></param>
		public PngReader ( IO.Stream inputStream )
			: this( inputStream , "[NO FILENAME AVAILABLE]" )
		{

		}

		/// <summary>
		/// Constructs a PNGReader objet from a opened Stream
		/// </summary>
		/// <remarks>The constructor reads the signature and first chunk (IDHR)<seealso cref="FileHelper.CreatePngReader(string)"/>
		/// </remarks>
		/// <param name="filename">Optional, can be the filename or a description.</param>
		public PngReader ( IO.Stream inputStream , string filename )
		{
			this.filename = filename!=null ? filename : string.Empty;
			this.inputStream = inputStream;
			this.chunksList = new ChunksList(null);
			this.metadata = new PngMetadata(chunksList);
			this.offset = 0;
			
			// set default options
			this.CurrentChunkGroup = -1;
			this.ShouldCloseStream = true;
			this.MaxBytesMetadata = 5 * 1024 * 1024;
			this.MaxTotalBytesRead = 200 * 1024 * 1024; // 200MB
			this.SkipChunkMaxSize = 2 * 1024 * 1024;
			this.SkipChunkIds = new string[]{ "fdAT" };
			this.ChunkLoadBehaviour = Pngcs.Chunks.ChunkLoadBehaviour.LOAD_CHUNK_ALWAYS;
			
			// starts reading: signature
			byte[] pngid = new byte[8];
			PngHelperInternal.ReadBytes( inputStream , pngid , 0 , pngid.Length );
			offset += pngid.Length;
			if( !PngCsUtils.UnSafeEquals(pngid,PngHelperInternal.PNG_ID_SIGNATURE) ) throw new IO.IOException("Bad PNG signature");
			CurrentChunkGroup = ChunksList.CHUNK_GROUP_0_IDHR;
			
			// reads first chunk IDHR
			int clen = PngHelperInternal.ReadInt4( inputStream );
			offset += 4;
			if( clen!=13 ) throw new System.Exception($"IDHR chunk len!=13 ?? Is:{clen}");
			byte[] chunkid = new byte[4];
			PngHelperInternal.ReadBytes(inputStream, chunkid, 0, 4);
			if( !PngCsUtils.UnSafeEquals(chunkid, ChunkHelper.b_IHDR) ) throw new IO.IOException($"IHDR not found as first chunk??? [{ChunkHelper.ToString(chunkid)}]");
			offset += 4;
			PngChunkIHDR ihdr = (PngChunkIHDR)ReadChunk(chunkid, clen, false);
			bool alpha = (ihdr.Colormodel & 0x04)!=0;
			bool palette = (ihdr.Colormodel & 0x01)!=0;
			bool grayscale = (ihdr.Colormodel==0 || ihdr.Colormodel==4);
			
			// creates ImgInfo and imgLine, and allocates buffers
			ImgInfo = new ImageInfo(ihdr.Cols, ihdr.Rows, ihdr.Bitspc, alpha, grayscale, palette);
			rowb = new byte[ImgInfo.BytesPerRow + 1];
			rowbprev = new byte[rowb.Length];
			rowbfilter = new byte[rowb.Length];
			interlaced = ihdr.Interlaced==1;
			deinterlacer = interlaced ? new PngDeinterlacer(ImgInfo) : null;
			
			// some checks
			if( ihdr.Filmeth!=0 || ihdr.Compmeth!=0 || (ihdr.Interlaced & 0xFFFE)!=0 ) throw new IO.IOException("compmethod or filtermethod or interlaced unrecognized");
			if( ihdr.Colormodel<0 || ihdr.Colormodel>6 || ihdr.Colormodel==1 || ihdr.Colormodel==5 ) throw new IO.IOException($"Invalid colormodel {ihdr.Colormodel}");
			if( ihdr.Bitspc!=1 && ihdr.Bitspc!=2 && ihdr.Bitspc!=4 && ihdr.Bitspc!=8 && ihdr.Bitspc!=16 ) throw new IO.IOException($"Invalid bit depth {ihdr.Bitspc}");
		}


		bool FirstChunksNotYetRead () => CurrentChunkGroup<ChunksList.CHUNK_GROUP_1_AFTERIDHR;


		/// <summary>
		/// Internally called after having read the last line. 
		/// It reads extra chunks after IDAT, if present.
		/// </summary>
		void ReadLastAndClose ()
		{
			if( CurrentChunkGroup<ChunksList.CHUNK_GROUP_5_AFTERIDAT )
			{
				if( idatIstream!=null ) idatIstream.Close();
				ReadLastChunks();
			}
			Close();
		}

		void Close ()
		{
			if( CurrentChunkGroup<ChunksList.CHUNK_GROUP_6_END )
			{
				// this could only happen if forced close
				if( idatIstream!=null ) idatIstream.Close();
				CurrentChunkGroup = ChunksList.CHUNK_GROUP_6_END;
			}
			if( ShouldCloseStream )
			{
				inputStream.Close();
			}
		}

		void UnfilterRow ( int nbytes )
		{
			int ftn = rowbfilter[0];
			FilterType filterType = (FilterType)ftn;
			switch( filterType )
			{
				case Pngcs.FilterType.FILTER_NONE:	  UnfilterRowNone(nbytes);		break;
				case Pngcs.FilterType.FILTER_SUB:	   UnfilterRowSub(nbytes);		 break;
				case Pngcs.FilterType.FILTER_UP:		UnfilterRowUp(nbytes);		  break;
				case Pngcs.FilterType.FILTER_AVERAGE:   UnfilterRowAverage(nbytes);	 break;
				case Pngcs.FilterType.FILTER_PAETH:	 UnfilterRowPaeth(nbytes);	   break;
				default:								throw new IO.IOException($"Filter type {ftn} not implemented");
			}
			if( crctest!=null ) crctest.Update( rowb , 1 , nbytes );
		}


		void UnfilterRowAverage ( int nbytes )
		{
			int bpp = ImgInfo.BytesPixel;
			int i, j, x;
			for( j=1-bpp , i=1 ; i<=nbytes ; i++ , j++ )
			{
				x = (j>0) ? rowb[j] : (byte)0;
				rowb[i] = (byte)( rowbfilter[i] + (x + (rowbprev[i] & 0xFF)) / 2 );
			}
		}

		void UnfilterRowNone ( int nbytes )
		{
			for( int i=1 ; i<=nbytes ; i++ )
				rowb[i] = (byte)( rowbfilter[i] );
		}

		void UnfilterRowPaeth ( int nbytes )
		{
			int bpp = ImgInfo.BytesPixel;
			int i, j, x, y;
			for( j=1-bpp , i=1 ; i<=nbytes ; i++ , j++ )
			{
				x = j>0 ? rowb[j] : (byte)0;
				y = j>0 ? rowbprev[j] : (byte)0;
				rowb[i] = (byte)( rowbfilter[i] + PngHelperInternal.FilterPaethPredictor( x , rowbprev[i] , y ) );
			}
		}

		void UnfilterRowSub ( int nbytes )
		{
			int bpp = ImgInfo.BytesPixel;
			int i, j;
			for( i=1 ; i<=bpp ; i++ )
				rowb[i] = (byte)rowbfilter[i];
			for( j=1 , i=bpp+1 ; i<=nbytes ; i++ , j++ )
				rowb[i] = (byte)( rowbfilter[i] + rowb[j] );
		}

		void UnfilterRowUp ( int nbytes )
		{
			for( int i=1 ; i<=nbytes ; i++ )
				rowb[i] = (byte)( rowbfilter[i]+rowbprev[i] );
		}

		/// <summary>
		/// Reads chunks before first IDAT. Position before: after IDHR (crc included)
		/// Position after: just after the first IDAT chunk id Returns length of first
		/// IDAT chunk , -1 if not found
		/// </summary>
		void ReadFirstChunks ()
		{
			if( !FirstChunksNotYetRead() ) return;
			int clen = 0;
			bool found = false;
			byte[] chunkid = new byte[4]; // it's important to reallocate in each
			this.CurrentChunkGroup = ChunksList.CHUNK_GROUP_1_AFTERIDHR;
			while( !found )
			{
				clen = PngHelperInternal.ReadInt4(inputStream);
				offset += 4;
				if( clen<0 ) break;
				PngHelperInternal.ReadBytes( inputStream , chunkid , 0 , 4 );
				offset += 4;
				if( PngCsUtils.UnSafeEquals(chunkid, Pngcs.Chunks.ChunkHelper.b_IDAT) )
				{
					found = true;
					this.CurrentChunkGroup = ChunksList.CHUNK_GROUP_4_IDAT;
					// add dummy idat chunk to list
					chunksList.AppendReadChunk(new PngChunkIDAT(ImgInfo, clen, offset - 8), CurrentChunkGroup);
					break;
				}
				else if( PngCsUtils.UnSafeEquals(chunkid, Pngcs.Chunks.ChunkHelper.b_IEND) ) throw new IO.IOException($"END chunk found before image data (IDAT) at offset={offset}");
				string chunkids = ChunkHelper.ToString(chunkid);
				if( chunkids.Equals(ChunkHelper.PLTE) )
				{
					this.CurrentChunkGroup = ChunksList.CHUNK_GROUP_2_PLTE;
				}
				ReadChunk( chunkid , clen , false );
				if( chunkids.Equals(ChunkHelper.PLTE) )
				{
					this.CurrentChunkGroup = ChunksList.CHUNK_GROUP_3_AFTERPLTE;
				}
			}
			int idatLen = found ? clen : -1;
			if( idatLen<0 ) throw new IO.IOException("first idat chunk not found!");
			iIdatCstream = new PngIDatChunkInputStream(inputStream, idatLen, offset);
			idatIstream = Zlib.ZlibStreamFactory.createZlibInputStream(iIdatCstream, true);
			if( !crcEnabled )
			{
				iIdatCstream.DisableCrcCheck();
			}
		}

		/// <summary>
		/// Reads (and processes ... up to a point) chunks after last IDAT.
		/// </summary>
		void ReadLastChunks ()
		{
			CurrentChunkGroup = ChunksList.CHUNK_GROUP_5_AFTERIDAT;
			// PngHelper.logdebug("idat ended? " + iIdatCstream.isEnded());
			if( !iIdatCstream.IsEnded() )
			{
				iIdatCstream.ForceChunkEnd();
			}
			int clen = iIdatCstream.GetLenLastChunk();
			byte[] chunkid = iIdatCstream.GetIdLastChunk();
			bool endfound = false;
			bool first = true;
			bool skip = false;
			while( !endfound )
			{
				skip = false;
				if( !first )
				{
					clen = PngHelperInternal.ReadInt4(inputStream);
					offset += 4;
					if( clen<0 ) throw new IO.IOException($"bad len {clen}");
					PngHelperInternal.ReadBytes(inputStream, chunkid, 0, 4);
					offset += 4;
				}
				first = false;
				if( PngCsUtils.UnSafeEquals( chunkid , ChunkHelper.b_IDAT ) )
				{
					skip = true; // extra dummy (empty?) idat chunk, it can happen, ignore it
				}
				else if( PngCsUtils.UnSafeEquals( chunkid , ChunkHelper.b_IEND ) )
				{
					CurrentChunkGroup = ChunksList.CHUNK_GROUP_6_END;
					endfound = true;
				}
				ReadChunk( chunkid , clen , skip );
			}
			if( !endfound ) throw new IO.IOException($"end chunk not found - offset={offset}");
			// PngHelper.logdebug("end chunk found ok offset=" + offset);
		}

		/// <summary>
		/// Reads chunkd from input stream, adds to ChunksList, and returns it.
		/// If it's skipped, a PngChunkSkipped object is created
		/// </summary>
		PngChunk ReadChunk ( byte[] chunkid , int clen , bool skipforced )
		{
			if( clen<0) throw new IO.IOException($"invalid chunk lenght: {clen}");
			// skipChunksByIdSet is created lazyly, if fist IHDR has already been read
			if( skipChunkIdsSet==null && CurrentChunkGroup>ChunksList.CHUNK_GROUP_0_IDHR )
			{
				skipChunkIdsSet = new Dictionary<string,int>();
				if( SkipChunkIds!=null )
				{
					foreach( string id in SkipChunkIds )
					{
						skipChunkIdsSet.Add( id , 1 );
					}
				}
			}

			string chunkidstr = ChunkHelper.ToString(chunkid);
			PngChunk pngChunk = null;
			bool critical = ChunkHelper.IsCritical(chunkidstr);
			bool skip = skipforced;
			if( MaxTotalBytesRead>0 && clen+offset>MaxTotalBytesRead ) throw new IO.IOException($"Maximum total bytes to read exceeeded: {MaxTotalBytesRead} offset:{offset} clen={clen}");
			// an ancillary chunks can be skipped because of several reasons:
			if( CurrentChunkGroup>ChunksList.CHUNK_GROUP_0_IDHR && !ChunkHelper.IsCritical(chunkidstr) )
			{
				skip = skip
					|| (SkipChunkMaxSize>0 && clen>=SkipChunkMaxSize)
					|| skipChunkIdsSet.ContainsKey(chunkidstr)
					|| ( MaxBytesMetadata>0 && clen>MaxBytesMetadata-bytesChunksLoaded )
					|| !ChunkHelper.ShouldLoad(chunkidstr,ChunkLoadBehaviour);
			}

			if( skip)
			{
				PngHelperInternal.SkipBytes(inputStream, clen);
				PngHelperInternal.ReadInt4(inputStream); // skip - we dont call PngHelperInternal.skipBytes(inputStream, clen + 4) for risk of overflow 
				pngChunk = new PngChunkSkipped(chunkidstr, ImgInfo, clen);
			}
			else
			{
				ChunkRaw chunk = new ChunkRaw( clen , chunkid , true );
				chunk.ReadChunkData( inputStream , crcEnabled || critical );
				pngChunk = PngChunk.Factory( chunk , ImgInfo );
				if( !pngChunk.Crit )
				{
					bytesChunksLoaded += chunk.Len;
				}
			}
			pngChunk.Offset = offset - 8L;
			chunksList.AppendReadChunk( pngChunk , CurrentChunkGroup );
			offset += clen + 4L;
			return pngChunk;
		}

		/// <summary> Logs/prints a warning. </summary>
		/// <remarks>
		/// The default behaviour is print to stderr, but it can be overriden.
		/// This happens rarely - most errors are fatal.
		/// </remarks>
		internal void logWarn ( string warn ) => System.Console.Error.WriteLine( warn );

		/// <summary> Returns the ancillary chunks available </summary>
		/// <remarks> If the rows have not yet still been read, this includes only the chunks placed before the pixels (IDAT) </remarks>
		/// <returns> ChunksList </returns>
		public ChunksList GetChunksList ()
		{
			if( FirstChunksNotYetRead() )
				ReadFirstChunks();
			return chunksList;
		}

		/// <summary> Returns the ancillary chunks available </summary>
		/// <remarks> See GetChunksList </remarks>
		/// <returns> PngMetadata </returns>
		public PngMetadata GetMetadata ()
		{
			if( FirstChunksNotYetRead() )
			{
				ReadFirstChunks();
			}
			return metadata;
		}

		/// <summary> Reads the row using ImageLine as buffer </summary>
		///<param name="imageRow"> Row number - just as a check </param>
		/// <returns> ImageLine that also is available inside this object </returns>
		public ImageLine ReadRow ( int imageRow ) => imgLine==null || imgLine.SampleType!=ImageLine.ESampleType.BYTE ? ReadRowInt(imageRow) : ReadRowByte(imageRow);

		public ImageLine ReadRowInt ( int imageRow )
		{
			imgLine = new ImageLine(
				ImgInfo ,
				ImageLine.ESampleType.INT ,
				unpackedMode ,
				null ,
				null ,
				imageRow
			);
			ReadRowInt( imgLine.Scanline , imageRow );
			imgLine.FilterUsed = (FilterType)rowbfilter[0];
			return imgLine;
		}

		public ImageLine ReadRowByte ( int imageRow )
		{
			imgLine = new ImageLine(
				ImgInfo ,
				ImageLine.ESampleType.BYTE ,
				unpackedMode ,
				null ,
				null ,
				imageRow
			);
			ReadRowByte( imgLine.ScanlineB , imageRow );
			imgLine.FilterUsed = (FilterType)rowbfilter[0];
			return imgLine;
		}

		public int[] ReadRow ( int[] buffer , int imageRow ) => ReadRowInt( buffer , imageRow );

		public int[] ReadRowInt ( int[] buffer , int imageRow )
		{
			if( buffer==null )
			{
				buffer = new int[ unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked ];
			}
			if( interlaced==false )
			{
				if( imageRow<=currentImageRow ) throw new IO.IOException($"rows must be read in increasing order: {imageRow}");
				int bytesread = 0;
				while( currentImageRow<imageRow )
				{
					bytesread = ReadRowRaw( currentImageRow + 1 );// read rows, perhaps skipping if necessary
				}
				decodeLastReadRowToInt( buffer , bytesread );
			}
			else// interlaced
			{
				if( deinterlacer.getImageInt()==null )
				{
					deinterlacer.setImageInt( ReadRowsInt().Scanlines );// read all image and store it in deinterlacer
				}
				System.Array.Copy(
					deinterlacer.getImageInt()[ imageRow ] ,
					0 ,
					buffer ,
					0 ,
					unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked
				);
			}
			return buffer;
		}

		public byte[] ReadRowByte ( byte[] buffer , int imageRow )
		{
			if( buffer==null )
			{
				buffer = new byte[ unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked ];
			}
			if( interlaced==false )
			{
				if( imageRow<=currentImageRow) throw new IO.IOException( $"rows must be read in increasing order: {imageRow}" );
				int bytesread = 0;
				while( currentImageRow<imageRow )
				{
					bytesread = ReadRowRaw( currentImageRow+1 ); // read rows, perhaps skipping if necessary
				}
				decodeLastReadRowToByte( buffer , bytesread );
			}
			else// interlaced
			{
				if( deinterlacer.getImageByte()==null )
				{
					deinterlacer.setImageByte( ReadRowsByte().ScanlinesB );// read all image and store it in deinterlacer
				}
				System.Array.Copy(
					deinterlacer.getImageByte()[ imageRow ] ,
					0 ,
					buffer ,
					0 ,
					unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked
				);
			}
			return buffer;
		}

		void decodeLastReadRowToInt ( int[] buffer , int bytesRead )// see http://www.libpng.org/pub/png/spec/1.2/PNG-DataRep.html
		{
			if( ImgInfo.BitDepth<=8 )
			{
				for( int i=0 , j=1 ; i<bytesRead ; i++ )
				{
					buffer[i] = (rowb[j++]);
				}
			}
			else
			{ // 16 bitspc
				for( int i=0 , j=1 ; j<bytesRead ; i++ )
				{
					buffer[i] = ( rowb[j++]<<8 ) + rowb[j++];
				}
			}
			if( ImgInfo.Packed && unpackedMode )
			{
				ImageLine.UnpackInplaceInt( ImgInfo , buffer , buffer , false );
			}
		}

		void decodeLastReadRowToByte ( byte[] buffer , int bytesRead )// see http://www.libpng.org/pub/png/spec/1.2/PNG-DataRep.html
		{
			if( ImgInfo.BitDepth<=8 )
			{
				System.Array.Copy( rowb , 1 , buffer , 0 , bytesRead );
			}
			else// 16 bitspc
			{
				for( int i=0 , j=1 ; j<bytesRead ; i++ , j+=2 )
				{
					buffer[ i ] = rowb[ j ]; // 16 bits in 1 byte: this discards the LSB!!!
				}
			}
			if( ImgInfo.Packed && unpackedMode )
			{
				ImageLine.UnpackInplaceByte( ImgInfo , buffer , buffer , false );
			}
		}


		public ImageLines ReadRowsInt ( int rowOffset , int imageRows , int rowStep )
		{
			if( imageRows<0 )
			{
				imageRows = (ImgInfo.Rows - rowOffset) / rowStep;
			}
			if( rowStep<1 || rowOffset<0 || imageRows*rowStep+rowOffset>ImgInfo.Rows ) throw new IO.IOException("bad args");
			ImageLines imlines = new ImageLines( ImgInfo , ImageLine.ESampleType.INT , unpackedMode , rowOffset , imageRows , rowStep );
			if( !interlaced )
			{
				for( int j=0; j<ImgInfo.Rows ; j++ )
				{
					int bytesread = ReadRowRaw(j); // read and perhaps discards
					int matrixRow = imlines.ImageRowToMatrixRowStrict(j);
					if( matrixRow>=0 )
					{
						decodeLastReadRowToInt( imlines.Scanlines[matrixRow] , bytesread );
					}
				}
			}
			else
			{ // and now, for something completely different (interlaced)
				int[] buf = new int[unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked];
				for( int p=1 ; p<=7 ; p++ )
				{
					deinterlacer.setPass(p);
					for( int i=0 ; i<deinterlacer.getRows() ; i++ )
					{
						int bytesread = ReadRowRaw(i);
						int j = deinterlacer.getCurrRowReal();
						int matrixRow = imlines.ImageRowToMatrixRowStrict(j);
						if( matrixRow>=0 )
						{
							decodeLastReadRowToInt( buf , bytesread );
							deinterlacer.deinterlaceInt( buf , imlines.Scanlines[matrixRow] , !unpackedMode );
						}
					}
				}
			}
			Dispose();
			return imlines;
		}

		public ImageLines ReadRowsInt () => ReadRowsInt( 0 , ImgInfo.Rows , 1 );

		public ImageLines ReadRowsByte ( int rowOffset , int imageRows , int rowStep )
		{
			if( imageRows<0 )
			{
				imageRows = (ImgInfo.Rows - rowOffset) / rowStep;
			}
			if( rowStep<1 || rowOffset<0 || imageRows*rowStep+rowOffset>ImgInfo.Rows ) throw new IO.IOException("bad args");
			ImageLines imlines = new ImageLines( ImgInfo , ImageLine.ESampleType.BYTE , unpackedMode , rowOffset , imageRows , rowStep );
			if( !interlaced )
			{
				for( int j=0 ; j<ImgInfo.Rows ; j++ )
				{
					int bytesread = ReadRowRaw(j); // read and perhaps discards
					int matrixRow = imlines.ImageRowToMatrixRowStrict(j);
					if( matrixRow>=0 ) decodeLastReadRowToByte( imlines.ScanlinesB[matrixRow] , bytesread );
				}
			}
			else
			{
				// and now, for something completely different (interlaced)
				byte[] buf = new byte[ unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked ];
				for( int p=1 ; p<=7 ; p++ )
				{
					deinterlacer.setPass( p );
					for( int i=0 ; i<deinterlacer.getRows() ; i++ )
					{
						int bytesread = ReadRowRaw(i);
						int j = deinterlacer.getCurrRowReal();
						int matrixRow = imlines.ImageRowToMatrixRowStrict(j);
						if( matrixRow>=0 )
						{
							decodeLastReadRowToByte( buf , bytesread );
							deinterlacer.deinterlaceByte( buf , imlines.ScanlinesB[matrixRow] , !unpackedMode );
						}
					}
				}
			}
			Dispose();
			return imlines;
		}

		public ImageLines ReadRowsByte () => ReadRowsByte( 0 , ImgInfo.Rows , 1 );

		int ReadRowRaw ( int imageRow )
		{
			if( imageRow==0 && FirstChunksNotYetRead() )
			{
				ReadFirstChunks();
			}
			if( imageRow==0 && interlaced )
			{
				System.Array.Clear( rowb , 0 , rowb.Length ); // new subimage: reset filters: this is enough, see the swap that happens lines
			}
			// below
			int bytesRead = ImgInfo.BytesPerRow; // NOT including the filter byte
			if( interlaced )
			{
				if( imageRow<0 || imageRow>deinterlacer.getRows() || (imageRow!=0 && imageRow!=deinterlacer.getCurrRowSubimg() + 1)) throw new IO.IOException($"invalid row in interlaced mode: {imageRow}");
				deinterlacer.setRow( imageRow );
				bytesRead = ( ImgInfo.BitspPixel * deinterlacer.getPixelsToRead() + 7 ) / 8;
				if( bytesRead<1 ) throw new System.Exception("what's going on??");
			}
			else
			{
				// check for non interlaced
				if( imageRow<0 || imageRow>=ImgInfo.Rows || imageRow!=currentImageRow+1 ) throw new IO.IOException( $"invalid row: {imageRow}" );
			}
			currentImageRow = imageRow;

			// swap buffers
			byte[] tmp = rowb;
			rowb = rowbprev;
			rowbprev = tmp;

			// loads in rowbfilter "raw" bytes, with filter
			PngHelperInternal.ReadBytes(idatIstream, rowbfilter, 0, bytesRead + 1);
			offset = iIdatCstream.GetOffset();
			if( offset<0 ) throw new System.Exception($"bad offset ?? {offset}");
			if( MaxTotalBytesRead>0 && offset>=MaxTotalBytesRead ) throw new IO.IOException($"Reading IDAT: Maximum total bytes to read exceeeded: {MaxTotalBytesRead} offset:{offset}");
			rowb[0] = 0;
			UnfilterRow(bytesRead);
			rowb[0] = rowbfilter[0];
			if( (currentImageRow==ImgInfo.Rows-1 && interlaced==false) || (interlaced && deinterlacer.isAtLastRow()) )
			{
				ReadLastAndClose();
			}
			return bytesRead;
		}

		public void ReadSkippingAllRows ()
		{
			if( FirstChunksNotYetRead() )
			{
				ReadFirstChunks();
			}
			// we read directly from the compressed stream, we dont decompress nor chec CRC
			iIdatCstream.DisableCrcCheck();
			int r;
			do { r = iIdatCstream.Read( rowbfilter , 0 , rowbfilter.Length ); }
			while( r>=0 );
			offset = iIdatCstream.GetOffset();
			if( offset<0 ) throw new System.Exception($"bad offset ?? {offset}");
			if( MaxTotalBytesRead>0 && offset>=MaxTotalBytesRead ) throw new IO.IOException($"Reading IDAT: Maximum total bytes to read exceeeded: {MaxTotalBytesRead} offset:{offset}");
			ReadLastAndClose();
		}


		// basic info
		public override string ToString () => $"filename={filename} {ImgInfo}";

		/// <summary> Normally this does nothing, but it can be used to force a premature closing </summary>
		public void Dispose ()
		{
			if( CurrentChunkGroup<ChunksList.CHUNK_GROUP_6_END ) Close();
		}

		public bool IsInterlaced () => interlaced;

		public void SetUnpackedMode ( bool unPackedMode ) => this.unpackedMode = unPackedMode;

		/**
		 * @see PngReader#setUnpackedMode(boolean)
		 */
		public bool IsUnpackedMode () => unpackedMode;

		public void SetCrcCheckDisabled () => crcEnabled = false;

		internal long GetCrctestVal () => crctest.GetValue();

		internal void InitCrctest () => this.crctest = new Zlib.Adler32();

		void System.IDisposable.Dispose () => this.Dispose();

	}
}
