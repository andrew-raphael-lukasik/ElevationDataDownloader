namespace Pngcs.Chunks
{
	class PngChunkSkipped : PngChunk
	{
		
		internal PngChunkSkipped ( string id , ImageInfo imgInfo , int clen )
			: base( id , imgInfo )
		{
			this.Length = clen;
		}

		public sealed override bool AllowsMultiple () => true;
		public sealed override ChunkRaw CreateRawChunk () => throw new System.Exception("Non supported for a skipped chunk");
		public sealed override void ParseFromRaw ( ChunkRaw c ) => throw new System.Exception("Non supported for a skipped chunk");
		public sealed override void CloneDataFromRead ( PngChunk other ) => throw new System.Exception("Non supported for a skipped chunk");
		public override ChunkOrderingConstraint GetOrderingConstraint () => ChunkOrderingConstraint.NONE;

	}
}
