namespace Pngcs.Chunks
{
	/// <summary> cHRM chunk, see http://www.w3.org/TR/PNG/#11cHRM </summary>
	public class PngChunkCHRM : PngChunkSingle
	{

		public const string ID = ChunkHelper.cHRM;

		double whitex, whitey;
		double redx, redy;
		double greenx, greeny;
		double bluex, bluey;

		public PngChunkCHRM ( ImageInfo info )
			: base( ID , info )
		{

		}

		public override ChunkOrderingConstraint GetOrderingConstraint () => ChunkOrderingConstraint.AFTER_PLTE_BEFORE_IDAT;

		public override ChunkRaw CreateRawChunk ()
		{
			ChunkRaw chunk = null;
			chunk = CreateEmptyChunk( 32 , true );
			byte[] data = chunk.Data;
			PngHelperInternal.WriteInt4tobytes( PngHelperInternal.DoubleToInt100000(whitex), data , 0 );
			PngHelperInternal.WriteInt4tobytes( PngHelperInternal.DoubleToInt100000(whitey), data , 4 );
			PngHelperInternal.WriteInt4tobytes( PngHelperInternal.DoubleToInt100000(redx), data , 8 );
			PngHelperInternal.WriteInt4tobytes( PngHelperInternal.DoubleToInt100000(redy), data , 12 );
			PngHelperInternal.WriteInt4tobytes( PngHelperInternal.DoubleToInt100000(greenx), data , 16 );
			PngHelperInternal.WriteInt4tobytes( PngHelperInternal.DoubleToInt100000(greeny), data , 20 );
			PngHelperInternal.WriteInt4tobytes( PngHelperInternal.DoubleToInt100000(bluex), data , 24 );
			PngHelperInternal.WriteInt4tobytes( PngHelperInternal.DoubleToInt100000(bluey), data , 28 );
			return chunk;
		}

		public override void ParseFromRaw ( ChunkRaw chunk )
		{
			if( chunk.Len!=32 ) throw new System.Exception($"bad chunk {chunk}");
			byte[] data = chunk.Data;
			whitex = PngHelperInternal.IntToDouble100000( PngHelperInternal.ReadInt4fromBytes(data,0) );
			whitey = PngHelperInternal.IntToDouble100000( PngHelperInternal.ReadInt4fromBytes(data,4) );
			redx = PngHelperInternal.IntToDouble100000( PngHelperInternal.ReadInt4fromBytes(data,8) );
			redy = PngHelperInternal.IntToDouble100000( PngHelperInternal.ReadInt4fromBytes(data,12) );
			greenx = PngHelperInternal.IntToDouble100000( PngHelperInternal.ReadInt4fromBytes( data,16) );
			greeny = PngHelperInternal.IntToDouble100000( PngHelperInternal.ReadInt4fromBytes(data,20) );
			bluex = PngHelperInternal.IntToDouble100000( PngHelperInternal.ReadInt4fromBytes(data,24) );
			bluey = PngHelperInternal.IntToDouble100000( PngHelperInternal.ReadInt4fromBytes(data,28) );
		}

		public override void CloneDataFromRead ( PngChunk other )
		{
			PngChunkCHRM otherx = (PngChunkCHRM)other;
			whitex = otherx.whitex;
			whitey = otherx.whitex;
			redx = otherx.redx;
			redy = otherx.redy;
			greenx = otherx.greenx;
			greeny = otherx.greeny;
			bluex = otherx.bluex;
			bluey = otherx.bluey;
		}

		public void SetChromaticities ( double whitex , double whitey , double redx , double redy , double greenx , double greeny , double bluex , double bluey )
		{
			this.whitex = whitex;
			this.redx = redx;
			this.greenx = greenx;
			this.bluex = bluex;
			this.whitey = whitey;
			this.redy = redy;
			this.greeny = greeny;
			this.bluey = bluey;
		}

		public double[] GetChromaticities () => new double[] { whitex, whitey, redx, redy, greenx, greeny, bluex, bluey };
		
	}
}
