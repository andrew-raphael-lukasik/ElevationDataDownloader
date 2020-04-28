namespace Pngcs
{
	public struct RGBA <T>
		where T : unmanaged
	{
		public T R, G, B, A;
		public RGB<T> RGB => new RGB<T>{ R=R , G=G , B=B };
	}
	public static class RGBA
	{
		public static UnityEngine.Color ToColor ( RGBA<int> rgba , float denominator ) => new UnityEngine.Color{ r=(float)rgba.R/denominator , g=(float)rgba.G/denominator , b=(float)rgba.B/denominator , a=(float)rgba.A/denominator };
	}
}
