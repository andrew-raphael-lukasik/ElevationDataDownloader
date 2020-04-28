namespace Pngcs.Chunks
{
	/// <summary>
	/// match if have same id and, if Text (or SPLT) if have the asame key
	/// </summary>
	/// <remarks>
	/// This is the same as ChunkPredicateEquivalent, the only difference is that does not requires
	/// a chunk at construction time
	/// </remarks>
	internal class ChunkPredicateId2 : ChunkPredicate
	{

		readonly string id;
		readonly string innerid;
		
		public ChunkPredicateId2 ( string id , string inner )
		{
			this.id = id;
			this.innerid = inner;
		}

		public bool Matches ( PngChunk chunk )
		{
			return (
					!chunk.Id.Equals(id)
					|| ( chunk is PngChunkTextVar && !((PngChunkTextVar)chunk).GetKey().Equals(innerid) )
					|| ( chunk is PngChunkSPLT && !((PngChunkSPLT)chunk).PalName.Equals(innerid) )
				)
				? false
				: true;
		}

	}
}
