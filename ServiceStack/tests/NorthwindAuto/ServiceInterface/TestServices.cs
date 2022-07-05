using ServiceStack;

namespace MyApp.ServiceInterface;

[Tag("Test")]
public class AllTypes : IReturn<AllTypes>
{
    public int Id { get; set; }
    public int? NullableId { get; set; }
    public byte Byte { get; set; }
    public short Short { get; set; }
    public int Int { get; set; }
    public long Long { get; set; }
    public UInt16 UShort { get; set; }
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
    public Char Char { get; set; }
    public KeyValuePair<string, string> KeyValuePair { get; set; }
    public DateTime? NullableDateTime { get; set; }
    public TimeSpan? NullableTimeSpan { get; set; }
    public List<string> StringList { get; set; }
    public string[] StringArray { get; set; }
    public Dictionary<string, string> StringMap { get; set; }
    public Dictionary<int, string> IntStringMap { get; set; }
    public SubType SubType { get; set; }
}

[Tag("Test")]
public class AllCollectionTypes : IReturn<AllCollectionTypes>
{
    public int[] IntArray { get; set; }
    public List<int> IntList { get; set; }

    public string[] StringArray { get; set; }
    public List<string> StringList { get; set; }

    public float[] FloatArray { get; set; }
    public List<double> DoubleList { get; set; }

    public byte[] ByteArray { get; set; }
    public char[] CharArray { get; set; }
    public List<decimal> DecimalList { get; set; }

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

public class SubType
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class ProfileGen {}


public class TestServices : Service
{
    public object Any(AllTypes request) => request;

    public object Any(AllCollectionTypes request) => request;

    public object Any(ProfileGen request)
    {
        Cache.Set("foo", "bar");
        Cache.Set("bax", 1);
        Cache.Set("qux", new Poco { Name = nameof(Poco) });
        
        Redis.IncrementValueBy("incr", 10);
        
        Redis.Hashes["hash"].AddRange(new Dictionary<string, string> {
            {"foo","bar"}, 
            {"baz","1"}
        });
        
        return request;
    }
}