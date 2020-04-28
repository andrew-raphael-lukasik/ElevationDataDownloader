using IO = System.IO;

namespace Pngcs.Chunks
{
	/// <summary>
	/// IHDR chunk: http://www.w3.org/TR/PNG/#11IHDR
	/// </summary>
	public class PngChunkIHDR : PngChunkSingle
	{

		public const string ID = ChunkHelper.IHDR;
		public int Cols {get;set;}
		public int Rows { get; set; }
		public int Bitspc { get; set; }
		public int Colormodel { get; set; }
		public int Compmeth { get; set; }
		public int Filmeth { get; set; }
		public int Interlaced { get; set; }

		public PngChunkIHDR ( ImageInfo info )
			: base( ID , info )
		{
			
		}

		public override ChunkOrderingConstraint GetOrderingConstraint () => ChunkOrderingConstraint.NA;

		public override ChunkRaw CreateRawChunk ()
		{
			ChunkRaw chunk = new ChunkRaw( 13 , ChunkHelper.b_IHDR , true );
			byte[] data = chunk.Data;
			int offset = 0;
			PngHelperInternal.WriteInt4tobytes( Cols , data , offset );
			offset += 4;
			PngHelperInternal.WriteInt4tobytes( Rows , data , offset );
			offset += 4;
			data[offset++] = (byte)Bitspc;
			data[offset++] = (byte)Colormodel;
			data[offset++] = (byte)Compmeth;
			data[offset++] = (byte)Filmeth;
			data[offset++] = (byte)Interlaced;
			return chunk;
		}

		public override void ParseFromRaw ( ChunkRaw chunk )
		{
			if( chunk.Len!=13 ) throw new System.Exception($"Bad IDHR len {chunk.Len}");
			IO.MemoryStream stream = chunk.GetAsByteStream();
			Cols = PngHelperInternal.ReadInt4( stream );
			Rows = PngHelperInternal.ReadInt4( stream );
			// bit depth: number of bits per channel
			Bitspc = PngHelperInternal.ReadByte( stream );
			Colormodel = PngHelperInternal.ReadByte( stream );
			Compmeth = PngHelperInternal.ReadByte( stream );
			Filmeth = PngHelperInternal.ReadByte( stream );
			Interlaced = PngHelperInternal.ReadByte( stream );
		}

		public override void CloneDataFromRead ( PngChunk other )
		{
			PngChunkIHDR otherx = (PngChunkIHDR)other;
			Cols = otherx.Cols;
			Rows = otherx.Rows;
			Bitspc = otherx.Bitspc;
			Colormodel = otherx.Colormodel;
			Compmeth = otherx.Compmeth;
			Filmeth = otherx.Filmeth;
			Interlaced = otherx.Interlaced;
		}

	}
}
