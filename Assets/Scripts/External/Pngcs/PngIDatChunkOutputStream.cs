using IO = System.IO;

namespace Pngcs
{
	/// <summary>
	/// outputs the stream for IDAT chunk , fragmented at fixed size (32k default).
	/// </summary>
	internal class PngIDatChunkOutputStream : ProgressiveOutputStream
	{

		const int SIZE_DEFAULT = 32768;// 32k
		readonly IO.Stream outputStream;

		public PngIDatChunkOutputStream ( IO.Stream outputStream_0 )
			: this( outputStream_0 , SIZE_DEFAULT )
		{

		}

		public PngIDatChunkOutputStream ( IO.Stream outputStream_0 , int size )
			: base( size>8 ? size : SIZE_DEFAULT )
		{
			this.outputStream = outputStream_0;
		}

		protected override void FlushBuffer ( byte[] b , int len )
		{
			var c = new Chunks.ChunkRaw( len , Chunks.ChunkHelper.b_IDAT , false );
			c.Data = b;
			c.WriteChunk( outputStream );
		}

		// closing the IDAT stream only flushes it, it does not close the underlying stream
		public override void Close () => Flush();

	}
}
