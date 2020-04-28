namespace Pngcs.Zlib
{
	public abstract class AZlibInputStream : System.IO.Stream
	{

		readonly protected System.IO.Stream rawStream;
		readonly protected bool leaveOpen;

		public AZlibInputStream ( System.IO.Stream st , bool leaveOpen )
		{
			rawStream = st;
			this.leaveOpen = leaveOpen;
		}

		public override bool CanRead
		{
			get => true;
		}

		public override bool CanWrite
		{
			get => false;
		}

		public override void SetLength(long value) => throw new System.NotImplementedException();


		public override bool CanSeek
		{
			get => false;
		}

		public override long Seek ( long offset , System.IO.SeekOrigin origin ) => throw new System.NotImplementedException();

		public override long Position {
			get => throw new System.NotImplementedException();
			set => throw new System.NotImplementedException();
		}

		public override long Length {
			get => throw new System.NotImplementedException();
		}


		public override void Write ( byte[] buffer , int offset , int count ) => throw new System.NotImplementedException();

		public override bool CanTimeout
		{
			get => false;
		}

		/// <summary> mainly for debugging </summary>
		public abstract string getImplementationId();

	}
}
