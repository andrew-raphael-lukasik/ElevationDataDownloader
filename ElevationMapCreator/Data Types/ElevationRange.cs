using System.Collections.Generic;

namespace ElevationMapCreator
{

	/// <summary> Simplifies finding elevation range in data </summary>
	[System.Serializable]
	public class ElevationRange
	{
		
		public float min = float.PositiveInfinity;
		public float max = float.NegativeInfinity;
		
		public void Append ( float elevation )
		{
			if( elevation < min ) { min = elevation; }
			if( elevation > max ) { max = elevation; }
		}

		public void Append ( IEnumerable<float> collection )
		{
			if( collection!=null )
			{
				foreach( float e in collection )
				{
					if( e < min ) { min = e; }
					if( e > max ) { max = e; }
				}
			}
		}

		public void Reset ()
		{
			this.min = float.PositiveInfinity;
			this.max = float.NegativeInfinity;
		}

	}
	
}
