using System.Collections.Generic;
using UnityEngine;

namespace ElevationMapCreator
{
    class ElevationService_OpenElevation : IElevationServiceProvider
	{
		#region interface implementation
		
		string IElevationServiceProvider.address => @"https://api.open-elevation.com/api/v1/lookup";

		bool IElevationServiceProvider.ParseApiResponse ( string apiResponse , List<float> elevations )
		{
			Results responseDeserialized = null;
			try { responseDeserialized = JsonUtility.FromJson<Results>( apiResponse ); }
			catch( System.Exception ex ) { Debug.LogException(ex); }
			if( responseDeserialized!=null )
			{
				if( responseDeserialized.results!=null )
				{
					foreach( var result in responseDeserialized.results )
					{
						elevations.Add( result.elevation );
					}
					return true;
				}
				else
				{
					Debug.LogError( $"Cannot parse: \"{ apiResponse }\"" );
					return false;
				}
			}
			else
			{
				Debug.LogError( "{ nameof(responseDeserialized) } is null" );
				return false;
			}
		}

		#endregion
		#region nested types
		
		[System.Serializable]
		class Results
		{
			#pragma warning disable 0649//"field is never assigned to"
			public Result[] results;
			#pragma warning restore 0649
		}

		[System.Serializable]
		struct Result
		{
			#pragma warning disable 0649//"field is never assigned to"
			public float elevation;
			public float latitude;
			public float longitude;
			#pragma warning restore 0649
		}

		[System.Serializable]
		public class Locations
		{
			public Coordinate[] locations;
		}
		
		#endregion
	}
}
