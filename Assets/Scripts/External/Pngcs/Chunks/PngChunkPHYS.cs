namespace Pngcs.Chunks
{
	/// <summary> pHYs chunk: http://www.w3.org/TR/PNG/#11pHYs </summary>
	public class PngChunkPHYS : PngChunkSingle
	{
		
		public const string ID = ChunkHelper.pHYs;
		public long PixelsxUnitX { get; set; }
		public long PixelsxUnitY { get; set; }

		/// <summary> 0: unknown 1:metre </summary>
		public int Units { get; set; }

		public PngChunkPHYS ( ImageInfo info )
			: base( ID , info )
		{

		}

		public override ChunkOrderingConstraint GetOrderingConstraint () => ChunkOrderingConstraint.BEFORE_IDAT;

		public override ChunkRaw CreateRawChunk ()
		{
			ChunkRaw chunk = CreateEmptyChunk( 9 , true );
			byte[] data = chunk.Data;
			PngHelperInternal.WriteInt4tobytes( (int)PixelsxUnitX , data , 0 );
			PngHelperInternal.WriteInt4tobytes( (int)PixelsxUnitY , data , 4 );
			data[8] = (byte)Units;
			return chunk;
		}

		public override void CloneDataFromRead ( PngChunk other )
		{
			PngChunkPHYS otherx = (PngChunkPHYS)other;
			this.PixelsxUnitX = otherx.PixelsxUnitX;
			this.PixelsxUnitY = otherx.PixelsxUnitY;
			this.Units = otherx.Units;
		}

		public override void ParseFromRaw ( ChunkRaw chunk )
		{
			byte[] data = chunk.Data;
			if( chunk.Len!=9 ) throw new System.Exception($"bad chunk length {chunk}");
			PixelsxUnitX = PngHelperInternal.ReadInt4fromBytes( data , 0 );
			if( PixelsxUnitX<0 )
			{
				PixelsxUnitX += 0x100000000L;
			}
			PixelsxUnitY = PngHelperInternal.ReadInt4fromBytes( data , 4 );
			if( PixelsxUnitY<0 )
			{
				PixelsxUnitY += 0x100000000L;
			}
			Units = PngHelperInternal.ReadInt1fromByte( data , 8 );
		}

		/// <summary> returns -1 if not in meters, or not equal </summary>
		public double GetAsDpi ()
		{
			if( Units!=1 || PixelsxUnitX!=PixelsxUnitY ) return -1;
			return ((double)PixelsxUnitX) * 0.0254d;
		}

		/// <summary> returns -1 if the physicial unit is unknown </summary>
		public double[] GetAsDpi2 ()
		{
			if( Units!=1 ) return new double[]{ -1, -1 };
			return new double[]{ ((double)PixelsxUnitX) * 0.0254, ((double)PixelsxUnitY) * 0.0254 };
		}

		/// <summary> same in both directions </summary>
		public void SetAsDpi ( double dpi )
		{
			Units = 1;
			PixelsxUnitX = (long)(dpi / 0.0254d + 0.5d);
			PixelsxUnitY = PixelsxUnitX;
		}

		public void SetAsDpi2 ( double dpix , double dpiy )
		{
			Units = 1;
			PixelsxUnitX = (long)(dpix / 0.0254 + 0.5);
			PixelsxUnitY = (long)(dpiy / 0.0254 + 0.5);
		}
	 
	}
}
