using IO = System.IO;

namespace Pngcs.Zlib
{
	public class ZlibStreamFactory
	{
		public static AZlibInputStream createZlibInputStream ( IO.Stream st , bool leaveOpen )
		{
//#if NET45
			return new ZlibInputStreamMs(st,leaveOpen);
//#endif
//#if SHARPZIPLIB
//		  return new ZlibInputStreamIs(st, leaveOpen);
//#endif
		}

		public static AZlibInputStream createZlibInputStream ( IO.Stream stream ) => createZlibInputStream(stream, false);

		public static AZlibOutputStream createZlibOutputStream ( IO.Stream stream , int compressLevel , EDeflateCompressStrategy strat , bool leaveOpen )
		{
//#if NET45
				return new ZlibOutputStreamMs( stream, compressLevel,strat, leaveOpen);
//#endif
//#if SHARPZIPLIB
//			return new ZlibOutputStreamIs(st, compressLevel, strat, leaveOpen);
//#endif
		}

		public static AZlibOutputStream createZlibOutputStream ( IO.Stream stream ) => createZlibOutputStream( stream , false );

		public static AZlibOutputStream createZlibOutputStream ( IO.Stream stream , bool leaveOpen )
			=> createZlibOutputStream( stream , DeflateCompressLevel.DEFAULT , EDeflateCompressStrategy.Default , leaveOpen );
	}
}
