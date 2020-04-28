#if SHARPZIPLIB

using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
// ONLY IF SHARPZIPLIB IS AVAILABLE

namespace Pngcs.Zlib
{
	/// <summary>
	/// Zip input (inflater) based on ShaprZipLib
	/// </summary>
	class ZlibInputStreamIs : AZlibInputStream
	{

		InflaterInputStream ist;

		public ZlibInputStreamIs ( Stream st , bool leaveOpen )
			: base( st , leaveOpen )
		{
			ist = new InflaterInputStream(st);
			ist.IsStreamOwner = !leaveOpen;
		}

		public override int Read ( byte[] bytes , int offset , int count ) => ist.Read( bytes , offset , count );

		public override int ReadByte () => ist.ReadByte();

		public override void Close () => ist.Close();


		public override void Flush () => ist.Flush();

		public override string getImplementationId () => "Zlib inflater: SharpZipLib";

	}
}
#endif
