using IO = System.IO;

namespace Pngcs
{
	class BufferedStreamFeeder
	{
		
		/// <summary> Stream from which bytes are read </summary>
		public IO.Stream Stream => _stream;
		IO.Stream _stream;

		byte[] buf;
		int pendinglen; // bytes read and stored in buf that have not yet still been fed to IBytesConsumer
		int offset;
		bool eof = false;
		bool closeStream = true;
		bool failIfNoFeed = false;
		const int DEFAULTSIZE = 8192;

	   	public BufferedStreamFeeder ( IO.Stream ist )
		   : this( ist , DEFAULTSIZE )
		{

		}

		public BufferedStreamFeeder ( IO.Stream ist , int bufsize )
		{
			this._stream = ist;
			buf = new byte[ bufsize ];
		}
		
		/// <summary> Feeds bytes to the consumer </summary>
		/// <returns> Bytes actually consumed. Should return 0 only if the stream is EOF or the consumer is done </returns>
		public int Feed ( IBytesConsumer consumer ) => Feed( consumer , -1 );

		public int Feed ( IBytesConsumer consumer , int maxbytes )
		{
			int n = 0;
			if( pendinglen==0 ) RefillBuffer();
			int tofeed = maxbytes>0 && maxbytes<pendinglen ? maxbytes : pendinglen;
			if( tofeed>0 )
			{
				n = consumer.consume( buf , offset , tofeed );
				if( n>0 )
				{
					offset += n;
					pendinglen -= n;
				}
			}
			if( n<1 && failIfNoFeed ) { throw new IO.IOException("failed feed bytes"); }
			return n;
		}

		public bool FeedFixed ( IBytesConsumer consumer , int nbytes )
		{
			int remain = nbytes;
			while( remain>0 )
			{
				int n = Feed( consumer , remain );
				if( n<1 ) return false;
				remain -= n;
			}
			return true;
		}

		protected void RefillBuffer ()
		{
			if( pendinglen>0 || eof ) return; // only if not pending data
			// try to read
			offset = 0;
			pendinglen = _stream.Read( buf , 0 , buf.Length );
			if( pendinglen<0 )
			{
				Close();
			}
		}

		public bool HasMoreToFeed ()
		{
			if( eof ) return pendinglen>0;
			else RefillBuffer();
			return pendinglen>0;
		}

		public void SetCloseStream ( bool closeStream ) => this.closeStream = closeStream;

		public void Close ()
		{
			eof = true;
			buf = null;
			pendinglen = 0;
			offset = 0;
			if( _stream!=null && closeStream ) _stream.Close();
			_stream = null;
		}

	   	public void SetInputStream ( IO.Stream stream )// to reuse this object
		{
			this._stream = stream;
			eof = false;
		}

		public bool IsEof () => eof;

		public void SetFailIfNoFeed ( bool failIfNoFeed ) => this.failIfNoFeed = failIfNoFeed;

	}
}
