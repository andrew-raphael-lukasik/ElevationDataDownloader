using IO = System.IO;

namespace Pngcs.Chunks
{
	/// <summary> sPLT chunk: http://www.w3.org/TR/PNG/#11sPLT </summary>
	public class PngChunkSPLT : PngChunkMultiple
	{

		public const string ID = ChunkHelper.sPLT;

		/// <summary> Must be unique in image </summary>
		public string PalName { get; set; }

		/// <summary> 8-16 </summary>
		public int SampleDepth { get; set; }

		/// <summary> 5 elements per entry </summary>
		public int[] Palette { get; set; }

		public PngChunkSPLT ( ImageInfo info )
			: base( ID , info )
		{
			PalName = "";
		}


		public override ChunkOrderingConstraint GetOrderingConstraint () => ChunkOrderingConstraint.BEFORE_IDAT;

		public override ChunkRaw CreateRawChunk ()
		{
			var ba = new IO.MemoryStream();
			ChunkHelper.WriteBytesToStream( ba , ChunkHelper.ToBytes(PalName) );
			ba.WriteByte( 0 ); // separator
			ba.WriteByte( (byte)SampleDepth );
			int nentries = GetNentries();
			for( int n=0 ; n<nentries ; n++ )
			{
				for( int i=0 ; i<4 ; i++ )
				{
					if( SampleDepth==8 )
					{
						PngHelperInternal.WriteByte( ba , (byte)Palette[n*5+i] );
					}
					else
					{
						PngHelperInternal.WriteInt2( ba , Palette[n*5+i] );
					}
				}
				PngHelperInternal.WriteInt2( ba , Palette[n*5+4] );
			}
			byte[] b = ba.ToArray();
			ChunkRaw chunk = CreateEmptyChunk( b.Length , false );
			chunk.Data = b;
			return chunk;
		}

		public override void ParseFromRaw ( ChunkRaw chunk )
		{
			byte[] data = chunk.Data;
			int length = data.Length;
			int t = -1;
			for( int i=0 ; i<length ; i++ )// look for first zero
			{
				if( data[i]==0 )
				{
					t = i;
					break;
				}
			}
			if( t<=0 || t>length-2 ) throw new System.Exception("bad sPLT chunk: no separator found");
			PalName = ChunkHelper.ToString( data , 0 , t );
			SampleDepth = PngHelperInternal.ReadInt1fromByte( data , t+1 );
			t += 2;
			int nentries = ( length-t )/( SampleDepth==8 ? 6 : 10 );
			Palette = new int[ nentries*5 ];
			int r, g, b, a, f, ne;
			ne = 0;
			for( int i=0 ; i<nentries ; i++ )
			{
				if( SampleDepth==8 )
				{
					r = PngHelperInternal.ReadInt1fromByte( data , t++ );
					g = PngHelperInternal.ReadInt1fromByte( data , t++ );
					b = PngHelperInternal.ReadInt1fromByte( data , t++ );
					a = PngHelperInternal.ReadInt1fromByte( data , t++ );
				}
				else
				{
					r = PngHelperInternal.ReadInt2fromBytes( data , t );
					t += 2;
					g = PngHelperInternal.ReadInt2fromBytes( data , t );
					t += 2;
					b = PngHelperInternal.ReadInt2fromBytes( data , t );
					t += 2;
					a = PngHelperInternal.ReadInt2fromBytes( data , t );
					t += 2;
				}
				f = PngHelperInternal.ReadInt2fromBytes( data , t );
				t += 2;
				Palette[ne++] = r;
				Palette[ne++] = g;
				Palette[ne++] = b;
				Palette[ne++] = a;
				Palette[ne++] = f;
			}
		}

		public override void CloneDataFromRead ( PngChunk other )
		{
			PngChunkSPLT otherx = (PngChunkSPLT)other;
			PalName = otherx.PalName;
			SampleDepth = otherx.SampleDepth;
			Palette = new int[otherx.Palette.Length];
			System.Array.Copy( otherx.Palette , 0 , Palette , 0 , Palette.Length );

		}

		public int GetNentries () => Palette.Length/5;

	}
}
