namespace Pngcs
{
	public struct RGB <T>
		where T : unmanaged
	{
		public T R, G, B;
		public RGBA<T> RGBA ( T A ) => new RGBA<T>{ R=R , G=G , B=B , A=A };
	}
	public static class RGB
	{
		public static UnityEngine.Color ToColor ( RGB<int> rgb , float denominator ) => new UnityEngine.Color{ r=(float)rgb.R/denominator , g=(float)rgb.G/denominator , b=(float)rgb.B/denominator };
	}
}
