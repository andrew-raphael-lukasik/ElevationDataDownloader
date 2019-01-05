using UnityEngine;

namespace ElevationMapCreator
{
    [System.Serializable] 
    public struct CoordinateInt
    {
        public int latitude;
        public int longitude;
        public static CoordinateInt operator + ( CoordinateInt a , CoordinateInt b ) => new CoordinateInt{ latitude = a.latitude + b.latitude , longitude = a.longitude + b.longitude };
        public static CoordinateInt operator - ( CoordinateInt a , CoordinateInt b ) => new CoordinateInt{ latitude = a.latitude - b.latitude , longitude = a.longitude - b.longitude };
        //public static CoordinateInt operator * ( CoordinateInt coord , float f ) => new CoordinateInt{ latitude = coord.latitude * f , longitude = coord.longitude * f };
        public static bool operator == ( CoordinateInt a , CoordinateInt b ) => a.latitude==b.latitude && a.longitude==b.longitude;
        public static bool operator != ( CoordinateInt a , CoordinateInt b ) => a.latitude!=b.latitude && a.longitude!=b.longitude;

        public override bool Equals ( object obj )
        {
            if( obj==null || (obj is CoordinateInt)==false ) { return false; }
            return this==(CoordinateInt)obj;
        }

        override public string ToString () { return JsonUtility.ToJson( this ); }

        public override int GetHashCode ()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + this.latitude;
                hash = hash * 31 + this.longitude;
                return hash;
            }
        }

    }
}
