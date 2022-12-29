using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.UseCases
{
    /// <summary>
    /// Solution in response to:
    /// http://stackoverflow.com/questions/5057684/json-c-deserializing-a-changing-content-or-a-piece-of-json-response
    /// </summary>
    public class CentroidTests
    {
        public class Centroid
        {
            public Centroid(decimal latitude, decimal longitude)
            {
                Latitude = latitude;
                Longitude = longitude;
            }

            public string LatLon
            {
                get
                {
                    return String.Format("{0};{1}", Latitude, Longitude);
                }
            }

            public decimal Latitude { get; set; }

            public decimal Longitude { get; set; }
        }

        public class BoundingBox
        {
            public Centroid SouthWest { get; set; }

            public Centroid NorthEast { get; set; }
        }

        public class Place
        {
            public int WoeId { get; set; }

            public string PlaceTypeName { get; set; }

            public string Name { get; set; }

            public Dictionary<string, string> PlaceTypeNameAttrs { get; set; }

            public string Country { get; set; }

            public Dictionary<string, string> CountryAttrs { get; set; }

            public string Admin1 { get; set; }

            public Dictionary<string, string> Admin1Attrs { get; set; }

            public string Admin2 { get; set; }

            public string Admin3 { get; set; }

            public string Locality1 { get; set; }

            public string Locality2 { get; set; }

            public string Postal { get; set; }

            public Centroid Centroid { get; set; }

            public BoundingBox BoundingBox { get; set; }

            public int AreaRank { get; set; }

            public int PopRank { get; set; }

            public string Uri { get; set; }

            public string Lang { get; set; }
        }

        private const string JsonCentroid = @"{
   ""place"":{
      ""woeid"":12345,
      ""placeTypeName"":""State"",
      ""placeTypeName attrs"":{
         ""code"":8
      },
      ""name"":""My Region"",
      ""country"":"""",
      ""country attrs"":{
         ""type"":""Country"",
         ""code"":""XX""
      },
      ""admin1"":""My Region"",
      ""admin1 attrs"":{
         ""type"":""Region"",
         ""code"":""""
      },
      ""admin2"":"""",
      ""admin3"":"""",
      ""locality1"":"""",
      ""locality2"":"""",
      ""postal"":"""",
      ""centroid"":{
         ""latitude"":30.12345,
         ""longitude"":40.761292
      },
      ""boundingBox"":{
         ""southWest"":{
            ""latitude"":32.2799,
            ""longitude"":50.715958
         },
         ""northEast"":{
            ""latitude"":29.024891,
            ""longitude"":12.1234
         }
      },
      ""areaRank"":10,
      ""popRank"":0,
      ""uri"":""http:\/\/where.yahooapis.com"",
      ""lang"":""en-US""
   }
}";

        [Test]
        public void Can_Parse_Centroid_using_JsonObject()
        {
            Func<JsonObject, Centroid> toCentroid = map =>
                new Centroid(map.Get<decimal>("latitude"), map.Get<decimal>("longitude"));

            var place = JsonObject.Parse(JsonCentroid)
                .Object("place")
                .ConvertTo(x => new Place
                {
                    WoeId = x.Get<int>("woeid"),
                    PlaceTypeName = x.Get(""),
                    PlaceTypeNameAttrs = x.Object("placeTypeName attrs"),
                    Name = x.Get("Name"),
                    Country = x.Get("Country"),
                    CountryAttrs = x.Object("country attrs"),
                    Admin1 = x.Get("admin1"),
                    Admin1Attrs = x.Object("admin1 attrs"),
                    Admin2 = x.Get("admin2"),
                    Admin3 = x.Get("admin3"),
                    Locality1 = x.Get("locality1"),
                    Locality2 = x.Get("locality2"),
                    Postal = x.Get("postal"),

                    Centroid = x.Object("centroid")
                        .ConvertTo(toCentroid),

                    BoundingBox = x.Object("boundingBox")
                        .ConvertTo(y => new BoundingBox
                        {
                            SouthWest = y.Object("southWest").ConvertTo(toCentroid),
                            NorthEast = y.Object("northEast").ConvertTo(toCentroid)
                        }),

                    AreaRank = x.Get<int>("areaRank"),
                    PopRank = x.Get<int>("popRank"),
                    Uri = x.Get("uri"),
                    Lang = x.Get("lang"),
                });

            Console.WriteLine(place.Dump());

            /*Outputs:
			{
				WoeId: 12345,
				PlaceTypeNameAttrs: 
				{
					code: 8
				},
				CountryAttrs: 
				{
					type: Country,
					code: XX
				},
				Admin1: My Region,
				Admin1Attrs: 
				{
					type: Region,
					code: 
				},
				Admin2: ,
				Admin3: ,
				Locality1: ,
				Locality2: ,
				Postal: ,
				Centroid: 
				{
					LatLon: 30.12345;40.761292,
					Latitude: 30.12345,
					Longitude: 40.761292
				},
				BoundingBox: 
				{
					SouthWest: 
					{
						LatLon: 32.2799;50.715958,
						Latitude: 32.2799,
						Longitude: 50.715958
					},
					NorthEast: 
					{
						LatLon: 29.024891;12.1234,
						Latitude: 29.024891,
						Longitude: 12.1234
					}
				},
				AreaRank: 10,
				PopRank: 0,
				Uri: "http://where.yahooapis.com",
				Lang: en-US
			}
			**/
        }


    }
}