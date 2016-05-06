using System;
using System.Globalization;

namespace ServiceStack.Redis
{
    public struct RedisGeo
    {
        public double Longitude;
        public double Latitude;
        public string Member;

        public RedisGeo(double longitude, double latitude, string member)
        {
            Longitude = longitude;
            Latitude = latitude;
            Member = member;
        }

        public RedisGeo(string geoString) : this()
        {
            if (string.IsNullOrEmpty(geoString))
                throw new ArgumentNullException("geoString");

            var pos1 = geoString.IndexOf(' ');
            if (pos1 == -1)
                throw new ArgumentException("Invalid geoString: " + geoString);
            Longitude = double.Parse(geoString.Substring(0, pos1));

            var pos2 = geoString.IndexOf(' ', pos1 + 1);
            if (pos2 == -1)
                throw new ArgumentException("Invalid geoString: " + geoString);
            Latitude = double.Parse(geoString.Substring(pos1, pos2 - pos1));

            Member = geoString.Substring(pos2 + 1);
        }

        public override string ToString()
        {
            return Longitude.ToString(CultureInfo.InvariantCulture) 
                + " " 
                + Latitude.ToString(CultureInfo.InvariantCulture)
                + " "
                + Member;
        }

        public bool Equals(RedisGeo other)
        {
            return Longitude.Equals(other.Longitude) 
                && Latitude.Equals(other.Latitude) 
                && string.Equals(Member, other.Member);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is RedisGeo && Equals((RedisGeo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Longitude.GetHashCode();
                hashCode = (hashCode*397) ^ Latitude.GetHashCode();
                hashCode = (hashCode*397) ^ (Member != null ? Member.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}