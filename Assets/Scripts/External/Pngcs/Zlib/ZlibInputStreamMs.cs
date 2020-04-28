using IO = System.IO;
using Compression = System.IO.Compression;

// ONLY FOR .NET 4.5
namespace Pngcs.Zlib
{

//#if NET45

	/// <summary>
	/// Zip input (deflater) based on Ms DeflateStream (.net 4.5)
	/// </summary>
	internal class ZlibInputStreamMs : AZlibInputStream
	{

		public ZlibInputStreamMs ( IO.Stream stream , bool leaveOpen )
			: base( stream , leaveOpen )
		{

		}

		Compression.DeflateStream deflateStream;// lazily created, if real read is called
		bool initdone = false;
		bool closed = false;

		// Adler32 adler32 ; // we dont check adler32!
		bool fdict;// merely informational, not used
		int cmdinfo;// merely informational, not used
		byte[] dictid; // merely informational, not used
		byte[] crcread = null; // merely informational, not checked

		public override int Read ( byte[] bytes , int offset , int count )
		{
			if( !initdone ) doInit();
			if( deflateStream==null && count>0 ) initStream();
			// we dont't check CRC on reading
			int r = deflateStream.Read( bytes , offset , count );
			if( r<1 && crcread==null )
			{  // deflater has ended. we try to read next 4 bytes from raw stream (crc)
				crcread = new byte[4];
				for( int i=0 ; i<4 ; i++ )
				{
					crcread[i] = (byte)rawStream.ReadByte(); // we dont really check/use this
				}
			}
			return r;
		}

		public override void Close ()
		{
			if( !initdone ) doInit(); // can happen if never called write
			if( closed ) return;
			closed = true;
			if( deflateStream!=null )
			{
				deflateStream.Close();
			}
			if( crcread==null )
			{ // eat trailing 4 bytes
				crcread = new byte[4];
				for( int i=0 ; i<4 ; i++ )
					crcread[i] = (byte)rawStream.ReadByte();
			}
			if( !leaveOpen )
			{
				rawStream.Close();
			}
		}

		void initStream ()
		{
			if( deflateStream!=null ) return;
			deflateStream = new Compression.DeflateStream( rawStream , Compression.CompressionMode.Decompress , true );
		}

		void doInit ()
		{
			if( initdone ) return;
			initdone = true;
			// read zlib header : http://www.ietf.org/rfc/rfc1950.txt
			int cmf = rawStream.ReadByte();
			int flag = rawStream.ReadByte();
			if( cmf==-1 || flag==-1) return;
			if((cmf & 0x0f)!=8) throw new System.Exception("Bad compression method for ZLIB header: cmf=" + cmf);
			cmdinfo = ((cmf & (0xf0))>>8);// not used?
			fdict = (flag & 32)!=0;
			if( fdict) {
				dictid = new byte[4];
				for (int i = 0; i<4; i++) {
					dictid[i] = (byte)rawStream.ReadByte(); // we eat but don't use this
				}
			}
		}
	
		public override void Flush ()
		{
			if(deflateStream!=null) deflateStream.Flush();
		}

		public override  string getImplementationId () => "Zlib inflater: .Net CLR 4.5";
//#endif
	
	}
}
