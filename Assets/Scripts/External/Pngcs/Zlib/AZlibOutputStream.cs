using IO = System.IO;

namespace Pngcs.Zlib
{
	public abstract class AZlibOutputStream : IO.Stream
	{

		readonly protected IO.Stream rawStream;
		readonly protected bool leaveOpen;
		protected int compressLevel;
		protected EDeflateCompressStrategy strategy;

		public AZlibOutputStream ( IO.Stream st , int compressLevel , EDeflateCompressStrategy strat , bool leaveOpen )
		{
			rawStream = st;
			this.leaveOpen = leaveOpen;
			this.strategy = strat;
			this.compressLevel = compressLevel;
		}

		public override void SetLength ( long value ) => throw new System.NotImplementedException();

		public override bool CanSeek
		{
			get => false;
		}

		public override long Seek ( long offset , IO.SeekOrigin origin ) => throw new System.NotImplementedException();

		public override long Position
		{
			get => throw new System.NotImplementedException();
			set => throw new System.NotImplementedException();
		}

		public override long Length
		{
			get => throw new System.NotImplementedException();
		}


		public override int Read(byte[] buffer, int offset, int count) => throw new System.NotImplementedException();

		public override bool CanRead
		{
			get => false;
		}

		public override bool CanWrite
		{
			get => true;
		}

		public override bool CanTimeout
		{
			get => false;
		}

		/// <summary> mainly for debugging </summary>
		public abstract string getImplementationId();
		
	}
}
