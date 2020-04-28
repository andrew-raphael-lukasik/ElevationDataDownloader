namespace Pngcs
{

	public struct ARGB8 <T>
		where T : unmanaged
	{
		public T value;
		public static implicit operator T ( ARGB8<T> argb8 ) => argb8.value;
		public static implicit operator ARGB8<T> ( T t ) => new ARGB8<T>{ value = t };
	}

	public static class ARGB8
	{
		public static RGB<byte> ToRGB ( ARGB8<int> value )
		{
			return new RGB<byte>{
				R = (byte)((value>>16) & 0xFF) ,
				G = (byte)((value>>8) & 0xFF) ,
				B = (byte)(value & 0xFF)
			};
		}
		public static RGB<int> ToRGB ( int value )
		{
			return new RGB<int>{
				R = ((value>>16) & 0xFF) ,
				G = ((value>>8) & 0xFF) ,
				B = (value & 0xFF)
			};
		}
		public static RGBA<byte> ToRGBA ( ARGB8<int> value )
		{
			return new RGBA<byte>{
				R = (byte)((value>>16) & 0xFF) ,
				G = (byte)((value>>8) & 0xFF) ,
				B = (byte)(value & 0xFF) ,
				A = (byte)((value>>24) & 0xFF)
			};
		}
		public static RGBA<int> ToRGBA ( int value )
		{
			return new RGBA<int>{
				R = ((value>>16) & 0xFF) ,
				G = ((value>>8) & 0xFF) ,
				B = (value & 0xFF) ,
				A = ((value>>24) & 0xFF)
			};
		}
	}

}
