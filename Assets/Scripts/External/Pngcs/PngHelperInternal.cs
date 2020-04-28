using IO = System.IO;

namespace Pngcs
{
	/// <summary> Some utility static methods for internal use. </summary>
	public class PngHelperInternal
	{

		[System.ThreadStatic]
		static Zlib.CRC32 crc32Engine = null;

		/// <summary> thread-singleton crc engine </summary>
		public static Zlib.CRC32 GetCRC ()
		{
			if( crc32Engine==null) crc32Engine = new Zlib.CRC32();
			return crc32Engine;
		}

		public static readonly byte[] PNG_ID_SIGNATURE = { 256 - 119, 80, 78, 71, 13, 10, 26, 10 }; // png magic

		public static System.Text.Encoding charsetLatin1 = System.Text.Encoding.GetEncoding("ISO-8859-1"); // charset
		public static System.Text.Encoding charsetUtf8 = System.Text.Encoding.GetEncoding("UTF-8"); // charset used for some chunks

		public static int DoubleToInt100000 ( double d ) => (int)( d*100000.0 + 0.5 );

		public static double IntToDouble100000 ( int i ) => i/100000.0;

		public static void WriteInt2 ( IO.Stream ostream , int n )
		{
			byte[] temp = { (byte)((n>>8) & 0xff), (byte)(n & 0xff) };
			WriteBytes( ostream , temp );
		}

		/// <summary> -1 si eof </summary>
		public static int ReadInt2 ( IO.Stream mask0 )
		{
			int b1 = mask0.ReadByte();
			int b2 = mask0.ReadByte();
			if( b1==-1 || b2==-1 ) return -1;
			return (b1<<8) + b2;
		}

		/// <summary> -1 si eof </summary>
		public static int ReadInt4 ( IO.Stream mask0 )
		{
			int b1 = mask0.ReadByte();
			int b2 = mask0.ReadByte();
			int b3 = mask0.ReadByte();
			int b4 = mask0.ReadByte();
			if( b1==-1 || b2==-1 || b3==-1 || b4==-1 ) return -1;
			return (b1<<24) + (b2<<16) + (b3<<8) + b4;
		}

		public static int ReadInt1fromByte ( byte[] b , int offset ) => (b[offset] & 0xff);

		public static int ReadInt2fromBytes ( byte[] b , int offset ) => ((b[offset] & 0xff)<<16) | ((b[offset+1] & 0xff));

		public static int ReadInt4fromBytes ( byte[] b , int offset ) => ((b[offset] & 0xff)<<24) | ((b[offset+1] & 0xff)<<16) | ((b[offset+2] & 0xff)<<8) | (b[offset+3] & 0xff);

		public static void WriteInt2tobytes ( int n , byte[] b , int offset )
		{
			b[offset] = (byte)((n>>8) & 0xff);
			b[offset + 1] = (byte)(n & 0xff);
		}

		public static void WriteInt4tobytes ( int n , byte[] b , int offset )
		{
			b[offset] = (byte)((n>>24) & 0xff);
			b[offset + 1] = (byte)((n>>16) & 0xff);
			b[offset + 2] = (byte)((n>>8) & 0xff);
			b[offset + 3] = (byte)(n & 0xff);
		}

		public static void WriteInt4 ( IO.Stream ostream , int n )
		{
			byte[] temp = new byte[4];
			WriteInt4tobytes( n , temp , 0 );
			WriteBytes( ostream , temp );
			//Console.WriteLine("writing int " + n + " b=" + (sbyte)temp[0] + "," + (sbyte)temp[1] + "," + (sbyte)temp[2] + "," + (sbyte)temp[3]);
		}

		/// <summary> Guaranteed to read exactly len bytes. throws error if it cant </summary>
		public static void ReadBytes ( IO.Stream mask0 , byte[] b , int offset , int len )
		{
			if( len==0 ) return;
			int read = 0;
			while( read<len )
			{
				int n = mask0.Read( b , offset+read , len-read );
				if( n<1 ) throw new System.Exception($"error reading, {n} != {len}\n");
				read += n;
			}
		}

		public static void SkipBytes ( IO.Stream istream , int len )
		{
			byte[] buf = new byte[8192 * 4];
			int read, remain = len;
			while( remain>0 )
			{
				read = istream.Read( buf , 0 , remain>buf.Length ? buf.Length : remain );
				if( read<0 ) throw new IO.IOException("error reading (skipping) : EOF\n");
				remain -= read;
			}
		}

		public static void WriteBytes ( IO.Stream ostream , byte[] b ) => ostream.Write( b , 0 , b.Length );

		public static void WriteBytes ( IO.Stream ostream , byte[] b , int offset , int n ) => ostream.Write( b , offset , n );

		public static int ReadByte ( IO.Stream mask0 ) => mask0.ReadByte();

		public static void WriteByte ( IO.Stream ostream , byte b ) => ostream.WriteByte( (byte)b );

		// a = left, b = above, c = upper left
		public static int UnfilterRowPaeth ( int r , int a , int b , int c ) => (r+FilterPaethPredictor(a,b,c)) & 0xFF;

		public static int FilterPaethPredictor ( int a , int b , int c )
		{
			// from http://www.libpng.org/pub/png/spec/1.2/PNG-Filters.html
			// a = left, b = above, c = upper left
			int p = a + b - c;// ; initial estimate
			int pa = p>=a ? p-a : a-p;
			int pb = p>=b ? p-b : b-p;
			int pc = p>=c ? p-c : c-p;
			// ; return nearest of a,b,c,
			// ; breaking ties in order a,b,c.
			if( pa<=pb && pa<=pc ) return a;
			else if( pb<=pc ) return b;
			else return c;
		}

		public static void InitCrcForTests ( PngReader reader ) => reader.InitCrctest();

		public static long GetCrctestVal ( PngReader reader ) => reader.GetCrctestVal();

	}
}
