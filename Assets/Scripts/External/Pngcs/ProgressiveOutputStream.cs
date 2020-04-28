using IO = System.IO;

namespace Pngcs
{
	/// <summary>
	/// stream that outputs to memory and allows to flush fragments every 'size'
	/// bytes to some other destination
	/// </summary>
	abstract internal class ProgressiveOutputStream : IO.MemoryStream
	{
		
		readonly int size;
		long countFlushed = 0;

		public ProgressiveOutputStream ( int size_0 )
		{
			this.size = size_0;
			if( size<8 ) throw new System.Exception($"bad size for ProgressiveOutputStream: {size}");
		}

		public override void Close ()
		{
			Flush();
			base.Close();
		}

		public override void Flush ()
		{
			base.Flush();
			CheckFlushBuffer( true );
		}

		public override void Write ( byte[] b , int off , int len )
		{
			base.Write( b , off , len );
			CheckFlushBuffer(false);
		}

		public void Write ( byte[] b )
		{
			Write( b , 0 , b.Length );
			CheckFlushBuffer(false);
		}

		/// <summary>
		/// if it's time to flush data (or if forced==true) calls abstract method
		/// flushBuffer() and cleans those bytes from own buffer
		/// </summary>
		void CheckFlushBuffer ( bool forced )
		{
			int count = (int)Position;
			byte[] buf = GetBuffer();
			while( forced || count>=size )
			{
				int nb = size;
				if( nb>count ) nb = count;
				if( nb==0 ) return;
				FlushBuffer( buf , nb );
				countFlushed += nb;
				int bytesleft = count - nb;
				count = bytesleft;
				Position = count;
				if( bytesleft>0 )
				{
					System.Array.Copy( buf , nb , buf , 0 , bytesleft );
				}
			}
		}

		protected abstract void FlushBuffer ( byte[] b , int n );

		public long GetCountFlushed () => countFlushed;

	}
}
