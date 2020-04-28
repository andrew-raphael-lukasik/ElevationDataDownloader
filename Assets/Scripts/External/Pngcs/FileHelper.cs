using IO = System.IO;

namespace Pngcs
{
	/// <summary> A few utility static methods to read and write files </summary>
	public class FileHelper
	{

		public static IO.Stream OpenFileForReading ( string file )
		{
			IO.Stream isx = null;
			if( file==null || IO.File.Exists(file)==false ) throw new IO.IOException($"Cannot open file for reading ({file})");
			isx = new IO.FileStream( file , IO.FileMode.Open , IO.FileAccess.Read , IO.FileShare.Read );
			return isx;
		}

		public static IO.Stream OpenFileForWriting ( string file , bool allowOverwrite )
		{
			IO.Stream osx = null;
			if( IO.File.Exists(file) && !allowOverwrite ) throw new IO.IOException($"File already exists ({file}) and overwrite=false");
			osx = new IO.FileStream( file , IO.FileMode.Create , IO.FileAccess.Write , IO.FileShare.None );
			return osx;
		}

		/// <summary> Given a filename and a ImageInfo, produces a PngWriter object, ready for writing. </summary>
		/// <param name="fileName"> Path of file </param>
		/// <param name="imgInfo"> ImageInfo object </param>
		/// <param name="allowOverwrite"> Flag: if false and file exists, a IO.IOException is thrown </param>
		/// <returns> A PngWriter object, ready for writing </returns>
		public static PngWriter CreatePngWriter ( string fileName , ImageInfo imgInfo , bool allowOverwrite ) => new PngWriter( OpenFileForWriting( fileName , allowOverwrite ) , imgInfo , fileName );

		/// <summary> Given a filename, produces a PngReader object, ready for reading. </summary>
		/// <param name="fileName"> Path of file </param>
		/// <returns> PngReader, ready for reading </returns>
		public static PngReader CreatePngReader ( string fileName ) => new PngReader( OpenFileForReading( fileName ) , fileName );
		
	}
}
