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
        public string Hash { get; set; }
        public string Unit { get; set; }
        public double Distance { get; set; }
    }
}