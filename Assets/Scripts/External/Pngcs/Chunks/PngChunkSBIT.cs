namespace Pngcs.Chunks
{
	/// <summary>
	/// sBIT chunk: http://www.w3.org/TR/PNG/#11sBIT
	/// this chunk structure depends on the image type
	/// </summary>
	public class PngChunkSBIT : PngChunkSingle
	{

		public const string ID = ChunkHelper.sBIT;

		//	significant bits
		public int Graysb { get; set; }
		public int Alphasb { get; set; }
		public int Redsb { get; set; }
		public int Greensb { get; set; }
		public int Bluesb { get; set; }

		public PngChunkSBIT ( ImageInfo info )
			: base( ID , info )
		{

		}


		public override ChunkOrderingConstraint GetOrderingConstraint () => ChunkOrderingConstraint.BEFORE_PLTE_AND_IDAT;


		public override void ParseFromRaw ( ChunkRaw chunk )
		{
			byte[] data = chunk.Data;
			if( chunk.Len!=GetLen() ) throw new System.Exception($"bad chunk length {chunk}");
			if( ImgInfo.Greyscale )
			{
				Graysb = PngHelperInternal.ReadInt1fromByte( data , 0 );
				if( ImgInfo.Alpha )
					Alphasb = PngHelperInternal.ReadInt1fromByte( data , 1 );
			}
			else
			{
				Redsb = PngHelperInternal.ReadInt1fromByte( data , 0 );
				Greensb = PngHelperInternal.ReadInt1fromByte( data , 1 );
				Bluesb = PngHelperInternal.ReadInt1fromByte( data , 2 );
				if( ImgInfo.Alpha )
					Alphasb = PngHelperInternal.ReadInt1fromByte( data , 3 );
			}
		}

		public override ChunkRaw CreateRawChunk ()
		{
			ChunkRaw chunk = null;
			chunk = CreateEmptyChunk( GetLen() , true );
			byte[] data = chunk.Data;
			if( ImgInfo.Greyscale )
			{
				data[0] = (byte)Graysb;
				if( ImgInfo.Alpha ) data[1] = (byte)Alphasb;
			}
			else
			{
				data[0] = (byte)Redsb;
				data[1] = (byte)Greensb;
				data[2] = (byte)Bluesb;
				if( ImgInfo.Alpha ) data[3] = (byte)Alphasb;
			}
			return chunk;
		}


		public override void CloneDataFromRead ( PngChunk other )
		{
			PngChunkSBIT otherx = (PngChunkSBIT)other;
			Graysb = otherx.Graysb;
			Redsb = otherx.Redsb;
			Greensb = otherx.Greensb;
			Bluesb = otherx.Bluesb;
			Alphasb = otherx.Alphasb;
		}

		int GetLen ()
		{
			int len = ImgInfo.Greyscale ? 1 : 3;
			if( ImgInfo.Alpha ) len += 1;
			return len;
		}

	}
}
