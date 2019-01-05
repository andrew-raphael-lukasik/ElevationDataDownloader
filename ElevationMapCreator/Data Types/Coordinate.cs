using UnityEngine;

namespace ElevationMapCreator
{
    [System.Serializable] 
    public struct Coordinate
    {
        public float latitude;
        public float longitude;
        public static Coordinate operator + ( Coordinate a , Coordinate b ) => new Coordinate{ latitude = a.latitude + b.latitude , longitude = a.longitude + b.longitude };
        public static Coordinate operator - ( Coordinate a , Coordinate b ) => new Coordinate{ latitude = a.latitude - b.latitude , longitude = a.longitude - b.longitude };
        public static Coordinate operator * ( Coordinate coord , float f ) => new Coordinate{ latitude = coord.latitude * f , longitude = coord.longitude * f };
        public static bool operator == ( Coordinate a , Coordinate b ) => a.latitude==b.latitude && a.longitude==b.longitude;
        public static bool operator != ( Coordinate a , Coordinate b ) => a.latitude!=b.latitude && a.longitude!=b.longitude;

        public override bool Equals ( object obj )
        {
            if( obj==null || (obj is Coordinate)==false ) { return false; }
            return this==(Coordinate)obj;
        }

        override public string ToString () { return JsonUtility.ToJson( this ); }

        public override int GetHashCode ()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + this.latitude.GetHashCode();
                hash = hash * 31 + this.longitude.GetHashCode();
                return hash;
            }
        }

    }
}
