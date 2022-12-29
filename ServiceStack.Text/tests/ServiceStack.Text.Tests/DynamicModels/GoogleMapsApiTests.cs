using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.DynamicModels
{
    public class GoogleMapsApiTests
    {
        private const string JsonDto =
            @"{
   ""results"" : [
      {
         ""address_components"" : [
            {
               ""long_name"" : ""108"",
               ""short_name"" : ""108"",
               ""types"" : [ ""street_number"" ]
            },
            {
               ""long_name"" : ""S Almansor St"",
               ""short_name"" : ""S Almansor St"",
               ""types"" : [ ""route"" ]
            },
            {
               ""long_name"" : ""Alhambra"",
               ""short_name"" : ""Alhambra"",
               ""types"" : [ ""locality"", ""political"" ]
            },
            {
               ""long_name"" : ""Los Angeles"",
               ""short_name"" : ""Los Angeles"",
               ""types"" : [ ""administrative_area_level_2"", ""political"" ]
            },
            {
               ""long_name"" : ""California"",
               ""short_name"" : ""CA"",
               ""types"" : [ ""administrative_area_level_1"", ""political"" ]
            },
            {
               ""long_name"" : ""United States"",
               ""short_name"" : ""US"",
               ""types"" : [ ""country"", ""political"" ]
            },
            {
               ""long_name"" : ""91801"",
               ""short_name"" : ""91801"",
               ""types"" : [ ""postal_code"" ]
            }
         ],
         ""formatted_address"" : ""108 S Almansor St, Alhambra, CA 91801, USA"",
         ""geometry"" : {
            ""location"" : {
               ""lat"" : 34.096680,
               ""lng"" : -118.1197330
            },
            ""location_type"" : ""ROOFTOP"",
            ""viewport"" : {
               ""northeast"" : {
                  ""lat"" : 34.09802898029150,
                  ""lng"" : -118.1183840197085
               },
               ""southwest"" : {
                  ""lat"" : 34.09533101970850,
                  ""lng"" : -118.1210819802915
               }
            }
         },
         ""types"" : [ ""street_address"" ]
      },
      {
         ""address_components"" : [
            {
               ""long_name"" : ""91801"",
               ""short_name"" : ""91801"",
               ""types"" : [ ""postal_code"" ]
            },
            {
               ""long_name"" : ""Alhambra"",
               ""short_name"" : ""Alhambra"",
               ""types"" : [ ""locality"", ""political"" ]
            },
            {
               ""long_name"" : ""Los Angeles"",
               ""short_name"" : ""Los Angeles"",
               ""types"" : [ ""administrative_area_level_2"", ""political"" ]
            },
            {
               ""long_name"" : ""California"",
               ""short_name"" : ""CA"",
               ""types"" : [ ""administrative_area_level_1"", ""political"" ]
            },
            {
               ""long_name"" : ""United States"",
               ""short_name"" : ""US"",
               ""types"" : [ ""country"", ""political"" ]
            }
         ],
         ""formatted_address"" : ""Alhambra, CA 91801, USA"",
         ""geometry"" : {
            ""bounds"" : {
               ""northeast"" : {
                  ""lat"" : 34.1111460,
                  ""lng"" : -118.1081760
               },
               ""southwest"" : {
                  ""lat"" : 34.069770,
                  ""lng"" : -118.160660
               }
            },
            ""location"" : {
               ""lat"" : 34.08379580,
               ""lng"" : -118.11811990
            },
            ""location_type"" : ""APPROXIMATE"",
            ""viewport"" : {
               ""northeast"" : {
                  ""lat"" : 34.1111460,
                  ""lng"" : -118.1081760
               },
               ""southwest"" : {
                  ""lat"" : 34.069770,
                  ""lng"" : -118.160660
               }
            }
         },
         ""types"" : [ ""postal_code"" ]
      },
      {
         ""address_components"" : [
            {
               ""long_name"" : ""Alhambra"",
               ""short_name"" : ""Alhambra"",
               ""types"" : [ ""locality"", ""political"" ]
            },
            {
               ""long_name"" : ""Los Angeles"",
               ""short_name"" : ""Los Angeles"",
               ""types"" : [ ""administrative_area_level_2"", ""political"" ]
            },
            {
               ""long_name"" : ""California"",
               ""short_name"" : ""CA"",
               ""types"" : [ ""administrative_area_level_1"", ""political"" ]
            },
            {
               ""long_name"" : ""United States"",
               ""short_name"" : ""US"",
               ""types"" : [ ""country"", ""political"" ]
            }
         ],
         ""formatted_address"" : ""Alhambra, CA, USA"",
         ""geometry"" : {
            ""bounds"" : {
               ""northeast"" : {
                  ""lat"" : 34.1111460,
                  ""lng"" : -118.1081810
               },
               ""southwest"" : {
                  ""lat"" : 34.05992090,
                  ""lng"" : -118.1648350
               }
            },
            ""location"" : {
               ""lat"" : 34.0952870,
               ""lng"" : -118.12701460
            },
            ""location_type"" : ""APPROXIMATE"",
            ""viewport"" : {
               ""northeast"" : {
                  ""lat"" : 34.1111460,
                  ""lng"" : -118.1081810
               },
               ""southwest"" : {
                  ""lat"" : 34.05992090,
                  ""lng"" : -118.1648350
               }
            }
         },
         ""types"" : [ ""locality"", ""political"" ]
      },
      {
         ""address_components"" : [
            {
               ""long_name"" : ""Los Angeles"",
               ""short_name"" : ""Los Angeles"",
               ""types"" : [ ""administrative_area_level_2"", ""political"" ]
            },
            {
               ""long_name"" : ""California"",
               ""short_name"" : ""CA"",
               ""types"" : [ ""administrative_area_level_1"", ""political"" ]
            },
            {
               ""long_name"" : ""United States"",
               ""short_name"" : ""US"",
               ""types"" : [ ""country"", ""political"" ]
            }
         ],
         ""formatted_address"" : ""Los Angeles, CA, USA"",
         ""geometry"" : {
            ""bounds"" : {
               ""northeast"" : {
                  ""lat"" : 34.82319290,
                  ""lng"" : -117.6456040
               },
               ""southwest"" : {
                  ""lat"" : 32.79837620,
                  ""lng"" : -118.94490370
               }
            },
            ""location"" : {
               ""lat"" : 34.38718210,
               ""lng"" : -118.11226790
            },
            ""location_type"" : ""APPROXIMATE"",
            ""viewport"" : {
               ""northeast"" : {
                  ""lat"" : 34.82319290,
                  ""lng"" : -117.6456040
               },
               ""southwest"" : {
                  ""lat"" : 32.79837620,
                  ""lng"" : -118.94490370
               }
            }
         },
         ""types"" : [ ""administrative_area_level_2"", ""political"" ]
      },
      {
         ""address_components"" : [
            {
               ""long_name"" : ""California"",
               ""short_name"" : ""CA"",
               ""types"" : [ ""administrative_area_level_1"", ""political"" ]
            },
            {
               ""long_name"" : ""United States"",
               ""short_name"" : ""US"",
               ""types"" : [ ""country"", ""political"" ]
            }
         ],
         ""formatted_address"" : ""California, USA"",
         ""geometry"" : {
            ""bounds"" : {
               ""northeast"" : {
                  ""lat"" : 42.00951690,
                  ""lng"" : -114.1312110
               },
               ""southwest"" : {
                  ""lat"" : 32.53420710,
                  ""lng"" : -124.40961950
               }
            },
            ""location"" : {
               ""lat"" : 36.7782610,
               ""lng"" : -119.41793240
            },
            ""location_type"" : ""APPROXIMATE"",
            ""viewport"" : {
               ""northeast"" : {
                  ""lat"" : 42.00951690,
                  ""lng"" : -114.1312110
               },
               ""southwest"" : {
                  ""lat"" : 32.53420710,
                  ""lng"" : -124.40961950
               }
            }
         },
         ""types"" : [ ""administrative_area_level_1"", ""political"" ]
      },
      {
         ""address_components"" : [
            {
               ""long_name"" : ""United States"",
               ""short_name"" : ""US"",
               ""types"" : [ ""country"", ""political"" ]
            }
         ],
         ""formatted_address"" : ""United States"",
         ""geometry"" : {
            ""bounds"" : {
               ""northeast"" : {
                  ""lat"" : 71.3898880,
                  ""lng"" : -66.94539480000002
               },
               ""southwest"" : {
                  ""lat"" : 18.91106430,
                  ""lng"" : 172.45469670
               }
            },
            ""location"" : {
               ""lat"" : 37.090240,
               ""lng"" : -95.7128910
            },
            ""location_type"" : ""APPROXIMATE"",
            ""viewport"" : {
               ""northeast"" : {
                  ""lat"" : 71.3898880,
                  ""lng"" : -66.94539480000002
               },
               ""southwest"" : {
                  ""lat"" : 18.91106430,
                  ""lng"" : 172.45469670
               }
            }
         },
         ""types"" : [ ""country"", ""political"" ]
      }
   ],
   ""status"" : ""OK""
}";

        public class GeoLocationResponse
        {
            public string Status { get; set; }
            public List<GeoLocationResults> Results { get; set; }
        }

        public class GeoLocationResults
        {
            public List<AddressComponent> Address_Components { get; set; }
            public string Formatted_Address { get; set; }
            public Geometry Geometry { get; set; }
            public string[] Types { get; set; }
        }

        public class Geometry
        {
            public GeometryBounds Bounds { get; set; }
            public GeometryLatLong Location { get; set; }
            public string Location_Type { get; set; }
            public GeometryBounds Viewport { get; set; }
        }

        public class GeometryBounds
        {
            public GeometryLatLong NorthEast { get; set; }
            public GeometryLatLong SouthWest { get; set; }
        }

        public class GeometryLatLong
        {
            public string Lat { get; set; }
            public string Lng { get; set; }
        }

        public class AddressComponent
        {
            public string Long_Name { get; set; }
            public string Short_Name { get; set; }
            public List<string> Types { get; set; }
        }

        [Test]
        public void Can_parse_GMaps_api()
        {
            //short for JsonSerializer.DeserializeFromString<GeoLocationResults>(Json)
            var geoApiResponse = JsonDto.FromJson<GeoLocationResponse>();
            //geoApiResponse.PrintDump();

            //"Pretty Print:".Print();
            //geoApiResponse.ToJson().IndentJson().Print();
        }
    }
}