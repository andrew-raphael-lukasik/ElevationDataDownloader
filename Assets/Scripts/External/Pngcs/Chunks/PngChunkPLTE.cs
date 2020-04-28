namespace Pngcs.Chunks
{
	/// <summary> PLTE Palette chunk: this is the only optional critical chunk http://www.w3.org/TR/PNG/#11PLTE </summary>
	public class PngChunkPLTE : PngChunkSingle
	{

		public const string ID = ChunkHelper.PLTE;

		int numEntries = 0;
		public int NumEntries => numEntries;

		int[] entries;

		public PngChunkPLTE ( ImageInfo info )
			: base( ID , info )
		{
			this.numEntries = 0;
		}


		public override ChunkOrderingConstraint GetOrderingConstraint () => ChunkOrderingConstraint.NA;

		public override ChunkRaw CreateRawChunk ()
		{
			int len = 3 * numEntries;
			int[] rgb = new int[3];
			ChunkRaw c = CreateEmptyChunk( len , true );
			byte[] data = c.Data;
			for( int n=0 , i=0 ; n<numEntries ; n++ )
			{
				GetEntryRgb( n , rgb , 0 );
				data[i++] = (byte)rgb[0];
				data[i++] = (byte)rgb[1];
				data[i++] = (byte)rgb[2];
			}
			return c;
		}

		public override void ParseFromRaw ( ChunkRaw chunk )
		{
			SetNentries( chunk.Len/3 );
			byte[] data = chunk.Data;
			for( int n=0 , i=0 ; n<numEntries ; n++ )
			{
				SetEntry(
					n ,
					(int)( data[i++] & 0xff ) ,
					(int)( data[i++] & 0xff ) ,
					(int)( data[i++] & 0xff )
				);
			}
		}

		public override void CloneDataFromRead ( PngChunk other )
		{
			PngChunkPLTE otherx = (PngChunkPLTE)other;
			this.SetNentries( otherx.GetNentries() );
			System.Array.Copy( otherx.entries , 0 , entries , 0 , numEntries );
		}

		/// <summary> Also allocates array </summary>
		/// <param name="numEntries">1-256</param>
		public void SetNentries ( int numEntries )
		{
			this.numEntries = numEntries;
			if( numEntries<1 || numEntries>256 ) throw new System.Exception($"invalid pallette - nentries={numEntries}");
			if( entries==null || entries.Length!=numEntries )// alloc
				entries = new int[numEntries];
		}

		public int GetNentries () => numEntries;

		public void SetEntry ( int n , int r , int g , int b ) => entries[n] = (r<<16) | (g<<8) | b;

		/// <summary> as packed RGB8 </summary>
		public int GetEntry ( int n ) => entries[n];

	   
		/// <summary> Gets n'th entry, filling 3 positions of given array, at given offset </summary>
		public void GetEntryRgb ( int index , int[] rgb , int offset )
		{
			int v = entries[index];
			rgb[offset] = ((v & 0xff0000)>>16);
			rgb[offset+1] = ((v & 0xff00)>>8);
			rgb[offset+2] = (v & 0xff);
		}
		/// <summary> Gets n'th entry at given offset </summary>
		public RGB<int> GetEntryRgb ( int index , int offset )
		{
			int v = entries[index];
			return new RGB<int>{
				R = (v & 0xff0000)>>16 ,
				G = (v & 0xff00)>>8 ,
				B = v & 0xff
			};
		}

		/// <summary> minimum allowed bit depth, given palette size </summary>
		/// <returns> 1-2-4-8 </returns>
		public int MinBitDepth ()
		{
			if( numEntries<=2 ) return 1;
			else if( numEntries<=4 ) return 2;
			else if( numEntries<=16 ) return 4;
			else return 8;
		}
	}

}
