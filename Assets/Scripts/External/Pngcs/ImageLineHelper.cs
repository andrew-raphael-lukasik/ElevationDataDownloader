namespace Pngcs
{
	/// <summary>
	/// Bunch of utility static methods to process/analyze an image line. 
	/// Not essential at all, some methods are probably to be removed if future releases.
	/// TODO: document this better
	/// </summary>
	public class ImageLineHelper
	{

		/// <summary> Given an indexed line with a palette, unpacks as a RGB array </summary>
		/// <param name="line"> ImageLine as returned from PngReader </param>
		/// <param name="pal"> Palette chunk </param>
		/// <param name="trns"> TRNS chunk (optional) </param>
		/// <param name="buffer"> Preallocated array, optional </param>
		/// <returns> R G B (one byte per sample) </returns>
		public static int[] Palette2rgb ( ImageLine line , Chunks.PngChunkPLTE pal , Chunks.PngChunkTRNS trns , int[] buffer )
		{
			int numCols = line.ImgInfo.Cols;
			bool isalpha = trns!=null;
			int channels = isalpha ? 4 : 3;
			int nsamples = numCols * channels;
			if( buffer==null || buffer.Length<nsamples ) buffer = new int[ nsamples ];
			if( line.SamplesUnpacked==false ) line = line.UnpackToNewImageLine();
			int nindexesWithAlpha = trns!=null ? trns.PaletteAlpha.Length : 0;
			if( line.SampleType==Pngcs.ImageLine.ESampleType.BYTE )
			{
				var scanline = line.ScanlineB;
				if( isalpha )
				{
					var paletteAlpha = trns.PaletteAlpha;
					for( int col=0 ; col<numCols ; col++ )
					{
						int index = scanline[col] & 0xFF;
						pal.GetEntryRgb( index , buffer , col*channels );
						int alpha = index<nindexesWithAlpha ? paletteAlpha[index] : 255;
						buffer[ col*channels+3 ] = alpha;
					}
				}
				else
				{
					for( int col=0 ; col<numCols ; col++ )
					{
						int index = scanline[col] & 0xFF;
						pal.GetEntryRgb( index , buffer , col*channels );
					}
				}
			}
			else
			{
				var scanline = line.Scanline;
				if( isalpha )
				{
					var paletteAlpha = trns.PaletteAlpha;
					for( int col=0 ; col<numCols ; col++ )
					{
						int index = scanline[col];
						pal.GetEntryRgb( index , buffer , col*channels );
						int alpha = index<nindexesWithAlpha ? paletteAlpha[index] : 255;
						buffer[ col*channels+3 ] = alpha;
					}
				}
				else
				{
					for( int col=0 ; col<numCols ; col++ )
					{
						int index = scanline[col];
						pal.GetEntryRgb( index , buffer , col*channels );
					}
				}
			}
			return buffer;
		}

		public static int[] Palette2rgb ( ImageLine line , Chunks.PngChunkPLTE pal , int[] buf ) => Palette2rgb( line , pal , null , buf );

		public static ARGB8<int> ToARGB8 ( int r , int g , int b )
		{
			unchecked
			{
				return new ARGB8<int>{ value = ((int)(0xFF000000)) | ((r)<<16) | ((g)<<8) | (b) };
			}
		}

		public static ARGB8<int> ToARGB8 ( int r , int g , int b , int a ) => new ARGB8<int>{ value = ((a)<<24) | ((r)<<16) | ((g)<<8) | (b) };

		 public static ARGB8<int> ToARGB8 ( int[] buff , int offset , bool alpha )
		 {
			return alpha
				? ToARGB8( buff[offset++] , buff[offset++] , buff[offset++] , buff[offset] )
				: ToARGB8( buff[offset++] , buff[offset++] , buff[offset] );
		 }

		 public static ARGB8<int> ToARGB8 ( byte[] buff , int offset , bool alpha )
		 {
			return alpha
				? ToARGB8( buff[offset++] , buff[offset++] , buff[offset++] , buff[offset] )
				: ToARGB8( buff[offset++] , buff[offset++] , buff[offset] );
		 }

		public static void FromARGB8 ( ARGB8<int> value , int[] buff , int offset , bool alpha )
		{
			buff[ offset++ ] = ((value>>16) & 0xFF);
			buff[ offset++ ] = ((value>>8) & 0xFF);
			buff[ offset ] = (value & 0xFF);
			if( alpha ) buff[ offset + 1 ] = ((value>>24) & 0xFF);
		}

		public static void FromARGB8 ( ARGB8<int> value , byte[] buff , int offset , bool alpha )
		{
			buff[ offset++ ] = (byte)((value>>16) & 0xFF);
			buff[ offset++ ] = (byte)((value>>8) & 0xFF);
			buff[ offset ] = (byte)(value & 0xFF);
			if( alpha ) buff[ offset+1 ] = (byte)((value>>24) & 0xFF);
		}

		public static ARGB8<int> GetPixelToARGB8 ( ImageLine line , int column )
		{
			return line.IsInt()
				? ToARGB8( line.Scanline , column * line.channels , line.ImgInfo.Alpha )
				: ToARGB8( line.ScanlineB , column * line.channels , line.ImgInfo.Alpha );
		}

		public static void SetPixelFromARGB8 ( ImageLine line , int column , ARGB8<int> argb )
		{
			if( line.IsInt() ) FromARGB8( argb, line.Scanline , column*line.channels , line.ImgInfo.Alpha );
			else FromARGB8( argb , line.ScanlineB , column*line.channels , line.ImgInfo.Alpha );
		}

		public static void SetPixel ( ImageLine line , int column , int r , int g , int b , int a )
		{
			int offset = column * line.channels;
			if( line.IsInt() )
			{
				var scanline = line.Scanline;
				scanline[ offset++ ] = r;
				scanline[ offset++ ] = g;
				scanline[ offset ] = b;
				if( line.ImgInfo.Alpha ) scanline[ offset+1 ] = a;
			}
			else
			{
				var scanline = line.ScanlineB;
				scanline[ offset++ ] = (byte)r;
				scanline[ offset++ ] = (byte)g;
				scanline[ offset ] = (byte)b;
				if( line.ImgInfo.Alpha ) scanline[ offset+1 ] = (byte)a;
			}
		}

		public static void SetPixel ( int[] data , int value , int column , int channels ) => data[ column*channels ] = value;
		public static void SetPixel ( byte[] data , byte value , int column , int channels ) => data[ column*channels ] = value;
		public static void SetPixel ( int[] data , RGB<int> rgb , int column , int channels )
		{
			int offset = column * channels;
			data[ offset++ ] = rgb.R;
			data[ offset++ ] = rgb.G;
			data[ offset ] = rgb.B;
		}
		public static void SetPixel ( int[] data , RGBA<int> rgba , int column , int channels )
		{
			int offset = column * channels;
			data[ offset++ ] = rgba.R;
			data[ offset++ ] = rgba.G;
			data[ offset ] = rgba.B;
			data[ offset+1 ] = rgba.A;
		}
		public static void SetPixel ( byte[] data , RGB<byte> rgb , int column , int channels )
		{
			int offset = column * channels;
			data[ offset++ ] = rgb.R;
			data[ offset++ ] = rgb.G;
			data[ offset ] = rgb.B;
		}
		public static void SetPixel ( byte[] data , RGBA<byte> rgba , int column , int channels )
		{
			int offset = column * channels;
			data[ offset++ ] = rgba.R;
			data[ offset++ ] = rgba.G;
			data[ offset ] = rgba.B;
			data[ offset+1 ] = rgba.A;
		}
		public static void SetPixel ( ImageLine line , int column , int value )
		{
			if( line.channels!=1 ) throw new System.Exception("this method is for 1 channel images only");
			if( line.IsInt() ) line.Scanline[ column ] = value;
			else line.ScanlineB[ column ] = (byte)value;
		}

		public static void SetPixel ( ImageLine line , int col , int r , int g , int b ) => SetPixel( line , col , r , g , b , line.MaxSampleVal );

		public static double ReadDouble ( ImageLine line , int pos )
		{
			return line.IsInt()
				? ( line.Scanline[pos] / (line.MaxSampleVal + 0.9) )
				: ( (line.ScanlineB[pos]) / (line.MaxSampleVal + 0.9) );
		}

		public static void WriteDouble ( ImageLine line , double d , int pos )
		{
			if( line.IsInt() ) line.Scanline[ pos ] = (int)(d * (line.MaxSampleVal + 0.99));
			else line.ScanlineB[ pos ] = (byte)(d * (line.MaxSampleVal + 0.99));
		}

		public static int Interpol ( int a , int b , int c , int d , double dx , double dy )
		{
			// a b -> x (0-1)
			// c d
			double e = a * (1.0 - dx) + b * dx;
			double f = c * (1.0 - dx) + d * dx;
			return (int)( e * (1 - dy) + f * dy + 0.5 );
		}

		public static int ClampTo_0_255 ( int i ) => i>255 ? 255 : (i<0 ? 0 : i);

		/**
		 * [0,1)
		 */
		public static double ClampDouble ( double i ) => i<0 ? 0 : (i>=1 ? 0.999999 : i);

		public static int ClampTo_0_65535 ( int i ) => i>65535 ? 65535 : (i<0 ? 0 : i);

		public static int ClampTo_128_127 ( int x ) => x>127 ? 127 : (x<-128 ? -128 : x);

		public static int[] Unpack ( ImageInfo imgInfo , int[] src , int[] dst , bool scale )
		{
			int len1 = imgInfo.SamplesPerRow;
			int len0 = imgInfo.SamplesPerRowPacked;
			if( dst==null || dst.Length<len1 ) dst = new int[ len1 ];
			if( imgInfo.Packed ) ImageLine.UnpackInplaceInt( imgInfo , src , dst , scale );
			else System.Array.Copy( src , 0 , dst , 0 , len0 );
			return dst;
		}
		public static byte[] Unpack ( ImageInfo imgInfo , byte[] src , byte[] dst , bool scale )
		{
			int len1 = imgInfo.SamplesPerRow;
			int len0 = imgInfo.SamplesPerRowPacked;
			if( dst==null || dst.Length<len1 ) dst = new byte[ len1 ];
			if( imgInfo.Packed ) ImageLine.UnpackInplaceByte( imgInfo , src , dst , scale );
			else System.Array.Copy( src , 0 , dst , 0 , len0 );
			return dst;
		}

		public static int[] Pack ( ImageInfo imgInfo , int[] src , int[] dst , bool scale )
		{
			int len0 = imgInfo.SamplesPerRowPacked;
			if( dst==null || dst.Length<len0 ) dst = new int[ len0 ];
			if( imgInfo.Packed ) ImageLine.PackInplaceInt( imgInfo , src , dst , scale );
			else System.Array.Copy( src , 0 , dst , 0 , len0 );
			return dst;
		}
		public static byte[] Pack ( ImageInfo imgInfo , byte[] src , byte[] dst , bool scale )
		{
			int len0 = imgInfo.SamplesPerRowPacked;
			if( dst==null || dst.Length<len0 ) dst = new byte[ len0 ];
			if( imgInfo.Packed ) ImageLine.PackInplaceByte( imgInfo , src , dst , scale );
			else System.Array.Copy( src , 0 , dst , 0 , len0 );
			return dst;
		}

	}
}
