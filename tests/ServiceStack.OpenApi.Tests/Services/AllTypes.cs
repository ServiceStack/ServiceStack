using System;
using System.Collections.Generic;

namespace ServiceStack.OpenApi.Tests.Services
{
    public class AllTypes
    {
        public int Id { get; set; }
        public int? NullableId { get; set; }
        public byte ByteProperty { get; set; }
        public short ShortProperty { get; set; }
        public int IntProperty { get; set; }
        public long LongProperty { get; set; }
        public UInt16 UShortProperty { get; set; }
        public uint UIntProperty { get; set; }
        public ulong ULongProperty { get; set; }
        public float FloatProperty { get; set; }
        public double DoubleProperty { get; set; }
        public decimal DecimalProperty { get; set; }
        public string StringProperty { get; set; }
        public DateTime DateTimeProperty { get; set; }
        public TimeSpan TimeSpanProperty { get; set; }
        public DateTimeOffset DateTimeOffsetProperty { get; set; }
        public Guid GuidProperty { get; set; }
        public Char CharProperty { get; set; }
        public KeyValuePair<string, string> KeyValuePairProperty { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public TimeSpan? NullableTimeSpan { get; set; }
        public List<string> StringList { get; set; }
        public string[] StringArray { get; set; }
        public Dictionary<string, string> StringMap { get; set; }
        public Dictionary<int, string> IntStringMap { get; set; }
        public SubType SubType { get; set; }
    }

    public class AllCollectionTypes
    {
        public int[] IntArray { get; set; }
        public List<int> IntList { get; set; }

        public string[] StringArray { get; set; }
        public List<string> StringList { get; set; }

        public Poco[] PocoArray { get; set; }
        public List<Poco> PocoList { get; set; }

        public Dictionary<string, List<Poco>> PocoLookup { get; set; }
        public Dictionary<string, List<Dictionary<string, Poco>>> PocoLookupMap { get; set; }
    }

    public class Poco
    {
        public string Name { get; set; }
    }

    public abstract class HelloBase
    {
        public int Id { get; set; }
    }

    public abstract class HelloResponseBase
    {
        public int RefId { get; set; }
    }

    public class HelloType
    {
        public string Result { get; set; }
    }

    public abstract class HelloWithReturnResponse
    {
        public string Result { get; set; }
    }

    public class SubType
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}