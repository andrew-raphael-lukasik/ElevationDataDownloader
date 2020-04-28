#if SHARPZIPLIB
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
// ONLY IF SHARPZIPLIB IS AVAILABLE

namespace Pngcs.Zlib
{
	/// <summary>
	/// Zlib output (deflater) based on ShaprZipLib
	/// </summary>
	class ZlibOutputStreamIs : AZlibOutputStream
	{

		DeflaterOutputStream ost;
		Deflater deflater;
		public ZlibOutputStreamIs ( IO.Stream st , int compressLevel , EDeflateCompressStrategy strat , bool leaveOpen )
			: base( st ,compressLevel , strat , leaveOpen )
		{
			deflater=new Deflater( compressLevel );
			setStrat( strat );
			ost = new DeflaterOutputStream( st , deflater );
			ost.IsStreamOwner = !leaveOpen;
		}

		public void setStrat ( EDeflateCompressStrategy strat )
		{
			DeflateStrategy newDeflateStrategy = DeflateStrategy.Default;
			if( strat==EDeflateCompressStrategy.Filtered )
				newDeflateStrategy = DeflateStrategy.Filtered;
			else if( strat==EDeflateCompressStrategy.Huffman )
				newDeflateStrategy = DeflateStrategy.HuffmanOnly;
			else
				newDeflateStrategy = DeflateStrategy.Default;
			
			deflater.SetStrategy( newDeflateStrategy );
		}

		public override void Write ( byte[] buffer , int offset , int count ) => ost.Write( buffer , offset , count );

		public override void WriteByte ( byte value ) => ost.WriteByte( value );
 
		public override void Close () => ost.Close();

		public override void Flush () => ost.Flush();

		public override string getImplementationId () => "Zlib deflater: SharpZipLib";

	}
}
#endif
