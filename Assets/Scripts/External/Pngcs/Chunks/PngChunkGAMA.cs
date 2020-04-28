namespace Pngcs.Chunks
{
	/// <summary> gAMA chunk, see http://www.w3.org/TR/PNG/#11gAMA </summary>
	public class PngChunkGAMA : PngChunkSingle
	{

		public const string ID = ChunkHelper.gAMA;
		double gamma;

		public PngChunkGAMA ( ImageInfo info )
			: base( ID , info )
		{

		}

		public override ChunkOrderingConstraint GetOrderingConstraint () => ChunkOrderingConstraint.BEFORE_PLTE_AND_IDAT;

		public override ChunkRaw CreateRawChunk ()
		{
			ChunkRaw chunk = CreateEmptyChunk(4,true);
			int g = (int)( gamma * 100000 + 0.5d );
			PngHelperInternal.WriteInt4tobytes( g , chunk.Data , 0 );
			return chunk;
		}

		public override void ParseFromRaw ( ChunkRaw chunk )
		{
			if( chunk.Len!=4 ) throw new System.Exception($"bad chunk {chunk}");
			int g = PngHelperInternal.ReadInt4fromBytes(chunk.Data,0);
			gamma = ((double)g) / 100000.0d;
		}

		public override void CloneDataFromRead ( PngChunk other ) => gamma = ((PngChunkGAMA)other).gamma;

		public double GetGamma () => gamma;
		public void SetGamma ( double gamma ) => this.gamma = gamma;
		
	}
}
