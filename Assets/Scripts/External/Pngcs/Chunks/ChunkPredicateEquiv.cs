namespace Pngcs.Chunks
{
	/// <summary>
	/// An ad-hoc criterion, perhaps useful, for equivalence.
	/// <see cref="ChunkHelper.Equivalent(PngChunk,PngChunk)"/> 
	/// </summary>
	internal class ChunkPredicateEquiv : ChunkPredicate
	{

		readonly PngChunk chunk;

		/// <summary> Creates predicate based of reference chunk </summary>
		public ChunkPredicateEquiv ( PngChunk chunk ) => this.chunk = chunk;

		/// <summary> Check for match </summary>
		public bool Matches ( PngChunk c ) => ChunkHelper.Equivalent(c, chunk);

	}

}
