namespace Pngcs.Chunks
{
	/// <summary> Unknown (for our chunk factory) chunk type. </summary>
	public class PngChunkUNKNOWN : PngChunkMultiple
	{

		byte[] data;

		public PngChunkUNKNOWN ( string id , ImageInfo info )
			: base( id , info )
		{

		}

		PngChunkUNKNOWN ( PngChunkUNKNOWN c , ImageInfo info )
			: base( c.Id , info )
		{
			System.Array.Copy( c.data , 0 , data , 0 , c.data.Length );
		}

		public override ChunkOrderingConstraint GetOrderingConstraint () => ChunkOrderingConstraint.NONE;


		public override ChunkRaw CreateRawChunk ()
		{
			ChunkRaw p = CreateEmptyChunk( data.Length , false );
			p.Data = this.data;
			return p;
		}

		public override void ParseFromRaw ( ChunkRaw chunk ) => data = chunk.Data;

		/* does not copy! */
		public byte[] GetData () => data;

		/* does not copy! */
		public void SetData ( byte[] data_0 ) => this.data = data_0;

		public override void CloneDataFromRead ( PngChunk other )
		{
			// THIS SHOULD NOT BE CALLED IF ALREADY CLONED WITH COPY CONSTRUCTOR
			PngChunkUNKNOWN c = (PngChunkUNKNOWN)other;
			data = c.data; // not deep copy
		}
		
	}
}
