using ServiceStack;
using System.Globalization;
using System.Runtime.Serialization;

namespace MyApp.ServiceModel;

public class AllTypes : IReturn<AllTypes>
{
    public int Id { get; set; }
    public int? NullableId { get; set; }
    public byte Byte { get; set; }
    public short Short { get; set; }
    public int Int { get; set; }
    public long Long { get; set; }
    public ushort UShort { get; set; }
    public uint UInt { get; set; }
    public ulong ULong { get; set; }
    public float Float { get; set; }
    public double Double { get; set; }
    public decimal Decimal { get; set; }
    public string String { get; set; }
    public DateTime DateTime { get; set; }
    public TimeSpan TimeSpan { get; set; }
    public DateTimeOffset DateTimeOffset { get; set; }
    public Guid Guid { get; set; }
    public char Char { get; set; }
    public KeyValuePair<string, string> KeyValuePair { get; set; }
    public DateTime? NullableDateTime { get; set; }
    public TimeSpan? NullableTimeSpan { get; set; }
    public List<string> StringList { get; set; }
    public string[] StringArray { get; set; }
    public Dictionary<string, string> StringMap { get; set; }
    public Dictionary<int, string> IntStringMap { get; set; }
    public SubType SubType { get; set; }
    public Point Point { get; set; }

    [DataMember(Name = "aliasedName")]
    public string OriginalName { get; set; }

    public static AllTypes Create(int i)
    {
        return new AllTypes
        {
            Byte = (byte)i,
            Char = (char)i,
            DateTime = new DateTime(2000 + i % 2000, 1, 1),
            Decimal = i * 1.1m,
            Double = i * 1.1d,
            Float = i * 1.1f,
            Guid = System.Guid.NewGuid(),
            Int = i,
            Long = i,
            Short = (short)i,
            String = i.ToString(),
            TimeSpan = new TimeSpan(i, 1, 1, 1),
            UInt = (uint)i,
            ULong = (ulong)i,
        };
    }

    public override bool Equals(object obj)
    {
        var other = obj as AllTypes;
        if (other == null) return false;

        return this.Byte == other.Byte
            && this.Char == other.Char
            && this.DateTime == other.DateTime
            && this.Decimal == other.Decimal
            && this.Double == other.Double
            && this.Float == other.Float
            && this.Guid == other.Guid
            && this.Int == other.Int
            && this.Long == other.Long
            && this.Short == other.Short
            && this.String == other.String
            && this.TimeSpan == other.TimeSpan
            && this.UInt == other.UInt
            && this.ULong == other.ULong;
    }

    public override int GetHashCode() => base.GetHashCode();
}

public class AllCollectionTypes
{
    public int[] IntArray { get; set; }
    public List<int> IntList { get; set; }

    public string[] StringArray { get; set; }
    public List<string> StringList { get; set; }

    public Poco[] PocoArray { get; set; }
    public List<Poco> PocoList { get; set; }

    public byte?[] NullableByteArray { get; set; }
    public List<byte?> NullableByteList { get; set; }

    public DateTime?[] NullableDateTimeArray { get; set; }
    public List<DateTime?> NullableDateTimeList { get; set; }

    public Dictionary<string, List<Poco>> PocoLookup { get; set; }
    public Dictionary<string, List<Dictionary<string, Poco>>> PocoLookupMap { get; set; }
}
public class SubType
{
    public int Id { get; set; }
    public string Name { get; set; }
}


public class Poco
{
    public string Name { get; set; }
}

public struct Point
{
    public Point(double x = 0, double y = 0) : this()
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

    public double X { get; set; }
    public double Y { get; set; }

    public override string ToString()
    {
        return X.ToString(CultureInfo.InvariantCulture) + "," + Y.ToString(CultureInfo.InvariantCulture);
    }
}

public class HelloAllTypes : IReturn<HelloAllTypesResponse>
{
    public string Name { get; set; }
    public AllTypes AllTypes { get; set; }
    public AllCollectionTypes AllCollectionTypes { get; set; }
}

public class HelloAllTypesResponse
{
    public string Result { get; set; }
    public AllTypes AllTypes { get; set; }
    public AllCollectionTypes AllCollectionTypes { get; set; }
}
