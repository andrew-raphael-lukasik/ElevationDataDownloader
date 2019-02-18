using System.Collections.Generic;
using UnityEngine;

namespace ElevationMapCreator
{
    class ElevationService_Google : IElevationServiceProvider
    {
        #region interface implementation


        System.Net.Http.HttpMethod IElevationServiceProvider.httpMethod => System.Net.Http.HttpMethod.Get;

        string IElevationServiceProvider.RequestUri ( string json , string id )
        {
            return $"https://maps.googleapis.com/maps/api/elevation/json?key={ id }&locations={ json }";
        }

        string IElevationServiceProvider.GetRequestContent ( List<Coordinate> coordinates )
        {
            var sb = new System.Text.StringBuilder();
            for( int i=0 ; i<coordinates.Count ; i++ )
            {
                var next = coordinates[ i ];
                sb.AppendFormat( "{0},{1}|" , next.latitude , next.longitude );
            }
            sb.Remove( sb.Length-1 , 1 );
            return sb.ToString();
        }

        bool IElevationServiceProvider.ParseResponse ( string apiResponse , List<float> elevations  )
        {
            Results responseDeserialized = null;
            try { responseDeserialized = JsonUtility.FromJson<Results>( apiResponse ); }
            catch( System.Exception ex ) { Debug.LogException(ex); }
            if( responseDeserialized!=null )
            {
                if( responseDeserialized.error_message==null )
                {
                    foreach( var result in responseDeserialized.results )
                    {
                        elevations.Add( result.elevation );
                    }
                    return true;
                }
                else
                {
                    Debug.LogError( $"Error message:{ responseDeserialized.error_message }\nstatus: { responseDeserialized.status }\nraw response:\"{ apiResponse }\"" );
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
            public string error_message;
            public string status;
            #pragma warning restore 0649
        }

        [System.Serializable]
        struct Result
        {
            #pragma warning disable 0649//"field is never assigned to"
            public float elevation;
            public Location location;
            public float resolution;
            #pragma warning restore 0649
        }

        [System.Serializable]
        public struct Location
        {
            #pragma warning disable 0649//"field is never assigned to"
            public float lat;
            public float lng;
            #pragma warning restore 0649
        }



        #endregion
    }
}
