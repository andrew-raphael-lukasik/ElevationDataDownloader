﻿using System.IO;

namespace Pngcs
{
    class BufferedStreamFeeder
    {
        Stream _stream;
        byte[] buf;
        int pendinglen; // bytes read and stored in buf that have not yet still been fed to IBytesConsumer
        int offset;
        bool eof = false;
        bool closeStream = true;
        bool failIfNoFeed = false;
        const int DEFAULTSIZE = 8192;

       	public BufferedStreamFeeder ( Stream ist )
           : this( ist , DEFAULTSIZE )
        {

	    }

    	public BufferedStreamFeeder ( Stream ist , int bufsize )
        {
	    	this._stream = ist;
	    	buf = new byte[ bufsize ];
	    }


        /// <summary>
        /// Stream from which bytes are read
        /// </summary>
        public Stream getStream ()
        {
            return _stream;
        }
        
        /// <summary>
        /// Feeds bytes to the consumer 
        ///  Returns bytes actually consumed
        ///  This should return 0 only if the stream is EOF or the consumer is done
        /// </summary>
        /// <param name="consumer"></param>
        /// <returns></returns>
        public int feed ( IBytesConsumer consumer )
        {
            return feed( consumer , -1 );
        }

        public int feed ( IBytesConsumer consumer , int maxbytes )
        {
            int n = 0;
            if( pendinglen==0 )
            {
                refillBuffer();
            }
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
            if( n<1 && failIfNoFeed ) { throw new PngjInputException( "failed feed bytes" ); }
            return n;
        }

        public bool feedFixed(IBytesConsumer consumer, int nbytes)
        {
            int remain = nbytes;
            while (remain > 0)
            {
                int n = feed(consumer, remain);
                if (n < 1)
                    return false;
                remain -= n;
            }
            return true;
        }

        protected void refillBuffer ()
        {
            if( pendinglen>0 || eof )
                return; // only if not pending data
            try
            {
                // try to read
                offset = 0;
                pendinglen = _stream.Read( buf , 0 , buf.Length );
                if( pendinglen<0 )
                {
                    close();
                }
            }
            catch( IOException e )
            {
                throw new PngjInputException( e );
            }
        }

        public bool hasMoreToFeed ()
        {
            if( eof )
                return pendinglen>0;
            else
                refillBuffer();
            return pendinglen > 0;
        }

        public void setCloseStream ( bool closeStream )
        {
            this.closeStream = closeStream;
        }

        public void close ()
        {
            eof = true;
            buf = null;
            pendinglen = 0;
            offset = 0;
            try
            {
                if( _stream!=null && closeStream )
                {
                    _stream.Close();
                }
            }
            catch( System.Exception e )
            {
                PngHelperInternal.Log( "Exception closing stream" , e );
            }
            _stream = null;
        }

       	public void setInputStream ( Stream ist )// to reuse this object
        {
		    this._stream = ist;
		    eof = false;
	    }

        public bool isEof ()
        {
            return eof;
        }

        public void setFailIfNoFeed ( bool failIfNoFeed )
        {
            this.failIfNoFeed = failIfNoFeed;
        }

    }
}
