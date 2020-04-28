namespace Pngcs.Chunks
{
	/// <summary> tRNS chunk: http://www.w3.org/TR/PNG/#11tRNS </summary>
	public class PngChunkTRNS : PngChunkSingle
	{

		public const string ID = ChunkHelper.tRNS;
	
		// this chunk structure depends on the image type
		// only one of these is meaningful
		public int Gray;

		RGB<int> Rgb;

		public int[] PaletteAlpha;

		public PngChunkTRNS ( ImageInfo info )
			: base( ID , info )
		{

		}

		public override ChunkOrderingConstraint GetOrderingConstraint () => ChunkOrderingConstraint.AFTER_PLTE_BEFORE_IDAT;

		public override ChunkRaw CreateRawChunk ()
		{
			ChunkRaw chunk = null;
			if( ImgInfo.Greyscale )
			{
				chunk = CreateEmptyChunk( 2 , true );
				byte[] data = chunk.Data;
				PngHelperInternal.WriteInt2tobytes( Gray , data , 0 );
			}
			else if( ImgInfo.Indexed )
			{
				chunk = CreateEmptyChunk( PaletteAlpha.Length , true );
				byte[] data = chunk.Data;
				int length = data.Length;

				for( int n=0 ; n<length ; n++ )
					data[n] = (byte)PaletteAlpha[n];
			}
			else
			{
				chunk = CreateEmptyChunk( 6 , true );
				byte[] data = chunk.Data;

				PngHelperInternal.WriteInt2tobytes( Rgb.R , data , 0 );
				PngHelperInternal.WriteInt2tobytes( Rgb.G , data , 0 );
				PngHelperInternal.WriteInt2tobytes( Rgb.B , data , 0 );
			}
			return chunk;
		}

		public override void ParseFromRaw ( ChunkRaw chunk )
		{
			byte[] data = chunk.Data;
			if( ImgInfo.Greyscale )
			{
				Gray = PngHelperInternal.ReadInt2fromBytes( data , 0 );
			}
			else if( ImgInfo.Indexed )
			{
				int numEntries = data.Length;
				PaletteAlpha = new int[numEntries];
				for( int n=0 ; n<numEntries ; n++ )
					PaletteAlpha[n] = (int)( data[n] & 0xff );
			}
			else
			{
				Rgb = new RGB<int>{
					R = PngHelperInternal.ReadInt2fromBytes( data , 0 ) ,
					G = PngHelperInternal.ReadInt2fromBytes( data , 2 ) ,
					B = PngHelperInternal.ReadInt2fromBytes( data , 4 )
				};
			}
		}

		public override void CloneDataFromRead ( PngChunk other )
		{
			PngChunkTRNS otherx = (PngChunkTRNS)other;
			Gray = otherx.Gray;
			Rgb = otherx.Rgb;
			if( otherx.PaletteAlpha!=null )
			{
				PaletteAlpha = new int[ otherx.PaletteAlpha.Length ];
				System.Array.Copy( otherx.PaletteAlpha , 0 , PaletteAlpha , 0 , PaletteAlpha.Length );
			}
		}

		public void SetRGB ( RGB<int> rgb )
		{
			#if DEBUG
			if( ImgInfo.Greyscale || ImgInfo.Indexed ) throw new System.Exception("only rgb or rgba images support this");
			#endif
			this.Rgb = rgb;
		}
		public void SetRGB ( int r , int g , int b ) => SetRGB( new RGB<int>{ R=r , G=g , B=b } );

		/// <summary> utiliy method : to use when only one pallete index is set as totally transparent </summary>
		public void setIndexEntryAsTransparent ( int palAlphaIndex )
		{
			#if DEBUG
			if( !ImgInfo.Indexed ) throw new System.Exception("only indexed images support this");
			#endif
			PaletteAlpha = new int[]{ palAlphaIndex+1 };
			for( int i=0 ; i<palAlphaIndex ; i++ )
				PaletteAlpha[i] = 255;
			PaletteAlpha[ palAlphaIndex ] = 0;
		}
		
	}
}
