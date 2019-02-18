using System.Collections.Generic;
using UnityEngine;

namespace ElevationMapCreator
{
    public interface IElevationServiceProvider
    {
        System.Net.Http.HttpMethod httpMethod { get; }
        string RequestUri ( string json , string id );
        string GetRequestContent ( List<Coordinate> coordinates );
        bool ParseResponse ( string apiResponse , List<float> elevations );
    }
}
