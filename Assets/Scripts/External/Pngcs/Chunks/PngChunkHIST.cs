namespace Pngcs.Chunks
{
	/// <summary>
	/// hIST chunk, see http://www.w3.org/TR/PNG/#11hIST
	/// Only for palette images
	/// </summary>
	public class PngChunkHIST : PngChunkSingle
	{

		public readonly static string ID = ChunkHelper.hIST;

		int[] hist = new int[0]; // should have same lenght as palette

		public PngChunkHIST ( ImageInfo info )
			: base( ID , info )
		{

		}

		public override ChunkOrderingConstraint GetOrderingConstraint () => ChunkOrderingConstraint.AFTER_PLTE_BEFORE_IDAT;

		public override ChunkRaw CreateRawChunk ()
		{
			ChunkRaw chunk = null;
			if( !ImgInfo.Indexed ) throw new System.Exception("only indexed images accept a HIST chunk");
			chunk = CreateEmptyChunk( hist.Length*2 , true );
			byte[] data = chunk.Data;
			for( int i=0 ; i<hist.Length ; i++ )
			{
				PngHelperInternal.WriteInt2tobytes( hist[i] , data , i*2 );
			}
			return chunk;
		}

		public override void ParseFromRaw ( ChunkRaw chunk )
		{
			if( !ImgInfo.Indexed ) throw new System.Exception("only indexed images accept a HIST chunk");
			byte[] data = chunk.Data;
			int nentries = data.Length/2;
			hist = new int[nentries];
			for( int i=0 ; i<hist.Length ; i++ )
			{
				hist[i] = PngHelperInternal.ReadInt2fromBytes( data , i*2 );
			}
		}

		public override void CloneDataFromRead ( PngChunk other)
		{
			PngChunkHIST otherx = (PngChunkHIST)other;
			hist = new int[otherx.hist.Length];
			System.Array.Copy( otherx.hist , 0 , this.hist , 0 , otherx.hist.Length );
		}

		public int[] GetHist () => hist;

		/// <summary> should have same length as palette </summary>
		public void SetHist ( int[] hist ) => this.hist = hist;

	}
}
