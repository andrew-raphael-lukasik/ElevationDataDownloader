using System.Threading.Tasks;
using IO = System.IO;

using UnityEngine;

namespace Pngcs.Unity
{
    public static class PNG
    {

        /// <summary> Write PNG to file </summary>
        public static async Task WriteAsync
        (
            Texture2D texture ,
            string filePath
        )
        {
            var pixels = texture.GetPixels();
            var format = texture.format;
            int bitDepth = GetBitDepth( format );
            bool alpha = GetIsAlpha( format );
            bool greyscale = GetIsGreyscale( format );
            await WriteAsync(
                pixels:     pixels ,
                width:      texture.width ,
                height:     texture.height ,
                bitDepth:   bitDepth ,
                alpha:      alpha ,
                greyscale:  greyscale ,
                filePath:filePath
            );
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
            var info = new ImageInfo(
                width ,
                height ,
                bitDepth ,
                alpha ,
                greyscale ,
                false//not implemented here yet
            );
            await Task.Run( ()=> {
                
                // open image for writing:
                PngWriter writer = FileHelper.CreatePngWriter( filePath , info , true );
                
                // add some optional metadata (chunks)
                var meta = writer.GetMetadata();
                meta.SetTimeNow( 0 );// 0 seconds fron now = now

                int numRows = info.Rows;
                int numCols = info.Cols;
                for( int row=0 ; row<numRows ; row++ )
                {
                    ImageLine imageline = new ImageLine( info );
                    int maxSampleVal = imageline.maxSampleVal;

                    //fill line:
                    if( greyscale==false )
                    {
                        if( alpha )
                        {
                            for( int col=0 ; col<numCols ; col++ )
                            {
                                RGBA rgba = ToRGBA( pixels[ IndexPngToTexture( row , col , numRows , numCols ) ] , bitDepth );
                                ImageLineHelper.SetPixel( imageline , col , rgba.r , rgba.g , rgba.b , rgba.a );
                            }
                        }
                        else
                        {
                            for( int col=0 ; col<numCols ; col++ )
                            {
                                RGB rgb = ToRGB( pixels[ IndexPngToTexture( row , col , numRows , numCols ) ] , bitDepth );
                                ImageLineHelper.SetPixel( imageline , col , rgb.r , rgb.g , rgb.b );
                            }
                        }
                    }
                    else
                    {
                        if( alpha==false )
                        {
                            for( int col=0 ; col<numCols ; col++ )
                            {
                                int r = ToInt( pixels[ IndexPngToTexture( row , col , numRows , numCols ) ].r , bitDepth );
                                ImageLineHelper.SetPixel( imageline , col , r );
                            }
                        }
                        else
                        {
                            for( int col=0 ; col<numCols ; col++ )
                            {
                                int a = ToInt( pixels[ IndexPngToTexture( row , col , numRows , numCols ) ].a , bitDepth );
                                ImageLineHelper.SetPixel( imageline , col , a );
                            }
                        }
                    }
                    
                    //write line:
                    writer.WriteRow( imageline , row );
                }
                writer.End();

            } );
            
            await Task.CompletedTask;
        }

        public static async Task WriteGrayscaleAsync
        (
            int[] pixels ,
            int width ,
            int height ,
            int bitDepth ,
            bool alpha ,
            string filePath
        )
        {
            var info = new ImageInfo(
                width ,
                height ,
                bitDepth ,
                alpha ,
                true ,
                false//not implemented here yet
            );
            await Task.Run( ()=> {
                
                // open image for writing:
                PngWriter writer = FileHelper.CreatePngWriter( filePath , info , true );
                
                // add some optional metadata (chunks)
                var meta = writer.GetMetadata();
                meta.SetTimeNow( 0 );// 0 seconds fron now = now

                int numRows = info.Rows;
                int numCols = info.Cols;
                for( int row=0 ; row<numRows ; row++ )
                {
                    ImageLine imageline = new ImageLine( info );
                    int maxSampleVal = imageline.maxSampleVal;

                    //fill line:
                    if( alpha==false )
                    {
                        for( int col=0 ; col<numCols ; col++ )
                        {
                            int r = pixels[ IndexPngToTexture( row , col , numRows , numCols ) ];
                            ImageLineHelper.SetPixel( imageline , col , r );
                        }
                    }
                    else
                    {
                        for( int col=0 ; col<numCols ; col++ )
                        {
                            int a = pixels[ IndexPngToTexture( row , col , numRows , numCols ) ];
                            ImageLineHelper.SetPixel( imageline , col , a );
                        }
                    }
                    
                    //write line:
                    writer.WriteRow( imageline , row );
                }
                writer.End();

            } );
            
            await Task.CompletedTask;
        }

        /// <summary> Create Texture2D from PNG file </summary>
        public static async Task<Texture2D> ReadAsync
        (
            string filePath
        )
        {
            Texture2D result;
            int numRows = -1;
            int numCols = -1;
            TextureFormat textureFormat = 0;
            Color[] pixels = null;
            PngReader reader = null;
            try
            {
                reader = FileHelper.CreatePngReader( filePath );
                var info = reader.ImgInfo;
                numRows = info.Rows;
                numCols = info.Cols;
                pixels = new Color[ numCols * numRows ];
            
                int channels = info.Channels;
                int bitDepth = info.BitDepth;
                if( info.Indexed ) { throw new System.NotImplementedException( "indexed png not implemented" ); }

                //select appropriate texture format:
                textureFormat = GetTextureFormat( bitDepth , channels );
                
                //create pixel array:
                await Task.Run( ()=> {

                    for( int row=0 ; row<numRows ; row++ )
                    {
                        ImageLine imageLine = reader.ReadRowInt( row );
                        var scanline = imageLine.Scanline;
                        if( imageLine.SampleType==ImageLine.ESampleType.INT )
                        {
                            for( int col=0 ; col<numCols ; col++ )
                            {
                                var color = new Color();
                                for( int ch=0 ; ch<channels ; ch++ )
                                {
                                    int raw = scanline[ col * channels + ch ];
                                    float rawMax = GetBitDepthMaxValue( bitDepth );
                                    float value = (float)raw / rawMax;

                                    //
                                    if( ch==0 ) { color.r = value; }
                                    else if( ch==1 ) { color.g = value; }
                                    else if( ch==2 ) { color.b = value; }
                                    else if( ch==3 ) { color.a = value; }
                                    else { throw new System.Exception( $"channel { ch } not implemented" ); }
                                }
                                pixels[ IndexPngToTexture( row , col , numRows , numCols ) ] = color;
                            }
                        }
                        else { throw new System.Exception( $"imageLine.SampleType { imageLine.SampleType } not implemented" ); }
                    }

                } );
            }
            catch ( System.Exception ex )
            {
                Debug.LogException( ex );
                if( pixels==null )
                {
                    numCols = 2;
                    numRows = 2;
                    pixels = new Color[ numCols * numRows ];
                }
                if( textureFormat==0 ) { textureFormat = TextureFormat.RGBA32; }
            }
            finally
            {
                if( reader!=null ) reader.End();

                //create texture
                result = new Texture2D( numCols , numRows , textureFormat , false , true );
                result.wrapMode = TextureWrapMode.Clamp;
                result.SetPixels( pixels );
                result.Apply();
            }
            return result;
        }

        /// <summary> Texture2D's rows start from the bottom while PNG from the top. Hence inverted y/row.  </summary>
        public static int IndexPngToTexture ( int row , int col , int numRows , int numCols ) => numCols * ( numRows - 1 - row ) + col;

        public static int Index2dto1d ( int x , int y , int width ) => y * width + x;
        
        public static int GetBitDepth ( TextureFormat format )
        {
            switch ( format )
            {
                case TextureFormat.Alpha8: return 8;
                case TextureFormat.R8: return 8;
                case TextureFormat.R16: return 16;
                case TextureFormat.RHalf: return 16;
                case TextureFormat.RFloat: return 32;
                case TextureFormat.RGB24: return 8;
                case TextureFormat.RGBA32: return 8;
                case TextureFormat.RGBAHalf: return 16;
                case TextureFormat.RGBAFloat: return 32;
                default: throw new System.NotImplementedException( $"format '{ format }' not implemented" );
            }
        }

        public static bool GetIsAlpha ( TextureFormat format )
        {
            switch ( format )
            {
                case TextureFormat.Alpha8: return true;//? im not 100% sure is it alpha or, for example, red channel
                case TextureFormat.R8: return false;
                case TextureFormat.R16: return false;
                case TextureFormat.RHalf: return false;
                case TextureFormat.RFloat: return false;
                case TextureFormat.RGB24: return false;
                case TextureFormat.RGBA32: return true;
                case TextureFormat.RGBAHalf: return true;
                case TextureFormat.RGBAFloat: return true;
                default: throw new System.NotImplementedException( $"format '{ format }' not implemented" );
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

        public static TextureFormat GetTextureFormat ( int bitDepth , int channels )
        {
            switch ( bitDepth*10 + channels )
            {
                case 84: return TextureFormat.RGBA32;
                case 83: return TextureFormat.RGB24;
                case 81: return TextureFormat.Alpha8;
                //case 81: return TextureFormat.R8;//not sure how to infer between Alpha8 and R8 
                case 161: return TextureFormat.R16;
                //case 161: return TextureFormat.RHalf;//not sure how to infer between R16 and RHalf 
                case 321: return TextureFormat.RFloat;
                default: throw new System.NotImplementedException( $"bit depth '{ bitDepth }' for '{ channels }' channels not implemented" );
            }
        }

        public static int ToInt ( float color , int bitDepth )
        {
            float max = GetBitDepthMaxValue( bitDepth );
            return (int)( color * max );
        }

        public static RGB ToRGB ( Color color , int bitDepth )
        {
            float max = GetBitDepthMaxValue( bitDepth );
            return new RGB {
                r = (int)( color.r * max ) ,
                g = (int)( color.g * max ) ,
                b = (int)( color.b * max )
            };
        }
        
        public static RGBA ToRGBA ( Color color , int bitDepth )
        {
            float max = GetBitDepthMaxValue( bitDepth );
            return new RGBA {
                r = (int)( color.r * max ) ,
                g = (int)( color.g * max ) ,
                b = (int)( color.b * max ) ,
                a = (int)( color.a * max )
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
                default: throw new System.Exception( $"bitDepth '{ bitDepth }' not implemented" );
            }
        }

        public struct RGB { public int r, g, b; }

        public struct RGBA { public int r, g, b, a; }

    }

}
