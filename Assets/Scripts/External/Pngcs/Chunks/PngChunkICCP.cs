namespace Pngcs.Chunks
{
	/// <summary> iCCP Chunk: see http://www.w3.org/TR/PNG/#11iCCP </summary>
	public class PngChunkICCP : PngChunkSingle
	{
		
		public const string ID = ChunkHelper.iCCP;
		string profileName;
		byte[] compressedProfile;

		public PngChunkICCP ( ImageInfo info )
			: base( ID , info )
		{

		}

		public override ChunkOrderingConstraint GetOrderingConstraint () => ChunkOrderingConstraint.BEFORE_PLTE_AND_IDAT;


		public override ChunkRaw CreateRawChunk ()
		{
			ChunkRaw chunk = CreateEmptyChunk( profileName.Length+compressedProfile.Length+2 , true );
			byte[] data = chunk.Data;
			System.Array.Copy( Chunks.ChunkHelper.ToBytes(profileName) , 0 , data , 0 , profileName.Length );
			data[profileName.Length] = 0;
			data[profileName.Length + 1] = 0;
			System.Array.Copy( compressedProfile , 0 , data , profileName.Length+2 , compressedProfile.Length );
			return chunk;
		}

		public override void ParseFromRaw ( ChunkRaw chunk )
		{
			byte[] data = chunk.Data;
			int pos0 = Chunks.ChunkHelper.PosNullByte( data );
			profileName = PngHelperInternal.charsetLatin1.GetString( data , 0 , pos0 );
			int comp = data[pos0+1] & 0xff;
			if( comp!=0 ) throw new System.Exception("bad compression for ChunkTypeICCP");
			int compdatasize = data.Length - (pos0 + 2);
			compressedProfile = new byte[compdatasize];
			System.Array.Copy( data , pos0+2 , compressedProfile , 0 , compdatasize );
		}

		public override void CloneDataFromRead ( PngChunk other )
		{
			PngChunkICCP otherx = (PngChunkICCP)other;
			profileName = otherx.profileName;
			compressedProfile = new byte[otherx.compressedProfile.Length];
			System.Array.Copy( otherx.compressedProfile , compressedProfile , compressedProfile.Length );
		}

		/// <summary> Sets profile name and profile </summary>
		/// <param name="name">profile name </param>
		/// <param name="profile">profile (latin1 string)</param>
		public void SetProfileNameAndContent ( string name , string profile ) => SetProfileNameAndContent(name, ChunkHelper.ToBytes(profileName));

		/// <summary> Sets profile name and profile </summary>
		/// <param name="name">profile name </param>
		/// <param name="profile">profile (uncompressed)</param>
		public void SetProfileNameAndContent ( string name , byte[] profile )
		{
			profileName = name;
			compressedProfile = ChunkHelper.CompressBytes(profile, true);
		}
			
		public string GetProfileName () => profileName;

		/// <summary> This uncompresses the string! </summary>
		public byte[] GetProfile () => ChunkHelper.CompressBytes( compressedProfile , false );

		public string GetProfileAsString () => ChunkHelper.ToString(GetProfile());

	}
}
