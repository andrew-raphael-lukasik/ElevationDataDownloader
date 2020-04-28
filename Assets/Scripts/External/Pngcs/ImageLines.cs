namespace Pngcs
{
	/// <summary>
	/// Wraps a set of rows from a image, read in a single operation, stored in a int[][] or byte[][] matrix
	/// They can be a subset of the total rows, but in this case they are equispaced.
	/// </summary>
	/// <note> See also ImageLine </note>
	public class ImageLines
	{

		public ImageInfo ImgInfo { get; private set; }
		public ImageLine.ESampleType sampleType { get; private set; }
		public bool SamplesUnpacked { get; private set; }
		public int RowOffset { get; private set; }
		public int ImageRows { get; private set; }
		public int RowStep { get; private set; }
		internal readonly int channels;
		internal readonly int bitDepth;
		internal readonly int elementsPerRow;
		public int[][] Scanlines { get; private set; }
		public byte[][] ScanlinesB { get; private set; }

		public ImageLines( ImageInfo ImgInfo ,  ImageLine.ESampleType sampleType , bool unpackedMode , int rowOffset , int imageRows , int rowStep )
		{
			this.ImgInfo = ImgInfo;
			channels = ImgInfo.Channels;
			bitDepth = ImgInfo.BitDepth;
			this.sampleType = sampleType;
			this.SamplesUnpacked = unpackedMode || !ImgInfo.Packed;
			this.RowOffset = rowOffset;
			this.ImageRows = imageRows;
			this.RowStep = rowStep;
			elementsPerRow = unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked;
			if( sampleType==ImageLine.ESampleType.INT )
			{
				Scanlines = new int[imageRows][];
				for( int i=0 ; i<imageRows ; i++ ) Scanlines[i] = new int[elementsPerRow];
				ScanlinesB = null;
			}
			else if( sampleType==ImageLine.ESampleType.BYTE )
			{
				ScanlinesB = new byte[imageRows][];
				for( int i=0 ; i<imageRows ; i++ ) ScanlinesB[i] = new byte[elementsPerRow];
				Scanlines = null;
			}
			else throw new System.Exception("bad ImageLine initialization");
		}

		/// <summary>
		/// Translates from image row number to matrix row.
		/// If you are not sure if this image row in included, use better ImageRowToMatrixRowStrict
		/// </summary>
		/// <param name="imageRow">Row number in the original image (from 0) </param>
		/// <returns>Row number in the wrapped matrix. Undefined result if invalid</returns>
		public int ImageRowToMatrixRow ( int imageRow )
		{
			int r = (imageRow - RowOffset) / RowStep;
			return r<0 ? 0 : ( r<ImageRows ? r : ImageRows-1 );
		}

		/// <summary> Translates from image row number to matrix row </summary>
		/// <param name="imageRow">Row number in the original image (from 0) </param>
		/// <returns>Row number in the wrapped matrix. Returns -1 if invalid</returns>
		public int ImageRowToMatrixRowStrict ( int imageRow )
		{
			imageRow -= RowOffset;
			int matrixRow = imageRow>=0 && imageRow%RowStep==0 ? imageRow/RowStep : -1;
			return matrixRow<ImageRows ? matrixRow : -1;
		}

		/// <summary> Translates from matrix row number to real image row number </summary>
		/// <param name="matrixRow"> Row number inside the matrix </param>
		public int MatrixRowToImageRow ( int matrixRow ) => matrixRow*RowStep + RowOffset;

		/// <summary>
		/// Constructs and returns an ImageLine object backed by a matrix row.
		/// This is quite efficient, no deep copy.
		/// </summary>
		/// <param name="matrixRow"> Row number inside the matrix </param>
		public ImageLine GetImageLineAtMatrixRow ( int matrixRow )
		{
			if( matrixRow<0 || matrixRow>ImageRows ) throw new System.Exception($"Bad row {matrixRow}. Should be positive and less than {ImageRows}");
			int[] intData = sampleType==ImageLine.ESampleType.INT ? Scanlines[matrixRow] : null;
			byte[] byteData = sampleType==ImageLine.ESampleType.INT ? null : ScanlinesB[matrixRow];
			return new ImageLine(
				imgInfo:		ImgInfo ,
				sampleType:		  sampleType ,
				unpackedMode:   SamplesUnpacked ,
				scanLineInt:	intData ,
				scanLineBytes:  byteData ,
				imageRow:	   MatrixRowToImageRow( matrixRow )
			);
		}

	}
}
