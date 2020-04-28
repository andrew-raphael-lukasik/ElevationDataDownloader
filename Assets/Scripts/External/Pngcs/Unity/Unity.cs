using System.Threading.Tasks;
using IO = System.IO;

using UnityEngine;

namespace Pngcs.Unity
{
	public static class PNG
	{

		/// <summary> Write PNG to file, async </summary>
		public static async Task WriteAsync
		(
			Texture2D texture ,
			string filePath
		)
		{
			try
			{
				Color[] pixels = texture.GetPixels();
				int width = texture.width;
				int height = texture.height;
				var format = texture.format;
				int bitDepth = GetBitDepth( format );
				bool alpha = GetIsAlpha( format );
				bool greyscale = GetIsGreyscale( format );
				
				await WriteAsync(
					pixels:	 pixels ,
					width:	  width ,
					height:	 height ,
					bitDepth:   bitDepth ,
					alpha:	  alpha ,
					greyscale:  greyscale ,
					filePath:   filePath
				);
			}
			catch( System.Exception ex ) { Debug.LogException(ex); await Task.CompletedTask; }//kills debugger execution loop on exception
			finally { await Task.CompletedTask; }
		}
		public static async Task WriteAsync
		(
			Color[] pixels ,
			int width ,
			int height ,
			int bitDepth ,
			bool alpha ,
			bool greyscale ,
			string filePath
		)
		{
			try
			{
				await Task.Run( ()=> {
					Write(
						pixels:	 pixels ,
						width:	  width ,
						height:	 height ,
						bitDepth:   bitDepth ,
						alpha:	  alpha ,
						greyscale:  greyscale ,
						filePath:   filePath
					);
					return Task.CompletedTask;
				} );
			}
			catch( System.Exception ex ) { Debug.LogException(ex); await Task.CompletedTask; }//kills debugger execution loop on exception
			finally { await Task.CompletedTask; }
		}

		/// <summary>
		/// Write large texture to PNG, async.
		/// Execute from Main Thread.
		/// </summary>
		public static async Task WriteLargeAsync
		(
			Texture2D texture ,
			string filePath
		)
		{
			await WriteLargeAsync( texture , filePath , FillLine );
		}

		/// <summary>
		/// Write large texture to PNG, async. FillLine( texture , image line , image info , row ).
		/// Execute from Main Thread.
		/// </summary>
		public static async Task WriteLargeAsync
		(
			Texture2D texture ,
			string filePath ,
			System.Func<Texture2D,int[],ImageInfo,int,Task> fillLine
		)
		{
			try
			{
				var imageInfo = new ImageInfo(
					texture.width ,
					texture.height ,
					GetBitDepth( texture.format ) ,
					GetIsAlpha( texture.format ) ,
					GetIsGreyscale(texture. format ) ,
					false//not implemented here yet//bitDepth==4
				);
					
				// open image for writing:
				PngWriter writer = FileHelper.CreatePngWriter( filePath , imageInfo , true );
				
				// add some optional metadata (chunks)
				var meta = writer.GetMetadata();
				meta.SetTimeNow( 0 );// 0 seconds fron now = now

				int numRows = imageInfo.Rows;
				int numCols = imageInfo.Cols;
				int channels = imageInfo.Channels;
				for( int row=0 ; row<numRows ; row++ )
				{
					//fill line on main or different thread:
					int[] samples = new int[ imageInfo.SamplesPerRow ];
					await fillLine( texture , samples , imageInfo , row );
					
					//write line on another thread:
					ImageLine line = new ImageLine( imageInfo , ImageLine.ESampleType.INT , false , samples , null , row );
					await Task.Run( ()=> {
						writer.WriteRow( line , row );
						return Task.CompletedTask;
					} );
				}
				writer.End();
			}
			catch( System.Exception ex ) { Debug.LogException(ex); await Task.CompletedTask; }//kills debugger execution loop on exception
			finally { await Task.CompletedTask; }
		}

		/// <summary> Write PNG to file </summary>
		public static void Write
		(
			Texture2D texture ,
			string filePath
		)
		{
			if( texture.isReadable==false )
			{
				Debug.LogError( $"'{ texture.name }' is not readable so the texture memory can not be accessed from scripts. You can make the texture readable in the Texture Import Settings." );
				return;
			}
			var pixels = texture.GetPixels();
			var format = texture.format;
			int bitDepth = GetBitDepth( format );
			bool alpha = GetIsAlpha( format );
			bool greyscale = GetIsGreyscale( format );
			Write(
				pixels:	 pixels ,
				width:	  texture.width ,
				height:	 texture.height ,
				bitDepth:   bitDepth ,
				alpha:	  alpha ,
				greyscale:  greyscale ,
				filePath:   filePath
			);
		}
		public static void Write
		(
			Color[] pixels ,
			int width ,
			int height ,
			int bitDepth ,
			bool alpha ,
			bool greyscale ,
			string filePath
		)
		{
			var imageInfo = new ImageInfo(
				width ,
				height ,
				bitDepth ,
				alpha ,
				greyscale ,
				false//not implemented here yet//bitDepth==4
			);
				
			// open image for writing:
			PngWriter writer = FileHelper.CreatePngWriter( filePath , imageInfo , true );
			
			// add some optional metadata (chunks)
			var meta = writer.GetMetadata();
			meta.SetTimeNow( 0 );// 0 seconds fron now = now

			int numRows = imageInfo.Rows;
			int numCols = imageInfo.Cols;
			int channels = imageInfo.Channels;
			for( int row=0 ; row<numRows ; row++ )
			{
				int[] ints = new int[ imageInfo.SamplesPerRow ];

				//fill line:
				if( greyscale==false )
				{
					if( alpha )
					{
						for( int col=0 ; col<numCols ; col++ )
						{
							RGBA<int> rgba = ToRGBA( pixels[ IndexPngToTexture( row , col , numRows , numCols ) ] , bitDepth );
							ImageLineHelper.SetPixel( ints , rgba , col , channels );
						}
					}
					else
					{
						for( int col=0 ; col<numCols ; col++ )
						{
							RGB<int> rgb = ToRGB( pixels[ IndexPngToTexture( row , col , numRows , numCols ) ] , bitDepth );
							ImageLineHelper.SetPixel( ints , rgb , col , channels );
						}
					}
				}
				else
				{
					if( alpha==false )
					{
						for( int col=0 ; col<numCols ; col++ )
						{
							int R = ToInt( pixels[ IndexPngToTexture( row , col , numRows , numCols ) ].r , bitDepth );
							ImageLineHelper.SetPixel( ints , R , col , channels );
						}
					}
					else
					{
						for( int col=0 ; col<numCols ; col++ )
						{
							int A = ToInt( pixels[ IndexPngToTexture( row , col , numRows , numCols ) ].a , bitDepth );
							ImageLineHelper.SetPixel( ints , A , col , channels );
						}
					}
				}
				
				//write line:
				ImageLine imageline = new ImageLine( imageInfo , ImageLine.ESampleType.INT , false , ints , null , row );
				writer.WriteRow( imageline , row );
			}
			writer.End();
		}

		static async Task FillLine
		(
			Texture2D texture ,
			int[] samples ,
			ImageInfo imageInfo ,
			int row
		)
		{
			int numCols = imageInfo.Cols;
			int numRows = imageInfo.Rows;
			int bitDepth = imageInfo.BitDepth;
			bool alpha = imageInfo.Alpha;
			bool greyscale = imageInfo.Greyscale;
			int channels = imageInfo.Channels;

			//fill line:
			Color[] pixels = texture.GetPixels( 0 , row , numCols , 1 );
			if( greyscale==false )
			{
				if( alpha )
				{
					for( int col=0 ; col<numCols ; col++ )
					{
						RGBA<int> rgba = ToRGBA( pixels[col] , bitDepth );
						ImageLineHelper.SetPixel( samples , rgba , col , channels );
					}
				}
				else
				{
					for( int col=0 ; col<numCols ; col++ )
					{
						RGB<int> rgb = ToRGB( pixels[col] , bitDepth );
						ImageLineHelper.SetPixel( samples , rgb , col , channels );
					}
				}
			}
			else
			{
				if( alpha==false )
				{
					for( int col=0 ; col<numCols ; col++ )
					{
						int R = ToInt( pixels[col].r , bitDepth );
						ImageLineHelper.SetPixel( samples , R , col , channels );
					}
				}
				else
				{
					for( int col=0 ; col<numCols ; col++ )
					{
						int A = ToInt( pixels[col].a , bitDepth );
						ImageLineHelper.SetPixel( samples , A , col , channels );
					}
				}
			}

			await Task.CompletedTask;
		}

		/// <summary> Writes 16-bit grayscale image </summary>
		public static async Task WriteGrayscaleAsync
		(
			ushort[] pixels ,
			int width ,
			int height ,
			bool alpha ,
			string filePath
		)
		{
			try {
				var imageInfo = new ImageInfo(
					width ,
					height ,
					16 ,
					alpha ,
					true ,
					false//not implemented here yet
				);
				await Task.Run( ()=> {
					
					// open image for writing:
					PngWriter writer = FileHelper.CreatePngWriter( filePath , imageInfo , true );
					
					// add some optional metadata (chunks)
					var meta = writer.GetMetadata();
					meta.SetTimeNow( 0 );// 0 seconds fron now = now

					int numRows = imageInfo.Rows;
					int numCols = imageInfo.Cols;
					int channels = imageInfo.Channels;
					for( int row=0 ; row<numRows ; row++ )
					{
						//fill line:
						int[] ints = new int[ imageInfo.SamplesPerRow ];
						if( alpha==false )
						{
							for( int col=0 ; col<numCols ; col++ )
							{
								ushort R = pixels[ IndexPngToTexture( row , col , numRows , numCols ) ];
								ImageLineHelper.SetPixel( ints , R , col , channels );
							}
						}
						else
						{
							for( int col=0 ; col<numCols ; col++ )
							{
								ushort A = pixels[ IndexPngToTexture( row , col , numRows , numCols ) ];
								ImageLineHelper.SetPixel( ints , A , col , channels );
							}
						}
						
						//write line:
						ImageLine imageline = new ImageLine( imageInfo , ImageLine.ESampleType.INT , false , ints , null , row );
						writer.WriteRow( imageline , row );
					}
					writer.End();

				} );
			}
			catch( System.Exception ex ) { Debug.LogException(ex); await Task.CompletedTask; }//kills debugger execution loop on exception
			finally { await Task.CompletedTask; }
		}

		/// <summary> Writes 8-bit grayscale image </summary>
		public static async Task WriteGrayscaleAsync
		(
			byte[] pixels ,
			int width ,
			int height ,
			bool alpha ,
			string filePath
		)
		{
			try
			{
				var imageInfo = new ImageInfo(
					width ,
					height ,
					8 ,
					alpha ,
					true ,
					false//not implemented here yet
				);
				await Task.Run( ()=> {
					
					// open image for writing:
					PngWriter writer = FileHelper.CreatePngWriter( filePath , imageInfo , true );
					
					// add some optional metadata (chunks)
					var meta = writer.GetMetadata();
					meta.SetTimeNow( 0 );// 0 seconds fron now = now

					int numRows = imageInfo.Rows;
					int numCols = imageInfo.Cols;
					int channels = imageInfo.Channels;
					for( int row=0 ; row<numRows ; row++ )
					{
						byte[] bytes = new byte[ numCols ];
						//fill line:
						if( alpha==false )
						{
							for( int col=0 ; col<numCols ; col++ )
							{
								byte R = pixels[ IndexPngToTexture( row , col , numRows , numCols ) ];
								ImageLineHelper.SetPixel( bytes , R , col , channels );
							}
						}
						else
						{
							for( int col=0 ; col<numCols ; col++ )
							{
								byte A = pixels[ IndexPngToTexture( row , col , numRows , numCols ) ];
								ImageLineHelper.SetPixel( bytes , A , col , channels );
							}
						}
						
						//write line:
						ImageLine imageline = new ImageLine( imageInfo , ImageLine.ESampleType.BYTE , false , null , bytes , row );
						writer.WriteRow( imageline , row );
					}
					writer.End();

				} );
			}
			catch( System.Exception ex ) { Debug.LogException(ex); await Task.CompletedTask; }//kills debugger execution loop on exception
			finally { await Task.CompletedTask; }
		}

		/// <summary> Create Texture2D from PNG file, async </summary>
		public static async Task<Texture2D> ReadAsync
		(
			string filePath
		)
		{
			Texture2D result = null;
			try
			{
				var readout = await ReadColorsAsync( filePath );
				result = new Texture2D( readout.width , readout.height , readout.textureFormatInfered , false , true );
				result.SetPixels( readout.pixels );
				result.Apply();
			}
			catch( System.Exception ex ) { Debug.LogException(ex); await Task.CompletedTask; }//kills debugger execution loop on exception
			return result;
		}

		/// <summary> Create Texture2D from PNG file </summary>
		public static Texture2D Read
		(
			string filePath
		)
		{
			Texture2D result = null;
			try
			{
				var readout = ReadColors( filePath );
				result = new Texture2D( readout.width , readout.height , readout.textureFormatInfered , false , true );
				result.SetPixels( readout.pixels );
				result.Apply();
			}
			catch( System.Exception ex ) { Debug.LogException(ex); }
			return result;
		}
		
		/// <summary> Create Color[] from PNG file, async </summary>
		public static async Task<ReadColorsResult> ReadColorsAsync
		(
			string filePath
		)
		{
			ReadColorsResult results = new ReadColorsResult{
				pixels = null ,
				width = -1 ,
				height = -1 ,
				textureFormatInfered = 0
			};
			PngReader reader = null;
			try
			{
				reader = FileHelper.CreatePngReader( filePath );
				var info = reader.ImgInfo;
				results.height = info.Rows;
				results.width = info.Cols;
				results.pixels = new Color[ results.width * results.height ];
			
				int channels = info.Channels;
				int bitDepth = info.BitDepth;

				//select appropriate texture format:
				results.textureFormatInfered = GetTextureFormat( bitDepth , channels , info.Indexed );
				
				//create pixel array:
				await Task.Run( ()=> {
					ReadSamples( reader , results );
				} );
			}
			catch ( System.Exception ex )
			{
				Debug.LogException( ex );
				if( results.pixels==null )
				{
					results.width = 2;
					results.height = 2;
					results.pixels = new Color[ results.width * results.height ];
				}
				if( results.textureFormatInfered==0 ) { results.textureFormatInfered = TextureFormat.RGBA32; }
			}
			finally
			{
				if( reader!=null ) reader.Dispose();
			}
			return results;
		}

		/// <summary> Create Color[] from PNG file </summary>
		public static ReadColorsResult ReadColors
		(
			string filePath
		)
		{
			ReadColorsResult results = new ReadColorsResult{
				pixels = null ,
				width = -1 ,
				height = -1 ,
				textureFormatInfered = 0
			};
			PngReader reader = null;
			try
			{
				reader = FileHelper.CreatePngReader( filePath );
				var info = reader.ImgInfo;
				results.height = info.Rows;
				results.width = info.Cols;
				results.pixels = new Color[ results.width * results.height ];
			
				int channels = info.Channels;
				int bitDepth = info.BitDepth;

				//select appropriate texture format:
				results.textureFormatInfered = GetTextureFormat( bitDepth , channels , info.Indexed );
				
				//create pixel array:
				ReadSamples( reader , results );
			}
			catch ( System.Exception ex )
			{
				Debug.LogException( ex );
				if( results.pixels==null )
				{
					results.width = 2;
					results.height = 2;
					results.pixels = new Color[ results.width * results.height ];
				}
				if( results.textureFormatInfered==0 ) { results.textureFormatInfered = TextureFormat.RGBA32; }
			}
			finally
			{
				if( reader!=null ) reader.Dispose();
			}
			return results;
		}

		/// <summary> Reads samples using given reader </summary>
		static void ReadSamples ( PngReader reader , ReadColorsResult results )
		{
			var info = reader.ImgInfo;
			int channels = info.Channels;
			int bitDepth = info.BitDepth;
			int numCols = results.width;
			int numRows = results.height;
			Color[] pixels = results.pixels;
			float max = GetBitDepthMaxValue( bitDepth );
			if( !info.Indexed )
			{
				if( !info.Packed )
				{
					for( int row=0 ; row<numRows ; row++ )
					{
						ImageLine imageLine = reader.ReadRow( row );
						ProcessNByteRow( imageLine , pixels );
					}
				}
				else
				{
					if( bitDepth==4 )
					{
						for( int row=0 ; row<numRows ; row++ )
						{
							ImageLine imageLine = reader.ReadRowByte( row );
							Process4BitRow( imageLine , pixels );
						}
					}
					else if( bitDepth==2 )
					{
						throw new System.Exception($"bit depth {bitDepth} for {channels} channels not implemented\n");
					}
					else if( bitDepth==1 )
					{
						for( int row=0 ; row<numRows ; row++ )
						{
							ImageLine imageLine = reader.ReadRowByte( row );
							Process1BitRow( imageLine , pixels );
						}
					}
					else
					{
						throw new System.Exception($"bit depth {bitDepth} for {channels} channels not implemented\n");
					}
				}
			}
			else
			{
				var plte = reader.GetMetadata().GetPLTE();
				if( bitDepth==8 )
				{
					if( info.Alpha )
					{
						var trns = reader.GetMetadata().GetTRNS();// transparency metadata, can be null
						for( int row=0 ; row<numRows ; row++ )
						{
							ImageLine imageLine = reader.ReadRow( row );
							Process8BitIndexedRow( imageLine , plte , trns , pixels );
						}
					}
					else
					{
						for( int row=0 ; row<numRows ; row++ )
						{
							ImageLine imageLine = reader.ReadRow( row );
							Process8BitIndexedRow( imageLine , plte , pixels );
						}
					}
				}
				else if( bitDepth==4 )
				{
					for( int row=0 ; row<numRows ; row++ )
					{
						ImageLine imageLine = reader.ReadRow( row );
						Process4BitIndexedRow( imageLine , plte , pixels );
					}
				}
				else
				{
					throw new System.Exception($"bit depth {bitDepth} for {channels} channels not implemented\n");
				}
			}
		}

		/// <summary> Creates ImageInfo object based on given png </summary>
		public static ImageInfo ReadImageInfo
		(
			string filePath
		)
		{
			ImageInfo imageInfo = null;
			PngReader reader = null;
			try
			{
				reader = FileHelper.CreatePngReader( filePath );
				imageInfo = reader.ImgInfo;
			}
			catch ( System.Exception ex ) { Debug.LogException( ex ); }
			finally
			{
				if( reader!=null ) reader.Dispose();
			}
			return imageInfo;
		}

		static void Process1BitRow ( ImageLine imageLine , Color[] pixels )
		{
			int row = imageLine.ImageRow;
			ImageInfo imgInfo = imageLine.ImgInfo;
			int numRows = imgInfo.Rows;
			int numCols = imgInfo.Cols;
			var scanline = imageLine.ScanlineB;
			for( int col=0 ; col<numCols/8 ; col++ )
			{
				byte B = scanline[ col ];
				for( int bit=0 ; bit<8 ; bit++ )
				{
					float val = BIT( bit , B );
					Color color = new Color{ r = val , g = val , b = val , a = val };
					pixels[ IndexPngToTexture( row , col*8+bit , numRows , numCols ) ] = color;
				}
			}
			int BIT ( int index , byte b ) => (b&(1<<index))!=0 ? 1<<index : 0;
		}

		static void Process4BitRow ( ImageLine imageLine , Color[] pixels )
		{
			int row = imageLine.ImageRow;
			ImageInfo imgInfo = imageLine.ImgInfo;
			int numRows = imgInfo.Rows;
			int numCols = imgInfo.Cols;
			float max = GetBitDepthMaxValue( 4 );
			var scanline = imageLine.ScanlineB;
			for( int col=0 ; col<numCols/2 ; col++ )
			{
				byte B = scanline[ col ];
				int hiNybble = (B & 0xF0) >> 4; //Left hand nybble
				int loNyblle = (B & 0x0F);	  //Right hand nybble
				float val1 = (float)hiNybble / max;
				float val2 = (float)loNyblle / max;
				Color color_1 = new Color{
					r = val1 ,
					g = val1 ,
					b = val1 ,
					a = val1
				};
				Color color_2 = new Color{
					r = val2 ,
					g = val2 ,
					b = val2 ,
					a = val2
				};
				pixels[ IndexPngToTexture( row , col*2+0 , numRows , numCols ) ] = color_1;
				pixels[ IndexPngToTexture( row , col*2+1 , numRows , numCols ) ] = color_2;
			}
		}

		static void ProcessNByteRow ( ImageLine imageLine , Color[] pixels )
		{
			int row = imageLine.ImageRow;
			ImageInfo imgInfo = imageLine.ImgInfo;
			int numRows = imgInfo.Rows;
			int numCols = imgInfo.Cols;
			int bitDepth = imgInfo.BitDepth;
			int channels = imgInfo.Channels;
			float max = GetBitDepthMaxValue( bitDepth );
			var scanline = imageLine.Scanline;
			for( int col=0 ; col<numCols ; col++ )
			{
				int i = col*channels;
				RGBA<int> rgba;
				if( channels==4 )
				{
					rgba = new RGBA<int>{
						R = scanline[i] ,
						G = scanline[i+1] ,
						B = scanline[i+2] ,
						A = scanline[i+3]
					};
				}
				else if( channels==3 )
				{
					rgba = new RGBA<int>{
						R = scanline[i] ,
						G = scanline[i+1] ,
						B = scanline[i+2] ,
						A = int.MaxValue
					};
				}
				else if( channels==2 )
				{
					int val = scanline[i+1];
					int a = scanline[i];
					rgba = new RGBA<int>{
						R = val , G = val , B = val ,
						A = a
					};
				}
				else if( channels==1 )
				{
					int val = scanline[i];
					rgba = new RGBA<int>{ R = val , G = val , B = val , A = val };
				}
				else
				{
					Debug.LogError( $"{channels} channels not implemented" );
					return;
				}

				Color color = new Color{
					r = (float)rgba.R / max ,
					g = (float)rgba.G / max ,
					b = (float)rgba.B / max ,
					a = (float)rgba.A / max
				};
				pixels[ IndexPngToTexture( row , col , numRows , numCols ) ] = color;
			}
		}

		static void Process8BitIndexedRow ( ImageLine imageLine , Chunks.PngChunkPLTE plte , Chunks.PngChunkTRNS trns , Color[] pixels )
		{
			#if DEBUG
			UnityEngine.Assertions.Assert.IsNotNull( trns , "make sure this image contains no transparency data" );
			#endif
			
			int row = imageLine.ImageRow;
			ImageInfo imgInfo = imageLine.ImgInfo;
			int numRows = imgInfo.Rows;
			int numCols = imgInfo.Cols;
			int bitDepth = imgInfo.BitDepth;
			int channels = imgInfo.Channels;// int channels = 4;
			float max = GetBitDepthMaxValue( bitDepth );
			int[] paletteAlpha = trns.PaletteAlpha;
			int numIndicesWithAlpha = paletteAlpha.Length;
			if( imageLine.SampleType==ImageLine.ESampleType.BYTE )
			{
				byte[] scanline = imageLine.ScanlineB;
				for( int col=0 ; col<numCols ; col++ )
				{
					int index = scanline[col] & 0xFF;
					RGB<int> rgb = plte.GetEntryRgb( index , col*channels );
					RGBA<int> rgba = rgb.RGBA( index<numIndicesWithAlpha ? paletteAlpha[index] : 255 );
					pixels[ IndexPngToTexture( row , col , numRows , numCols ) ] = RGBA.ToColor( rgba , max );
				}
			}
			else
			{
				int[] scanline = imageLine.Scanline;
				for( int col=0 ; col<numCols ; col++ )
				{
					int index = scanline[col];
					RGB<int> rgb = plte.GetEntryRgb( index , col*channels );
					RGBA<int> rgba = rgb.RGBA( index<numIndicesWithAlpha ? paletteAlpha[index] : 255 );
					pixels[ IndexPngToTexture( row , col , numRows , numCols ) ] = RGBA.ToColor( rgba , max );
				}
			}
		}
		static void Process8BitIndexedRow ( ImageLine line , Chunks.PngChunkPLTE plte , Color[] pixels )
		{
			int row = line.ImageRow;
			ImageInfo imgInfo = line.ImgInfo;
			int numRows = imgInfo.Rows;
			int numCols = imgInfo.Cols;
			int bitDepth = imgInfo.BitDepth;
			int channels = imgInfo.Channels;// int channels = 3;
			float max = GetBitDepthMaxValue( bitDepth );
			if( line.SampleType==ImageLine.ESampleType.BYTE )
			{
				byte[] scanline = line.ScanlineB;
				for( int col=0 ; col<numCols ; col++ )
				{
					int index = scanline[col] & 0xFF;
					RGB<int> rgb = plte.GetEntryRgb( index , col*channels );
					pixels[ IndexPngToTexture( row , col , numRows , numCols ) ] = RGB.ToColor( rgb , max );
				}
			}
			else
			{
				int[] scanline = line.Scanline;
				for( int col=0 ; col<numCols ; col++ )
				{
					int index = scanline[col];
					RGB<int> rgb = plte.GetEntryRgb( index , col*channels );
					pixels[ IndexPngToTexture( row , col , numRows , numCols ) ] = RGB.ToColor( rgb , max );
				}
			}
		}

		static void Process4BitIndexedRow ( ImageLine line , Chunks.PngChunkPLTE plte , Color[] pixels )
		{
			if( !line.SamplesUnpacked ) line = line.UnpackToNewImageLine();
			int row = line.ImageRow;
			ImageInfo info = line.ImgInfo;
			int numRows = info.Rows;
			int numCols = info.Cols;
			int channels = info.Channels;// int channels = 3;
			int numSamples = numCols * channels;
			int bitDepth = info.BitDepth;
			float max = GetBitDepthMaxValue( bitDepth );
			if( line.SampleType==ImageLine.ESampleType.BYTE )
			{
				byte[] scanline = line.ScanlineB;
				for( int col=0 ; col<numCols/2 ; col++ )
				{
					// int index = scanline[col] & 0xFF;
					// RGB<int> rgb = plte.GetEntryRgb( index , col*channels );
					// pixels[ IndexPngToTexture( row , col , numRows , numCols ) ] = RGB.ToColor( rgb , max );

					byte B = scanline[ col ];
					int lhsIndex = (B & 0xF0) >> 4; //Left hand nybble
					int rhsIndex = (B & 0x0F);	  //Right hand nybble

					RGB<int> lhsRgb = plte.GetEntryRgb( lhsIndex , col*channels );
					RGB<int> rhsRgb = plte.GetEntryRgb( rhsIndex , col*channels );
					
					pixels[ IndexPngToTexture( row , col*2+0 , numRows , numCols ) ] = RGB.ToColor( lhsRgb , max );
					pixels[ IndexPngToTexture( row , col*2+1 , numRows , numCols ) ] = RGB.ToColor( rhsRgb , max );
				}
			}
			else
			{
				throw new System.Exception($"Unpacking int not implemented for bit depth {bitDepth} & {channels} channels\n");
				//TODO: 1 int will contain 16 indices I believe
			}
		}

		/// <summary> Texture2D's rows start from the bottom while PNG from the top. Hence inverted y/row. </summary>
		public static int IndexPngToTexture ( int row , int col , int numRows , int numCols )
		{
			#if DEBUG
			if( row>=numRows ) throw new System.ArgumentOutOfRangeException( $"({row}) row>=numRows ({numRows})\n" );
			if( col>=numCols ) throw new System.ArgumentOutOfRangeException( $"({col}) col>=numCols ({numCols})\n" );
			#endif

			return numCols * ( numRows - 1 - row ) + col;
		}

		public static int Index2dto1d ( int x , int y , int width ) => y * width + x;
		
		public static int GetBitDepth ( TextureFormat format )
		{
			switch ( format )
			{
				case TextureFormat.DXT1: return 8;//4;//indexed color not implemented, fallback to 8
				case TextureFormat.DXT5: return 8;
				case TextureFormat.Alpha8: return 8;
				case TextureFormat.R8: return 8;
				case TextureFormat.R16: return 16;
				case TextureFormat.RHalf: return 16;
				case TextureFormat.RFloat: return 32;
				case TextureFormat.RGB24: return 8;
				case TextureFormat.RGBA32: return 8;
				case TextureFormat.RGBAHalf: return 16;
				case TextureFormat.RGBAFloat: return 32;
				default: throw new System.NotImplementedException($"format '{format}' not implemented\n");
			}
		}

		public static bool GetIsAlpha ( TextureFormat format )
		{
			switch ( format )
			{
				case TextureFormat.DXT5: return true;
				case TextureFormat.Alpha8: return true;
				case TextureFormat.ARGB4444: return true;
				case TextureFormat.ARGB32: return true;
				case TextureFormat.RGBA32: return true;
				case TextureFormat.RGBAHalf: return true;
				case TextureFormat.RGBAFloat: return true;
				default: return false;
			}
		}

		public static bool GetIsGreyscale ( TextureFormat format )
		{
			switch ( format )
			{
				case TextureFormat.Alpha8: return true;
				case TextureFormat.R8: return true;
				case TextureFormat.R16: return true;
				case TextureFormat.RHalf: return true;
				case TextureFormat.RFloat: return true;
				default: return false;
			}
		}

		/// <summary> Guess-timate most appropriate TextureFormat for given specification. </summary>
		public static TextureFormat GetTextureFormat ( int bitDepth , int channels , bool indexed )
		{
			switch ( bitDepth*10 + channels + (indexed?1000:0) )
			{
				case 11: return TextureFormat.Alpha8;
				case 21: return TextureFormat.Alpha8;
				case 41: return TextureFormat.Alpha8;
				case 44: return TextureFormat.RGBA4444;
				case 81: return TextureFormat.Alpha8;
				case 82: return TextureFormat.Alpha8;
				case 83: return TextureFormat.RGB24;
				case 84: return TextureFormat.RGBA32;
				case 161: return TextureFormat.R16;
				case 163: return TextureFormat.RGB565;
				case 164: return TextureFormat.RGBAHalf;
				case 321: return TextureFormat.RFloat;
				case 1011: return TextureFormat.Alpha8;
				case 1021: return TextureFormat.RGB24;
				case 1041: return TextureFormat.RGB24;
				case 1081: return TextureFormat.RGB24;
				default: throw new System.NotImplementedException($"bit depth '{bitDepth}' for '{channels}' channels not implemented\n");
			}
		}

		public static int ToInt ( float color , int bitDepth )
		{
			float max = GetBitDepthMaxValue( bitDepth );
			return (int)( color * max );
		}

		public static RGB<int> ToRGB ( Color color , int bitDepth )
		{
			float max = GetBitDepthMaxValue( bitDepth );
			return new RGB<int> {
				R = (int)( color.r * max ) ,
				G = (int)( color.g * max ) ,
				B = (int)( color.b * max )
			};
		}
		
		public static RGBA<int> ToRGBA ( Color color , int bitDepth )
		{
			float max = GetBitDepthMaxValue( bitDepth );
			return new RGBA<int> {
				R = (int)( color.r * max ) ,
				G = (int)( color.g * max ) ,
				B = (int)( color.b * max ) ,
				A = (int)( color.a * max )
			};
		}

		public static uint GetBitDepthMaxValue ( int bitDepth )
		{
			switch ( bitDepth )
			{
				case 1: return 1;
				case 2: return 3;
				case 4: return 15;
				case 8: return byte.MaxValue;
				case 16: return ushort.MaxValue;
				case 32: return uint.MaxValue;
				default: throw new System.Exception( $"bitDepth '{ bitDepth }' not implemented\n" );
			}
		}

		public struct ReadColorsResult
		{
			public Color[] pixels;
			public int width, height;
			public TextureFormat textureFormatInfered;
		}

	}

}
