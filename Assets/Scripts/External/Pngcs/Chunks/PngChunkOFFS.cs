namespace Pngcs.Chunks
{
	/// <summary> oFFs chunk: http://www.libpng.org/pub/png/spec/register/pngext-1.3.0-pdg.html#C.oFFs </summary>
	public class PngChunkOFFS : PngChunkSingle
	{

		public const string ID = "oFFs";

		long posX;
		long posY;
		int units; // 0: pixel 1:micrometer

		public PngChunkOFFS(ImageInfo info)
			: base( ID , info )
		{

		}

		public override ChunkOrderingConstraint GetOrderingConstraint () => ChunkOrderingConstraint.BEFORE_IDAT;

		public override ChunkRaw CreateRawChunk ()
		{
			ChunkRaw chunk = CreateEmptyChunk(9, true);
			byte[] data = chunk.Data;
			PngHelperInternal.WriteInt4tobytes( (int)posX , data , 0 );
			PngHelperInternal.WriteInt4tobytes( (int)posY , data , 4 );
			data[8] = (byte)units;
			return chunk;
		}

		public override void ParseFromRaw ( ChunkRaw chunk )
		{
			byte[] data = chunk.Data;
			if( chunk.Len!=9 ) throw new System.Exception($"bad chunk length {chunk}");
			posX = PngHelperInternal.ReadInt4fromBytes( data , 0 );
			if( posX<0 ) posX += 0x100000000L;
			posY = PngHelperInternal.ReadInt4fromBytes( data , 4 );
			if( posY<0) posY += 0x100000000L;
			units = PngHelperInternal.ReadInt1fromByte( data , 8 );
		}

		public override void CloneDataFromRead ( PngChunk other )
		{
			PngChunkOFFS otherx = (PngChunkOFFS)other;
			this.posX = otherx.posX;
			this.posY = otherx.posY;
			this.units = otherx.units;
		}
		
		/// <summary> 0: pixel, 1:micrometer </summary>
		public int GetUnits () => units;

		/// <summary> 0: pixel, 1:micrometer </summary>
		public void SetUnits ( int units ) => this.units = units;

		public long GetPosX () => posX;

		public void SetPosX ( long posX ) => this.posX = posX;

		public long GetPosY () => posY;

		public void SetPosY ( long posY ) => this.posY = posY;

	}
}
