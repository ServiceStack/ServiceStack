using System;
using System.Collections.Generic;
using ServiceStack;
using CheckGrpc.ServiceModel;
using System.Runtime.Serialization;

namespace CheckGrpc.ServiceModel
{
    [Route("/hello")]
    [Route("/hello/{Name}")]
    [DataContract]
    public class Hello : IReturn<HelloResponse>
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }
    }

    [DataContract]
    public class HelloResponse
    {
        [DataMember(Order = 1)]
        public string Result { get; set; }
        
        [DataMember(Order = 2)]
        public ResponseStatus ResponseStatus { get; set; }
    }
    
    [DataContract]
    public class HelloAllTypes : IReturn<HelloAllTypesResponse>
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }
        [DataMember(Order = 2)]
        public AllTypes AllTypes { get; set; }
        [DataMember(Order = 3)]
        public AllCollectionTypes AllCollectionTypes { get; set; }
    }

    [DataContract]
    public class HelloAllTypesResponse
    {
        [DataMember(Order = 1)]
        public string Result { get; set; }
        [DataMember(Order = 2)]
        public AllTypes AllTypes { get; set; }
        [DataMember(Order = 3)]
        public AllCollectionTypes AllCollectionTypes { get; set; }
        [DataMember(Order = 4)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class AllTypes : IReturn<AllTypes>
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public int? NullableId { get; set; }
        [DataMember(Order = 3)]
        public byte Byte { get; set; }
        [DataMember(Order = 4)]
        public short Short { get; set; }
        [DataMember(Order = 5)]
        public int Int { get; set; }
        [DataMember(Order = 6)]
        public long Long { get; set; }
        [DataMember(Order = 7)]
        public UInt16 UShort { get; set; }
        [DataMember(Order = 8)]
        public uint UInt { get; set; }
        [DataMember(Order = 9)]
        public ulong ULong { get; set; }
        [DataMember(Order = 10)]
        public float Float { get; set; }
        [DataMember(Order = 11)]
        public double Double { get; set; }
        [DataMember(Order = 12)]
        public decimal Decimal { get; set; }
        [DataMember(Order = 13)]
        public string String { get; set; }
        [DataMember(Order = 14)]
        public DateTime DateTime { get; set; }
        [DataMember(Order = 15)]
        public TimeSpan TimeSpan { get; set; }
        [DataMember(Order = 16)]
        public DateTimeOffset DateTimeOffset { get; set; }
        [DataMember(Order = 17)]
        public Guid Guid { get; set; }
        [DataMember(Order = 18)]
        public Char Char { get; set; }
        [DataMember(Order = 19)]
        public KeyValuePair<string,string> KeyValuePair { get; set; }
        [DataMember(Order = 20)]
        public DateTime? NullableDateTime { get; set; }
        [DataMember(Order = 21)]
        public TimeSpan? NullableTimeSpan { get; set; }
        [DataMember(Order = 22)]
        public List<string> StringList { get; set; }
        [DataMember(Order = 23)]
        public string[] StringArray { get; set; }
        [DataMember(Order = 24)]
        public Dictionary<string, string> StringMap { get; set; }
        [DataMember(Order = 25)]
        public Dictionary<int, string> IntStringMap { get; set; }
        [DataMember(Order = 26)]
        public SubType SubType { get; set; }
        [DataMember(Order = 27)]
        public Point Point { get; set; }

        [DataMember(Name = "aliasedName", Order = 28)]
        public string OriginalName { get; set; }
    }

    [DataContract]
    public class AllCollectionTypes
    {
        [DataMember(Order = 1)]
        public int[] IntArray { get; set; }
        [DataMember(Order = 2)]
        public List<int> IntList { get; set; }

        [DataMember(Order = 3)]
        public string[] StringArray { get; set; }
        [DataMember(Order = 4)]
        public List<string> StringList { get; set; }

        [DataMember(Order = 5)]
        public Poco[] PocoArray { get; set; }
        [DataMember(Order = 6)]
        public List<Poco> PocoList { get; set; }

        [DataMember(Order = 7)]
        public byte?[] NullableByteArray { get; set; }
        [DataMember(Order = 8)]
        public List<byte?> NullableByteList { get; set; }

        [DataMember(Order = 9)]
        public DateTime?[] NullableDateTimeArray { get; set; }
        [DataMember(Order = 10)]
        public List<DateTime?> NullableDateTimeList { get; set; }

//        [DataMember(Order = 11)]
//        public Dictionary<string, List<Poco>> PocoLookup { get; set; }
//        [DataMember(Order = 12)]
//        public Dictionary<string, List<Dictionary<string,Poco>>> PocoLookupMap { get; set; } 
    }

    [DataContract]
    public class Poco
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }
    }

    [DataContract]
    public class SubType
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string Name { get; set; }
    }
    
    [DataContract]
    public struct Point
    {
        public Point(double x=0, double y=0) : this()
        {
            X = x;
            Y = y;
        }

        public Point(string point) : this()
        {
            var parts = point.Split(',');
            X = double.Parse(parts[0]);
            Y = double.Parse(parts[1]);
        }

        [DataMember(Order = 1)]
        public double X { get; set; }
        [DataMember(Order = 2)]
        public double Y { get; set; }

        public override string ToString()
        {
            return X.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
    
}

namespace CheckGrpc.ServiceInterface
{
    public class MyServices : Service
    {
        public object Any(Hello request)
        {
            return new HelloResponse { Result = $"Hello, {request.Name}!" };
        }

        public object Any(HelloAllTypes request)
        {
            return new HelloAllTypesResponse
            {
                AllTypes = request.AllTypes,
                AllCollectionTypes = request.AllCollectionTypes, 
                Result = request.Name
            };
        }

        public object Any(AllTypes request)
        {
            return request;
        }
    }
}