namespace Pngcs.Chunks
{
	/// <summary>
	/// Match if have same Chunk Id
	/// </summary>
	internal class ChunkPredicateId : ChunkPredicate
	{
		readonly string id;
		public ChunkPredicateId ( string id ) => this.id = id;
		public bool Matches ( PngChunk chunk ) => chunk.Id.Equals(id);
	}
}
