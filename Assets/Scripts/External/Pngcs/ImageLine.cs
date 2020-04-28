namespace Pngcs
{
	/// <summary> Lightweight wrapper for an image scanline, for read and write </summary>
	/// <remarks> It can be (usually it is) reused while iterating over the image lines. See <c>scanline</c> field doc, to understand the format. </remarks>
	public class ImageLine
	{
		
		/// <summary> ImageInfo (readonly inmutable) </summary>
		public ImageInfo ImgInfo => _imgInfo;
		readonly ImageInfo _imgInfo;

		/// <summary> Samples of an image line </summary>
		/// <remarks>
		/// The 'scanline' is an array of integers, corresponds to an image line (row)
		/// Except for 'packed' formats (gray/indexed with 1-2-4 bitdepth) each int is a
		/// "sample" (one for channel), (0-255 or 0-65535) in the respective PNG sequence
		/// sequence : (R G B R G B...) or (R G B A R G B A...) or (g g g ...) or ( i i i
		/// ) (palette index)
		/// 
		/// For bitdepth 1/2/4 ,and if samplesUnpacked=false, each value is a PACKED byte! To get an unpacked copy,
		/// see <c>Pack()</c> and its inverse <c>Unpack()</c>
		/// 
		/// To convert a indexed line to RGB balues, see ImageLineHelper.PalIdx2RGB()
		/// (cant do the reverse)
		/// </remarks>
		public int[] Scanline => _scanline;
		readonly int[] _scanline;

		/// <summary> Same as Scanline, but with one byte per sample. Only one of Scanline and ScanlineB is valid - this depends on SampleType}
		/// </summary>
		public byte[] ScanlineB => _scanlineB;
		readonly byte[] _scanlineB;

		/// <summary> tracks the current row number (from 0 to rows-1) </summary>
		public int ImageRow => _imageRow;
		readonly int _imageRow;

		internal readonly int channels; // copied from imgInfo, more handy
		internal readonly int bitDepth; // copied from imgInfo, more handy

		/// <summary> Hown many elements has the scanline array =imgInfo.samplePerRowPacked, if packed, imgInfo.samplePerRow elsewhere </summary>
		public int ElementsPerRow => _elementsPerRow;
		readonly int _elementsPerRow;

		/// <summary> Maximum sample value that this line admits: typically 255; less if bitdepth less than 8, 65535 if 16bits </summary>
		public int MaxSampleVal => _maxSampleVal;
		readonly int _maxSampleVal;

		public enum ESampleType
		{
			INT, // 4 bytes per sample
			BYTE // 1 byte per sample
		}

		/// <summary> Determines if samples are stored in integers or in bytes </summary>
		public ESampleType SampleType { get; private set; }

		/// <summary>
		/// True: each scanline element is a sample.
		/// False: each scanline element has severals samples packed in a byte
		/// </summary>
		public bool SamplesUnpacked { get; private set; }

		/// <summary> informational only ; filled by the reader </summary>
		public FilterType FilterUsed { get; set; }

		/// <summary> Constructs an ImageLine </summary>
		/// <param name="imgInfo">Inmutable copy of PNG ImageInfo</param>
		/// <param name="sampleType">Storage for samples:INT (default) or BYTE</param>
		/// <param name="unpackedMode">If true and bitdepth less than 8, samples are unpacked. This has no effect if biddepth 8 or 16</param>
		public ImageLine ( ImageInfo imgInfo , ESampleType sampleType , bool unpackedMode , int[] scanLineInt , byte[] scanLineBytes , int imageRow )
		{
			this._imgInfo = imgInfo;
			channels = imgInfo.Channels;
			this.bitDepth = imgInfo.BitDepth;
			this.FilterUsed = FilterType.FILTER_UNKNOWN;
			this.SampleType = sampleType;
			this.SamplesUnpacked = unpackedMode || !imgInfo.Packed;
			this._elementsPerRow = this.SamplesUnpacked ? imgInfo.SamplesPerRow : imgInfo.SamplesPerRowPacked;
			this._imageRow = imageRow;
			if( sampleType==ESampleType.INT )
			{
				_scanline = scanLineInt!=null ? scanLineInt : new int[ ElementsPerRow ];
				_scanlineB = null;
				this._maxSampleVal = bitDepth==16 ? 0xFFFF : GetMaskForPackedFormatsLs( bitDepth );
			}
			else if( sampleType==ESampleType.BYTE )
			{
				_scanlineB = scanLineBytes!=null ? scanLineBytes : new byte[ ElementsPerRow ];
				_scanline = null;
				this._maxSampleVal = bitDepth==16 ? 0xFF : GetMaskForPackedFormatsLs( bitDepth );
			}
			else
			{
				throw new System.Exception("bad ImageLine initialization");
			}
		}

		static internal void UnpackInplaceInt ( ImageInfo iminfo , int[] src , int[] dst , bool Scale )
		{
			int bitDepth = iminfo.BitDepth;
			if( bitDepth>=8 ) { return; }// nothing to do
			int mask0 = GetMaskForPackedFormatsLs( bitDepth );
			int scalefactor = 8 - bitDepth;
			int offset0 = 8 * iminfo.SamplesPerRowPacked - bitDepth * iminfo.SamplesPerRow;
			int mask, offset, v;
			if( offset0!=8 )
			{
				mask = mask0<<offset0;
				offset = offset0; // how many bits to shift the mask to the right to recover mask0
			}
			else
			{
				mask = mask0;
				offset = 0;
			}
			for( int j=iminfo.SamplesPerRow-1 , i=iminfo.SamplesPerRowPacked-1 ; j>=0 ; j-- )
			{
				v = (src[i] & mask)>>offset;
				if( Scale ) { v <<= scalefactor; }
				dst[j] = v;
				mask <<= bitDepth;
				offset += bitDepth;
				if( offset==8 )
				{
					mask = mask0;
					offset = 0;
					i--;
				}
			}
		}

		static internal void PackInplaceInt ( ImageInfo iminfo , int[] src , int[] dst , bool scaled )
		{
			int bitDepth = iminfo.BitDepth;
			if( bitDepth>=8 ) { return; }// nothing to do
			int mask0 = GetMaskForPackedFormatsLs( bitDepth );
			int scalefactor = 8 - bitDepth;
			int offset0 = 8 - bitDepth;
			int v, v0;
			int offset = 8 - bitDepth;
			v0 = src[0]; // first value is special for in place
			dst[0] = 0;
			if( scaled ) { v0 >>= scalefactor; }
			v0 = ((v0 & mask0)<<offset);
			for( int i=0 , j=0 ; j<iminfo.SamplesPerRow ; j++ )
			{
				v = src[j];
				if( scaled ) { v >>= scalefactor; }
				dst[i] |= ((v & mask0)<<offset);
				offset -= bitDepth;
				if( offset<0 )
				{
					offset = offset0;
					i++;
					dst[i] = 0;
				}
			}
			dst[0] |= v0;
		}

		static internal void UnpackInplaceByte ( ImageInfo iminfo , byte[] src , byte[] dst , bool scale )
		{
			int bitDepth = iminfo.BitDepth;
			if( bitDepth>=8 ) return; // nothing to do
			int mask0 = GetMaskForPackedFormatsLs(bitDepth);
			int scalefactor = 8 - bitDepth;
			int offset0 = 8 * iminfo.SamplesPerRowPacked - bitDepth * iminfo.SamplesPerRow;
			int mask, offset, v;
			if( offset0!=8 )
			{
				mask = mask0<<offset0;
				offset = offset0; // how many bits to shift the mask to the right to recover mask0
			}
			else
			{
				mask = mask0;
				offset = 0;
			}
			for( int j=iminfo.SamplesPerRow-1 , i=iminfo.SamplesPerRowPacked-1 ; j>=0 ; j-- )
			{
				v = (src[i] & mask)>>offset;
				if( scale ) v <<= scalefactor;
				dst[j] = (byte)v;
				mask <<= bitDepth;
				offset += bitDepth;
				if( offset==8 )
				{
					mask = mask0;
					offset = 0;
					i--;
				}
			}
		}

		/** size original: samplesPerRow sizeFinal: samplesPerRowPacked (trailing elements are trash!) **/
		static internal void PackInplaceByte ( ImageInfo iminfo , byte[] src , byte[] dst , bool scaled )
		{
			int bitDepth = iminfo.BitDepth;
			if( bitDepth>=8 ) return;// nothing to do
			byte mask0 = (byte)GetMaskForPackedFormatsLs( bitDepth );
			byte scalefactor = (byte)( 8 - bitDepth );
			byte offset0 = (byte)( 8 - bitDepth );
			byte v, v0;
			int offset = 8 - bitDepth;
			v0 = src[0]; // first value is special
			dst[0] = 0;
			if( scaled ) v0 >>= scalefactor;
			v0 = (byte)((v0 & mask0)<<offset);
			for( int i=0 , j=0 ; j<iminfo.SamplesPerRow ; j++ )
			{
				v = src[j];
				if( scaled ) v >>= scalefactor;
				dst[i] |= (byte)((v & mask0)<<offset);
				offset -= bitDepth;
				if( offset<0 )
				{
					offset = offset0;
					i++;
					dst[i] = 0;
				}
			}
			dst[0] |= v0;
		}

		/// <summary> Makes a deep copy </summary>
		/// <remarks> You should rarely use this </remarks>
		internal void SetScanLine ( int[] b )
		{
			// makes copy
			System.Array.Copy( b , 0 , Scanline , 0 , Scanline.Length );
		}

		/// <summary> Makes a deep copy </summary>
		/// <remarks> You should rarely use this </remarks>
		internal int[] GetScanLineCopy ( int[] b )
		{
			if( b==null || b.Length<Scanline.Length )
			{
				b = new int[ Scanline.Length ];
			}
			System.Array.Copy( Scanline , 0 , b , 0 , Scanline.Length );
			return b;
		}

		public ImageLine UnpackToNewImageLine ()
		{
			ImageLine newline = new ImageLine( ImgInfo , SampleType , true , null , null , this.ImageRow );
			if( SampleType==ESampleType.INT ) UnpackInplaceInt( ImgInfo , Scanline , newline.Scanline , false );
			else UnpackInplaceByte( ImgInfo , ScanlineB , newline.ScanlineB , false );
			return newline;
		}

		public ImageLine PackToNewImageLine ()
		{
			ImageLine newline = new ImageLine( ImgInfo , SampleType , false , null , null , this.ImageRow );
			if( SampleType==ESampleType.INT ) PackInplaceInt( ImgInfo , Scanline , newline.Scanline , false );
			else PackInplaceByte( ImgInfo , ScanlineB , newline.ScanlineB , false );
			return newline;
		}

		public int[] GetScanlineInt () => Scanline;

		public byte[] GetScanlineByte () => ScanlineB;

		public bool IsInt () => SampleType==ESampleType.INT;

		public bool IsByte () => SampleType==ESampleType.BYTE;


		public override string ToString () => $"row={ImageRow} cols={ImgInfo.Cols} bpc={ImgInfo.BitDepth} size={Scanline.Length}";

		internal static int GetMaskForPackedFormats ( int bitDepth )
		{
			// Utility function for pack/unpack
			if( bitDepth==4 ) return 0xf0;
			else if( bitDepth==2 ) return 0xc0;
			else if( bitDepth==1 ) return 0x80;
			else return 0xff;
		}

		internal static int GetMaskForPackedFormatsLs ( int bitDepth )
		{
			// Utility function for pack/unpack
			if( bitDepth==4 ) return 0x0f;
			else if( bitDepth==2 ) return 0x03;
			else if( bitDepth==1 ) return 0x01;
			else return 0xff;
		}

	}
}
