using IO = System.IO;

namespace Pngcs.Chunks
{
	/// <summary> iTXt chunk:  http://www.w3.org/TR/PNG/#11iTXt </summary>
	/// </remarks> One of the three text chunks </remarks>
	public class PngChunkITXT : PngChunkTextVar
	{

		public const string ID = ChunkHelper.iTXt;
		bool compressed = false;
		string langTag = "";
		string translatedTag = "";

		public PngChunkITXT ( ImageInfo info )
			: base( ID , info )
		{

		}

		public override ChunkRaw CreateRawChunk ()
		{
			if( key.Length==0 ) throw new System.Exception("Text chunk key must be non empty");
			var ba = new IO.MemoryStream();
			ChunkHelper.WriteBytesToStream( ba , ChunkHelper.ToBytes(key) );
			ba.WriteByte( 0 ); // separator
			ba.WriteByte( compressed ? (byte)1 : (byte)0 );
			ba.WriteByte( 0 ); // compression method (always 0)
			ChunkHelper.WriteBytesToStream( ba , ChunkHelper.ToBytes(langTag) );
			ba.WriteByte( 0 ); // separator
			ChunkHelper.WriteBytesToStream( ba , ChunkHelper.ToBytesUTF8(translatedTag) );
			ba.WriteByte( 0 ); // separator
			byte[] textbytes = ChunkHelper.ToBytesUTF8( val );
			if( compressed ) textbytes = ChunkHelper.CompressBytes( textbytes , true );
			ChunkHelper.WriteBytesToStream( ba , textbytes );
			byte[] b = ba.ToArray();
			ChunkRaw chunk = CreateEmptyChunk( b.Length , false );
			chunk.Data = b;
			return chunk;
		}

		public override void ParseFromRaw ( ChunkRaw chunk )
		{
			byte[] data = chunk.Data;
			int length = data.Length;
			int nullsFound = 0;
			int[] nullsIdx = new int[3];
			for( int k=0 ; k<length ; k++ )
			{
				if( data[k]!=0 ) continue;
				nullsIdx[nullsFound] = k;
				nullsFound++;
				if( nullsFound==1 ) k += 2;
				if( nullsFound==3 ) break;
			}
			if( nullsFound!=3 ) throw new System.Exception("Bad formed PngChunkITXT chunk");
			key = ChunkHelper.ToString( data , 0 , nullsIdx[0] );
			int i = nullsIdx[0] + 1;
			compressed = data[i]==0 ? false : true;
			i++;
			if( compressed && data[i]!=0 ) throw new System.Exception("Bad formed PngChunkITXT chunk - bad compression method ");
			langTag = ChunkHelper.ToString( data , i , nullsIdx[1]-i );
			translatedTag = ChunkHelper.ToStringUTF8( data , nullsIdx[1]+1 , nullsIdx[2]-nullsIdx[1]-1 );
			i = nullsIdx[2] + 1;
			if( compressed )
			{
				byte[] bytes = ChunkHelper.CompressBytes( data , i , length-i , false );
				val = ChunkHelper.ToStringUTF8(bytes);
			}
			else
			{
				val = ChunkHelper.ToStringUTF8( data , i , length-i );
			}
		}

		public override void CloneDataFromRead ( PngChunk other )
		{
			PngChunkITXT otherx = (PngChunkITXT)other;
			key = otherx.key;
			val = otherx.val;
			compressed = otherx.compressed;
			langTag = otherx.langTag;
			translatedTag = otherx.translatedTag;
		}

		public bool IsCompressed () => compressed;

		public void SetCompressed ( bool compressed ) => this.compressed = compressed;

		public string GetLangtag () => langTag;

		public void SetLangtag ( string langtag ) => this.langTag = langtag;

		public string GetTranslatedTag () => translatedTag;

		public void SetTranslatedTag ( string translatedTag ) => this.translatedTag = translatedTag;
		
	}
}
