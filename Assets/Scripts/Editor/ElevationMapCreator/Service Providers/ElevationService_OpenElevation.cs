using System.Collections.Generic;
using UnityEngine;

namespace ElevationMapCreator
{
    class ElevationService_OpenElevation : IElevationServiceProvider
	{
		#region interface implementation

		
		System.Net.Http.HttpMethod IElevationServiceProvider.httpMethod => System.Net.Http.HttpMethod.Post;
		
		string IElevationServiceProvider.RequestUri ( string json , string id )
		{
			return "https://api.open-elevation.com/api/v1/lookup";
		}

		string IElevationServiceProvider.GetRequestContent ( List<Coordinate> coordinates )
        {
            return JsonUtility.ToJson( new Locations() { locations = coordinates } );
        }

		bool IElevationServiceProvider.ParseResponse ( string apiResponse , List<float> elevations )
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
				Debug.LogError( "Response deserialized is null" );
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
		public class Locations { public List<Coordinate> locations; }

		
		#endregion
	}
}
