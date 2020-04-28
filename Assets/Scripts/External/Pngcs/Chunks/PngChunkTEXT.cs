namespace Pngcs.Chunks
{
	/// <summary> tEXt chunk: latin1 uncompressed text </summary>
	public class PngChunkTEXT : PngChunkTextVar
	{
		public const string ID = ChunkHelper.tEXt;

		public PngChunkTEXT ( ImageInfo info )
			: base( ID , info )
		{

		}

		public override ChunkRaw CreateRawChunk ()
		{
			if( key.Length==0 ) throw new System.Exception("Text chunk key must be non empty");
			
			byte[] b1 = PngHelperInternal.charsetLatin1.GetBytes( key );
			byte[] b2 = PngHelperInternal.charsetLatin1.GetBytes( val );

			ChunkRaw chunk = CreateEmptyChunk( b1.Length+b2.Length+1 , true );
			byte[] data = chunk.Data;

			System.Array.Copy( b1 , 0 , data , 0 , b1.Length );
			data[b1.Length] = 0;
			System.Array.Copy( b2 , 0 , data , b1.Length+1 , b2.Length );

			return chunk;
		}

		public override void ParseFromRaw ( ChunkRaw c )
		{
			byte[] data = c.Data;
			int length = data.Length;
			int i;
			for( i=0 ; i<length ; i++ )
				if( data[i]==0 ) break;
			key = PngHelperInternal.charsetLatin1.GetString( data , 0 , i );
			i++;
			val = i<length ? PngHelperInternal.charsetLatin1.GetString( data , i , length-i ) : "";
		}

		public override void CloneDataFromRead ( PngChunk other )
		{
			PngChunkTEXT otherx = (PngChunkTEXT)other;
			key = otherx.key;
			val = otherx.val;
		}
		
	}
}
