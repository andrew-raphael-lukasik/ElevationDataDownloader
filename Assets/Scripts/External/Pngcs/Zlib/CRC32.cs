namespace Pngcs.Zlib
{
	// based on http://damieng.com/blog/2006/08/08/calculating_crc32_in_c_and_net
	public class CRC32
	{

		const uint defaultPolynomial = 0xedb88320;
		const uint defaultSeed = 0xffffffff;
		static uint[] defaultTable;

		uint hash;
		uint seed;
		uint[] table;

		public CRC32 ()
			: this( defaultPolynomial , defaultSeed )
		{

		}
		public CRC32 ( uint polynomial , uint seed )
		{
			table = InitializeTable( polynomial );
			this.seed = seed;
			this.hash = seed;
		}

		public void Update ( byte[] buffer ) => Update( buffer , 0 , buffer.Length );

		public void Update ( byte[] buffer , int start , int length )
		{
			for( int i=0, j=start ; i<length ; i++, j++)
			{
				unchecked
				{
					hash = (hash>>8) ^ table[buffer[j] ^ hash & 0xff];
				}
			}
		}

		public uint GetValue () => ~hash;

		public void Reset () => this.hash = seed;
	
		static uint[] InitializeTable ( uint polynomial )
		{
			if( polynomial==defaultPolynomial && defaultTable!=null ) return defaultTable;
			uint[] createTable = new uint[256];
			for( int i=0 ; i<256 ; i++ )
			{
				uint entry = (uint)i;
				for( int j=0 ; j<8 ; j++ )
				{
					entry = (entry & 1)==1
						? (entry>>1)^polynomial
						: entry>>1;
				}
				createTable[i] = entry;
			}
			if( polynomial==defaultPolynomial ) defaultTable = createTable;
			return createTable;
		}

	}
}
