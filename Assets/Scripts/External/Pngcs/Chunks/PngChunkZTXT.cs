namespace Pngcs.Chunks
{
	/// <summary> zTXt chunk: http://www.w3.org/TR/PNG/#11zTXt </summary>
	public class PngChunkZTXT : PngChunkTextVar
	{
		
		public const string ID = ChunkHelper.zTXt;

		public PngChunkZTXT ( ImageInfo info )
			: base( ID , info )
		{

		}

		public override ChunkRaw CreateRawChunk ()
		{
			if( key.Length==0 ) throw new System.Exception("Text chunk key must be non empty");
			var ba = new System.IO.MemoryStream();
			ChunkHelper.WriteBytesToStream( ba , ChunkHelper.ToBytes(key) );
			ba.WriteByte(0); // separator
			ba.WriteByte(0); // compression method: 0
			byte[] textbytes = ChunkHelper.CompressBytes( ChunkHelper.ToBytes(val) , true );
			ChunkHelper.WriteBytesToStream( ba , textbytes );
			byte[] b = ba.ToArray();
			ChunkRaw chunk = CreateEmptyChunk( b.Length , false );
			chunk.Data = b;
			return chunk;
		}

		public override void ParseFromRaw ( ChunkRaw c )
		{
			int nullsep = -1;
			var array = c.Data;
			int length = array.Length;
			for( int i=0; i<length ; i++ ) // look for first zero
			{
				if( array[i]!=0 ) continue;
				nullsep = i;
				break;
			}
			if( nullsep<0 || nullsep>length-2 ) throw new System.Exception("bad zTXt chunk: no separator found");
			key = ChunkHelper.ToString( array , 0 , nullsep );
			int compmet = (int)array[nullsep+1];
			if( compmet!=0 ) throw new System.Exception("bad zTXt chunk: unknown compression method");
			byte[] uncomp = ChunkHelper.CompressBytes( array , nullsep+2 , length-nullsep-2 , false ); // uncompress
			val = ChunkHelper.ToString(uncomp);
		}

		public override void CloneDataFromRead ( PngChunk other )
		{
			PngChunkZTXT otherx = (PngChunkZTXT)other;
			key = otherx.key;
			val = otherx.val;
		}

	}
}
