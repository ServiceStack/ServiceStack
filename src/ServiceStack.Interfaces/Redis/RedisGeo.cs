using System;
using System.Globalization;

namespace ServiceStack.Redis
{
    public class RedisGeo
    {
        public RedisGeo() { }

        public RedisGeo(double longitude, double latitude, string member)
        {
            Longitude = longitude;
            Latitude = latitude;
            Member = member;
        }

        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public string Member { get; set; }
    }

    public class RedisGeoResult
    {
        public string Member { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public long Hash { get; set; }
        public string Unit { get; set; }
        public double Distance { get; set; }
    }

    public static class RedisGeoUnit
    {
        public const string Meters = "m";
        public const string Kilometers = "km";
        public const string Miles = "mo";
        public const string Feet = "ft";
    }
}