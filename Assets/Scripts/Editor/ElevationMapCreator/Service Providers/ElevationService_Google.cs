using System.Collections.Generic;
using UnityEngine;

namespace ElevationMapCreator
{
    class ElevationService_Google : IElevationServiceProvider
    {
        #region interface implementation

        string IElevationServiceProvider.address => @"https://maps.googleapis.com/maps/api/elevation/json";

        bool IElevationServiceProvider.ParseApiResponse ( string apiResponse , List<float> elevations  )
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
