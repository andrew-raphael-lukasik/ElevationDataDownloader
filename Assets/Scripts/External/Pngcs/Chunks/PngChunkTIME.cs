namespace Pngcs.Chunks
{
	/// <summary> tIME chunk: http://www.w3.org/TR/PNG/#11tIME </summary>
	public class PngChunkTIME : PngChunkSingle
	{

		public const string ID = ChunkHelper.tIME;
	
		int year, mon, day, hour, min, sec;

		public PngChunkTIME ( ImageInfo info )
			: base( ID , info )
		{

		}

		public override ChunkOrderingConstraint GetOrderingConstraint () => ChunkOrderingConstraint.NONE;

		public override ChunkRaw CreateRawChunk ()
		{
			ChunkRaw c = CreateEmptyChunk( 7 , true );
			byte[] data = c.Data;
			PngHelperInternal.WriteInt2tobytes( year , data , 0 );
			data[2] = (byte)mon;
			data[3] = (byte)day;
			data[4] = (byte)hour;
			data[5] = (byte)min;
			data[6] = (byte)sec;
			return c;
		}

		public override void ParseFromRaw ( ChunkRaw chunk )
		{
			if( chunk.Len!=7 ) throw new System.Exception( $"bad chunk {chunk}" );
			byte[] data = chunk.Data;
			year = PngHelperInternal.ReadInt2fromBytes( data , 0 );
			mon = PngHelperInternal.ReadInt1fromByte( data , 2 );
			day = PngHelperInternal.ReadInt1fromByte( data , 3 );
			hour = PngHelperInternal.ReadInt1fromByte( data , 4 );
			min = PngHelperInternal.ReadInt1fromByte( data , 5 );
			sec = PngHelperInternal.ReadInt1fromByte( data , 6 );
		}

		public override void CloneDataFromRead ( PngChunk other )
		{
			PngChunkTIME x = (PngChunkTIME)other;
			year = x.year;
			mon = x.mon;
			day = x.day;
			hour = x.hour;
			min = x.min;
			sec = x.sec;
		}

		public void SetNow ( int secsAgo )
		{
			var d1 = System.DateTime.Now;
			year = d1.Year;
			mon = d1.Month;
			day = d1.Day;
			hour = d1.Hour;
			min = d1.Minute;
			sec = d1.Second;
		}

		internal void SetYMDHMS ( int yearx , int monx , int dayx , int hourx , int minx , int secx )
		{
			year = yearx;
			mon = monx;
			day = dayx;
			hour = hourx;
			min = minx;
			sec = secx;
		}

		public int[] GetYMDHMS () => new int[] { year, mon, day, hour, min, sec };

		/** format YYYY/MM/DD HH:mm:SS */
		public string GetAsString () => string.Format("%04d/%02d/%02d %02d:%02d:%02d", year, mon, day, hour, min, sec);

	}
}
