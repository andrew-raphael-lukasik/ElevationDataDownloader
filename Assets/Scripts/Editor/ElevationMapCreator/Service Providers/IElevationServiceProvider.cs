using System.Collections.Generic;
using UnityEngine;

namespace ElevationMapCreator
{
    public interface IElevationServiceProvider
    {
        bool ParseApiResponse ( string apiResponse , List<float> elevations );
        string address { get; }
    }
}
