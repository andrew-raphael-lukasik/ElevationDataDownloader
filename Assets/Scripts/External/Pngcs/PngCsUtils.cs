namespace Pngcs
{
	/// <summary> Utility functions for C# porting </summary>
	internal class PngCsUtils
	{

		// Copyright (c) 2008-2013 Hafthor Stefansson
		// Distributed under the MIT/X11 software license
		// Ref: http://www.opensource.org/licenses/mit-license.php.
		internal static unsafe bool UnSafeEquals ( byte[] A , byte[] B )
		{
			if( A==B ) return true;
			if( A==null || B==null || A.Length!=B.Length ) return false;
			fixed( byte* a=A , b=B )
			{
				byte* x1=a, x2=b;
				int l = A.Length;
				for( int i=0 ; i<l/8 ; i++, x1+=8, x2+=8 )
				if( *((long*)x1)!=*((long*)x2) ) return false;
				if( (l & 4)!=0 ) { if( *((int*)x1)!=*((int*)x2) ) return false; x1+=4; x2+=4; }
				if( (l & 2)!=0 ) { if( *((short*)x1)!=*((short*)x2) ) return false; x1+=2; x2+=2; }
				if( (l & 1)!=0 ) if( *((byte*)x1) != *((byte*)x2) ) return false;
				return true;
			}
		}

	}
}
