using System.Collections.Generic;
using IO = System.IO;

using Pngcs.Chunks;
using Pngcs.Zlib;

namespace Pngcs
{
	/// <summary> Writes a PNG image, line by line. </summary>
	public class PngWriter
	{

		/// <summary> Basic image info, inmutable </summary>
		public readonly ImageInfo ImgInfo;

		/// <summary> Filename, or description - merely informative, can be empty </summary>
		protected readonly string filename;

		FilterWriteStrategy filterStrat;

		/**
		 * Deflate algortithm compression strategy
		 */
		public EDeflateCompressStrategy CompressionStrategy { get; set; }

		/// <summary> Zip compression level (0 - 9) </summary>
		/// <remarks> Default:6 (maximum compression: 9) </remarks>
		public int CompressionLevel { get; set; }

		/// <summary> true: closes stream after ending write </summary>
		public bool ShouldCloseStream { get; set; }

		/// <summary> Maximum size of IDAT chunks </summary>
		/// <remarks> 0=use default (PngIDatChunkOutputStream 32768) </remarks>
		public int IdatMaxSize { get; set; }

		/// <summary> A high level wrapper of a ChunksList : list of written/queued chunks </summary>
		readonly PngMetadata metadata;

		/// <summary> Written/queued chunks </summary>
		readonly ChunksListForWrite chunksList;

		/// <summary> Raw current row, as array of bytes,counting from 1 (index 0 is reserved for filter type) </summary>
		protected byte[] rowb;

		/// <summary> Previuos raw row </summary>
		protected byte[] rowbprev; // rowb previous

		/// <summary> Raw current row, after filtered </summary>
		protected byte[] rowbfilter;

		/// <summary> Number of chunk group (0-6) last writen, or currently writing </summary>
		/// <remarks> See ChunksList.CHUNK_GROUP_NNN </remarks>
		public int CurrentChunkGroup { get; private set; }

		int currentRow = -1; // current line number
		readonly IO.Stream outputStream;

		PngIDatChunkOutputStream datStream;
		AZlibOutputStream datStreamDeflated;

		int[] histox = new int[256]; // auxiliar buffer, histogram, only used by reportResultsForFilter

		// this only influences the 1-2-4 bitdepth format - and if we pass a ImageLine to writeRow, this is ignored
		bool unpackedMode;
		bool needsPack; // autocomputed

		/// <summary> Constructs a PngWriter from a outputStream, with no filename information </summary>
		public PngWriter ( IO.Stream outputStream , ImageInfo imgInfo )
			: this( outputStream , imgInfo , "[NO FILENAME AVAILABLE]" )
		{

		}

		/// <summary> Constructs a PngWriter from a outputStream, with optional filename or description </summary>
		/// <remarks>
		/// After construction nothing is writen yet. You still can set some
		/// parameters (compression, filters) and queue chunks before start writing the pixels.
		/// 
		/// See also <c>FileHelper.createPngWriter()</c>
		/// </remarks>
		/// <param name="outputStream"> Opened stream for binary writing </param>
		/// <param name="imgInfo"> Basic image parameters </param>
		/// <param name="filename"> Optional, can be the filename or a description. </param>
		public PngWriter ( IO.Stream outputStream , ImageInfo imgInfo , string filename )
		{
			this.filename = filename==null ? "" : filename;
			this.outputStream = outputStream;
			this.ImgInfo = imgInfo;

			// defaults settings
			this.CompressionLevel = 6;
			this.ShouldCloseStream = true;
			this.IdatMaxSize = 0; // use default
			this.CompressionStrategy = EDeflateCompressStrategy.Filtered;

			// prealloc
			//scanline = new int[imgInfo.SamplesPerRowPacked];
			rowb = new byte[ imgInfo.BytesPerRow+1 ];
			rowbprev = new byte[ rowb.Length ];
			rowbfilter = new byte[ rowb.Length ];
			chunksList = new ChunksListForWrite( ImgInfo );
			metadata = new PngMetadata( chunksList );
			filterStrat = new FilterWriteStrategy( ImgInfo , FilterType.FILTER_DEFAULT );
			unpackedMode = false;
			needsPack = unpackedMode && imgInfo.Packed;
		}

		/// <summary> Called automatically before writing the first row </summary>
		void Initialize ()
		{
			datStream = new PngIDatChunkOutputStream( this.outputStream , this.IdatMaxSize );
			datStreamDeflated = ZlibStreamFactory.createZlibOutputStream( datStream , this.CompressionLevel , this.CompressionStrategy , true );
			WriteSignatureAndIHDR();
			WriteFirstChunks();
		}

		void ReportResultsForFilter ( int rown , FilterType type , bool tentative )
		{
			for( int i=0 ; i<histox.Length ; i++ )
				histox[i] = 0;
			int s = 0, v;
			for( int i=1 ; i<=ImgInfo.BytesPerRow ; i++ )
			{
				v = rowbfilter[i];
				s += v<0 ? -v : v;
				histox[ v & 0xFF ]++;
			}
			filterStrat.FillResultsForFilter( rown , type , s, histox , tentative );
		}

		void WriteEndChunk ()
		{
			PngChunkIEND c = new PngChunkIEND( ImgInfo );
			c.CreateRawChunk().WriteChunk( outputStream );
		}

		void WriteFirstChunks ()
		{
			int nw = 0;
			CurrentChunkGroup = ChunksList.CHUNK_GROUP_1_AFTERIDHR;
			nw = chunksList.WriteChunks(outputStream, CurrentChunkGroup);
			CurrentChunkGroup = ChunksList.CHUNK_GROUP_2_PLTE;
			nw = chunksList.WriteChunks(outputStream, CurrentChunkGroup);
			if( nw>0 && ImgInfo.Greyscale ) throw new IO.IOException("cannot write palette for this format");
			if( nw==0 && ImgInfo.Indexed ) throw new IO.IOException("missing palette");
			CurrentChunkGroup = ChunksList.CHUNK_GROUP_3_AFTERPLTE;
			nw = chunksList.WriteChunks(outputStream, CurrentChunkGroup);
			CurrentChunkGroup = ChunksList.CHUNK_GROUP_4_IDAT;
		}

		// not including end
		void WriteLastChunks ()
		{ 
			CurrentChunkGroup = ChunksList.CHUNK_GROUP_5_AFTERIDAT;
			chunksList.WriteChunks( outputStream , CurrentChunkGroup );
			// should not be unwriten chunks
			List<PngChunk> pending = chunksList.GetQueuedChunks();
			if( pending.Count>0 ) throw new IO.IOException( $"{pending.Count} chunks were not written! Eg: {pending[0]}" );
			CurrentChunkGroup = ChunksList.CHUNK_GROUP_6_END;
		}

		/// <summary> Write id signature and also "IHDR" chunk </summary>
		void WriteSignatureAndIHDR ()
		{
			CurrentChunkGroup = ChunksList.CHUNK_GROUP_0_IDHR;
			PngHelperInternal.WriteBytes( outputStream , PngHelperInternal.PNG_ID_SIGNATURE ); // signature
			PngChunkIHDR ihdr = new PngChunkIHDR( ImgInfo );
			// http://www.libpng.org/pub/png/spec/1.2/PNG-Chunks.html
			ihdr.Cols = ImgInfo.Cols;
			ihdr.Rows = ImgInfo.Rows;
			ihdr.Bitspc = ImgInfo.BitDepth;
			int colormodel = 0;
			if( ImgInfo.Alpha ) colormodel += 0x04;
			if( ImgInfo.Indexed ) colormodel += 0x01;
			if( !ImgInfo.Greyscale ) colormodel += 0x02;
			ihdr.Colormodel = colormodel;
			ihdr.Compmeth = 0; // compression method 0=deflate
			ihdr.Filmeth = 0; // filter method (0)
			ihdr.Interlaced = 0; // never interlace
			ihdr.CreateRawChunk().WriteChunk( outputStream );
		}

		protected void EncodeRowFromByte ( byte[] row )
		{
			if( row.Length==ImgInfo.SamplesPerRowPacked && !needsPack )
			{
				// some duplication of code - because this case is typical and it works faster this way
				int j = 1;
				if( ImgInfo.BitDepth<=8 )
				{
					foreach( byte x in row ) // optimized
					{
						rowb[j++] = x;
					}
				}
				else
				{
					// 16 bitspc
					foreach( byte x in row ) // optimized
					{
						rowb[j] = x;
						j += 2;
					}
				}
			}
			else
			{
				// perhaps we need to pack?
				if( row.Length>=ImgInfo.SamplesPerRow && needsPack )
				{
					ImageLine.PackInplaceByte( ImgInfo , row , row , false ); // row is packed in place!
				}
				if( ImgInfo.BitDepth<=8 )
				{
					for( int i=0, j=1 ; i<ImgInfo.SamplesPerRowPacked ; i++ )
					{
						rowb[j++] = row[i];
					}
				}
				else
				{
					// 16 bitspc
					for( int i=0, j=1 ; i<ImgInfo.SamplesPerRowPacked ; i++ )
					{
						rowb[j++] = row[i];
						rowb[j++] = 0;
					}
				}

			}
		}


		protected void EncodeRowFromInt ( int[] row )
		{
			if( row.Length==ImgInfo.SamplesPerRowPacked && !needsPack )
			{
				// some duplication of code - because this case is typical and it works faster this way
				int j = 1;
				if( ImgInfo.BitDepth<=8 )
				{
					foreach (int x in row) // optimized
					{
						rowb[j++] = (byte)x;
					}
				}
				else
				{
					// 16 bitspc
					foreach( int x in row ) // optimized
					{
						rowb[j++] = (byte)(x>>8);
						rowb[j++] = (byte)(x);
					}
				}
			}
			else
			{
				// perhaps we need to pack?
				if( row.Length>=ImgInfo.SamplesPerRow && needsPack )
				{
					ImageLine.PackInplaceInt( ImgInfo , row , row , false ); // row is packed in place!
				}
				int samplesPerRowPacked = ImgInfo.SamplesPerRowPacked;
				if( ImgInfo.BitDepth<=8 )
				{
					for( int i=0 , j=1 ; i<samplesPerRowPacked ; i++ )
					{
						rowb[ j++ ] = (byte)row[ i ];
					}
				}
				else
				{
					// 16 bitspc
					for( int i=0 , j=1 ; i<samplesPerRowPacked ; i++ )
					{
						rowb[j++] = (byte)(row[i]>>8);
						rowb[j++] = (byte)(row[i]);
					}
				}
			}
		}


		void FilterRow ( int rown )
		{
			// warning: filters operation rely on: "previos row" (rowbprev) is
			// initialized to 0 the first time
			if( filterStrat.ShouldTestAll(rown) )
			{
				FilterRowNone();
				ReportResultsForFilter( rown , FilterType.FILTER_NONE , true );
				FilterRowSub();
				ReportResultsForFilter( rown , FilterType.FILTER_SUB , true );
				FilterRowUp();
				ReportResultsForFilter( rown , FilterType.FILTER_UP , true );
				FilterRowAverage();
				ReportResultsForFilter( rown , FilterType.FILTER_AVERAGE , true );
				FilterRowPaeth();
				ReportResultsForFilter( rown , FilterType.FILTER_PAETH , true );
			}
			FilterType filterType = filterStrat.GimmeFilterType( rown , true );
			rowbfilter[0] = (byte)(int)filterType;
			switch( filterType )
			{
				case Pngcs.FilterType.FILTER_NONE:	  FilterRowNone(); break;
				case Pngcs.FilterType.FILTER_SUB:	   FilterRowSub(); break;
				case Pngcs.FilterType.FILTER_UP:		FilterRowUp(); break;
				case Pngcs.FilterType.FILTER_AVERAGE:   FilterRowAverage(); break;
				case Pngcs.FilterType.FILTER_PAETH:	 FilterRowPaeth(); break;
				default:								throw new IO.IOException($"Filter type {filterType} not implemented");
			}
			ReportResultsForFilter( rown , filterType , false );
		}

		void PrepareEncodeRow ( int rown )
		{
			if( datStream==null ) Initialize();
			currentRow++;

			#if DEBUG
			if( rown>=0 && currentRow!=rown ) throw new IO.IOException($"rows must be written in order: expected:{currentRow} passed:{rown}");
			#endif
			
			// swap
			byte[] tmp = rowb;
			rowb = rowbprev;
			rowbprev = tmp;
		}

		void FilterAndSend ( int rown )
		{
			FilterRow( rown );
			datStreamDeflated.Write( rowbfilter , 0 , ImgInfo.BytesPerRow + 1 );
		}

		void FilterRowAverage()
		{
			int i, j;
			int bytesPerRow = ImgInfo.BytesPerRow;
			int bytesPixel = ImgInfo.BytesPixel;
			for( j=1-bytesPixel , i=1 ; i<=bytesPerRow ; i++ , j++ )
			{
				rowbfilter[i] = (byte)(
					rowb[i] - ( (rowbprev[i]) + (j>0 ? rowb[j] : (byte)0) )/2
				);
			}
		}

		void FilterRowNone()
		{
			var bytesPerRow = ImgInfo.BytesPerRow;
			for( int i=1 ; i<=bytesPerRow ; i++ )
			{
				rowbfilter[i] = (byte)rowb[i];
			}
		}


		void FilterRowPaeth ()
		{
			int i, j;
			int bytesPerRow = ImgInfo.BytesPerRow;
			int bytesPixel = ImgInfo.BytesPixel;
			for( j=1-bytesPixel , i=1 ; i<=bytesPerRow ; i++ , j++ )
			{
				rowbfilter[i] = (byte)(
					rowb[i] - PngHelperInternal.FilterPaethPredictor(
						a:  j>0 ? rowb[j] : (byte)0 ,
						b:  rowbprev[i] ,
						c:  j>0 ? rowbprev[j] : (byte)0
					)
				);
			}
		}

		void FilterRowSub ()
		{
			int i, j;
			int bytesPixel = ImgInfo.BytesPixel;
			for( i=1 ; i<=bytesPixel ; i++ )
			{
				rowbfilter[i] = (byte)rowb[i];
			}
			int bytesPerRow = ImgInfo.BytesPerRow;
			for( j=1 , i=bytesPixel+1 ; i<=bytesPerRow ; i++ , j++ )
			{
				rowbfilter[i] = (byte)( rowb[i] - rowb[j] );
			}
		}

		void FilterRowUp ()
		{
			int bytesPerRow = ImgInfo.BytesPerRow;
			for( int i=1 ; i<=bytesPerRow ; i++ )
			{
				rowbfilter[ i ] = (byte)( rowb[i] - rowbprev[i] );
			}
		}

		/// <summary> Sums absolute value </summary>
		long SumRowbfilter ()
		{
			long s = 0;
			int bytesPerRow = ImgInfo.BytesPerRow;
			for( int i=1 ; i<=bytesPerRow ; i++ )
			{
				byte value = rowbfilter[i];
				s += value<0 ? -(long)value : (long)value;
			}
			return s;
		}

		/// <summary>
		/// copy chunks from reader - copy_mask : see ChunksToWrite.COPY_XXX
		/// If we are after idat, only considers those chunks after IDAT in PngReader
		/// TODO: this should be more customizable
		/// </summary>
		void CopyChunks ( PngReader reader , int copy_mask , bool onlyAfterIdat )
		{
			bool idatDone = CurrentChunkGroup>=ChunksList.CHUNK_GROUP_4_IDAT;
			if( onlyAfterIdat && reader.CurrentChunkGroup<ChunksList.CHUNK_GROUP_6_END ) throw new System.Exception("tried to copy last chunks but reader has not ended");
			foreach ( PngChunk chunk in reader.GetChunksList().GetChunks() )
			{
				int group = chunk.ChunkGroup;
				if(
					group<ChunksList.CHUNK_GROUP_4_IDAT
					&& idatDone
				)
				{
					continue;
				}
				bool copy = false;
				if( chunk.Crit )
				{
					if( chunk.Id.Equals(ChunkHelper.PLTE) )
					{
						if( ImgInfo.Indexed && ChunkHelper.MaskMatch(copy_mask, ChunkCopyBehaviour.COPY_PALETTE) )
						{
							copy = true;
						}
						if( !ImgInfo.Greyscale && ChunkHelper.MaskMatch(copy_mask, ChunkCopyBehaviour.COPY_ALL) )
						{
							copy = true;
						}
					}
				}
				else// ancillary
				{
					bool text = (chunk is PngChunkTextVar);
					bool safe = chunk.Safe;
					// notice that these if are not exclusive
					if( ChunkHelper.MaskMatch(copy_mask, ChunkCopyBehaviour.COPY_ALL) )
					{
						copy = true;
					}
					if( safe && ChunkHelper.MaskMatch(copy_mask, ChunkCopyBehaviour.COPY_ALL_SAFE) )
					{
						copy = true;
					}
					if( chunk.Id.Equals(ChunkHelper.tRNS) && ChunkHelper.MaskMatch(copy_mask, ChunkCopyBehaviour.COPY_TRANSPARENCY) )
					{
						copy = true;
					}
					if( chunk.Id.Equals(ChunkHelper.pHYs) && ChunkHelper.MaskMatch(copy_mask, ChunkCopyBehaviour.COPY_PHYS) )
					{
						copy = true;
					}
					if( text && ChunkHelper.MaskMatch( copy_mask , ChunkCopyBehaviour.COPY_TEXTUAL ) )
					{
						copy = true;
					}
					if(
						ChunkHelper.MaskMatch( copy_mask , ChunkCopyBehaviour.COPY_ALMOSTALL )
						&& !(ChunkHelper.IsUnknown(chunk) || text || chunk.Id.Equals(ChunkHelper.hIST) || chunk.Id.Equals(ChunkHelper.tIME))
					)
					{
						copy = true;
					}
					if( chunk is PngChunkSkipped )
					{
						copy = false;
					}
				}
				if( copy )
				{
					chunksList.Queue( PngChunk.CloneChunk( chunk , ImgInfo ) );
				}
			}
		}

		public void CopyChunksFirst ( PngReader reader , int copy_mask ) => CopyChunks( reader , copy_mask , false );

		public void CopyChunksLast ( PngReader reader , int copy_mask ) => CopyChunks( reader , copy_mask , true );

		/// <summary> Computes compressed size/raw size, approximate </summary>
		/// <remarks> Actually: compressed size = total size of IDAT data , raw size = uncompressed pixel bytes = rows * (bytesPerRow + 1) </remarks>
		public double ComputeCompressionRatio ()
		{
			if( CurrentChunkGroup<ChunksList.CHUNK_GROUP_6_END ) throw new System.Exception("must be called after End()");
			double compressed = (double)datStream.GetCountFlushed();
			double raw = (ImgInfo.BytesPerRow + 1) * ImgInfo.Rows;
			return compressed / raw;
		}


		/// <summary>
		/// Finalizes the image creation and closes the file stream.
		/// This MUST be called after writing the lines.
		/// </summary>  
		public void End ()
		{
			if( currentRow!=ImgInfo.Rows-1 ) throw new IO.IOException($"All rows have not been written. Last row is: {currentRow} / {ImgInfo.Rows-1}");
			datStreamDeflated.Close();
			datStream.Close();
			WriteLastChunks();
			WriteEndChunk();
			if( this.ShouldCloseStream ) outputStream.Close();
		}

		/// <summary> Filename or description, from the optional constructor argument. </summary>
		public string GetFilename() => filename;


		/// <summary> This uses the row number from the imageline! </summary>
		public void WriteRow ( ImageLine line , int rownumber )
		{
			SetUseUnPackedMode( line.SamplesUnpacked );
			if( line.SampleType==ImageLine.ESampleType.INT ) WriteRowInt( line.Scanline , rownumber );
			else WriteRowByte( line.ScanlineB , rownumber );
		}
		public void WriteRow ( int[] newrow ) => WriteRow( newrow , -1 );
		public void WriteRow ( int[] newrow , int rown ) => WriteRowInt( newrow , rown );

		/// <summary> Writes a full image row. </summary>
		/// <remarks>
		/// This must be called sequentially from n=0 to
		/// n=rows-1 One integer per sample , in the natural order: R G B R G B ... (or
		/// R G B A R G B A... if has alpha) The values should be between 0 and 255 for
		/// 8 bitspc images, and between 0- 65535 form 16 bitspc images (this applies
		/// also to the alpha channel if present) The array can be reused.
		/// </remarks>
		/// <param name="newrow"> Array of pixel values </param>
		/// <param name="rown"> Number of row, from 0 (top) to rows-1 (bottom) </param>
		public void WriteRowInt ( int[] newrow , int rown )
		{
			PrepareEncodeRow( rown );
			EncodeRowFromInt( newrow );
			FilterAndSend( rown );
		}

		public void WriteRowByte ( byte[] newrow , int rown )
		{
			PrepareEncodeRow( rown );
			EncodeRowFromByte( newrow );
			FilterAndSend( rown );
		}

		/// <summary> Writes all the pixels, calling writeRowInt() for each image row </summary>
		public void WriteRowsInt ( int[][] image )
		{
			int numRows = ImgInfo.Rows;
			for( int i=0 ; i<numRows ; i++ )
				WriteRowInt( image[i] , i );
		}

		/// <summary> Writes all the pixels, calling writeRowByte() for each image row </summary>
		public void WriteRowsByte ( byte[][] image )
		{
			int numRows = ImgInfo.Rows;
			for( int i=0 ; i<numRows ; i++ )
				WriteRowByte( image[i] , i );
		}

		public PngMetadata GetMetadata() => metadata;

		public ChunksListForWrite GetChunksList() => chunksList;

		/// <summary> Sets internal prediction filter type, or strategy to choose it. </summary>
		/// <remarks>
		/// This must be called just after constructor, before starting writing.
		/// Recommended values: DEFAULT (default) or AGGRESIVE
		/// </remarks>
		/// <param name="filterType"> One of the five prediction types or strategy to choose it </param>
		public void SetFilterType ( FilterType filterType ) => filterStrat = new FilterWriteStrategy( ImgInfo , filterType );

		public bool IsUnpackedMode => unpackedMode;

		public void SetUseUnPackedMode ( bool useUnpackedMode )
		{
			this.unpackedMode = useUnpackedMode;
			needsPack = unpackedMode && ImgInfo.Packed;
		}

	}
}
