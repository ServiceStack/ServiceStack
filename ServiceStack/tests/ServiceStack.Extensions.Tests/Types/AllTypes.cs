using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Extensions.Tests.Types;

public class HelloSubAllTypes
    : AllTypesBase, IReturn<SubAllTypes>
{
    public virtual int Hierarchy { get; set; }

    protected bool Equals(HelloSubAllTypes other) => base.Equals(other) && Hierarchy == other.Hierarchy;

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((HelloSubAllTypes)obj);
    }

    public override int GetHashCode() => Hierarchy;
}

public abstract class AllTypesBase
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

    protected bool Equals(AllTypesBase other)
    {
        return Id == other.Id && NullableId == other.NullableId && Byte == other.Byte && Short == other.Short &&
               Int == other.Int && Long == other.Long && UShort == other.UShort && UInt == other.UInt &&
               ULong == other.ULong && Float.Equals(other.Float) && Double.Equals(other.Double) &&
               Decimal == other.Decimal && String == other.String && DateTime.Equals(other.DateTime) &&
               TimeSpan.Equals(other.TimeSpan) && DateTimeOffset.Equals(other.DateTimeOffset) &&
               Guid.Equals(other.Guid) && Char == other.Char && KeyValuePair.Equals(other.KeyValuePair) &&
               Nullable.Equals(NullableDateTime, other.NullableDateTime) &&
               Nullable.Equals(NullableTimeSpan, other.NullableTimeSpan) && Equals(StringList, other.StringList) &&
               Equals(StringArray, other.StringArray) && Equals(StringMap, other.StringMap) &&
               Equals(IntStringMap, other.IntStringMap) && Equals(SubType, other.SubType);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((AllTypesBase)obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Id);
        hashCode.Add(NullableId);
        hashCode.Add(Byte);
        hashCode.Add(Short);
        hashCode.Add(Int);
        hashCode.Add(Long);
        hashCode.Add(UShort);
        hashCode.Add(UInt);
        hashCode.Add(ULong);
        hashCode.Add(Float);
        hashCode.Add(Double);
        hashCode.Add(Decimal);
        hashCode.Add(String);
        hashCode.Add(DateTime);
        hashCode.Add(TimeSpan);
        hashCode.Add(DateTimeOffset);
        hashCode.Add(Guid);
        hashCode.Add(Char);
        hashCode.Add(KeyValuePair);
        hashCode.Add(NullableDateTime);
        hashCode.Add(NullableTimeSpan);
        hashCode.Add(StringList);
        hashCode.Add(StringArray);
        hashCode.Add(StringMap);
        hashCode.Add(IntStringMap);
        hashCode.Add(SubType);
        return hashCode.ToHashCode();
    }
}

public partial class SubAllTypes
    : AllTypesBase
{
    public virtual int Hierarchy { get; set; }

    protected bool Equals(SubAllTypes other) => base.Equals(other) && Hierarchy == other.Hierarchy;

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SubAllTypes)obj);
    }

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Hierarchy);
}

public class AllTypes : IReturn<AllTypes>
{
    public int Id { get; set; }
    public int? NullableId { get; set; }
    public bool Boolean { get; set; }
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
    public DateTime LocalDateTime { get; set; }
    public TimeSpan TimeSpan { get; set; }
    public DateTimeOffset DateTimeOffset { get; set; }
#if NET6_0_OR_GREATER
    public DateOnly DateOnly { get; set; }
    public TimeOnly TimeOnly { get; set; }
#endif
    public Guid Guid { get; set; }
    public Char Char { get; set; }
    public KeyValuePair<string, string> KeyValuePair { get; set; }
    public DateTime? NullableDateTime { get; set; }
    public TimeSpan? NullableTimeSpan { get; set; }
    public List<string> StringList { get; set; }
    public string[] StringArray { get; set; }
    public Dictionary<string, string> StringMap { get; set; }
    public Dictionary<int, string> IntStringMap { get; set; }
    public byte?[] NullableBytes { get; set; }
    public SubType SubType { get; set; }

    public static AllTypes Create(int i)
    {
        return new()
        {
            Id = 1,
            NullableId = 1,
            Boolean = i % 2 == 0,
            Byte = (byte)i,
            Short = (short)i,
            Int = i,
            Long = i,
            UShort = (ushort)i,
            UInt = (uint)i,
            ULong = (ulong)i,
            Float = i,
            Double = i,
            Decimal = i,
            String = i.ToString(),
            DateTime = new(i % 2000, 1 + (i % 12), i % 28, i % 60, i % 60, i % 60),
            LocalDateTime = new(i % 2000, 1 + (i % 12), i % 28, i % 60, i % 60, i % 60, DateTimeKind.Local),
            TimeSpan = new(i, i, i, i),
            DateTimeOffset = new(i % 2000, 1 + (i % 12), i % 28, i % 60, i % 60, i % 60, TimeSpan.FromHours(i % 24)),
#if NET6_0_OR_GREATER
            DateOnly = new(i % 2000, 1 + (i % 12), i % 28),
            TimeOnly = new(i % 60, i % 60, i % 60),
#endif
            Guid = Guid.NewGuid(),
            Char = (char)i,
            KeyValuePair = new(i.ToString(), i.ToString()),
            NullableDateTime = new(i % 2000, 1 + (i % 12), i % 28, i % 60, i % 60, i % 60, DateTimeKind.Utc),
            NullableTimeSpan = new(i, i, i, i),
            StringList = [1.ToString()],
            //StringArray = [1.ToString()],
            IntStringMap = new() { [i] = i.ToString() },
            NullableBytes = [(byte)(i % 7)],
            SubType = SubType.Create(i),
        };
    }

    protected bool Equals(AllTypes other)
    {
        return Id == other.Id && NullableId == other.NullableId && Boolean == other.Boolean && Byte == other.Byte &&
               Short == other.Short && Int == other.Int && Long == other.Long && UShort == other.UShort &&
               UInt == other.UInt && ULong == other.ULong && Float.Equals(other.Float) && Double.Equals(other.Double) &&
               Decimal == other.Decimal && String == other.String && DateTime.Equals(other.DateTime) &&
               LocalDateTime.Equals(other.LocalDateTime) && TimeSpan.Equals(other.TimeSpan) &&
               DateTimeOffset.Equals(other.DateTimeOffset) && 
#if NET6_0_OR_GREATER
               DateOnly.Equals(other.DateOnly) && TimeOnly.Equals(other.TimeOnly) &&
#endif
               Guid.Equals(other.Guid) && Char == other.Char && 
               KeyValuePair.Equals(other.KeyValuePair) &&
               Nullable.Equals(NullableDateTime, other.NullableDateTime) &&
               Nullable.Equals(NullableTimeSpan, other.NullableTimeSpan) &&
               StringList.EquivalentTo(other.StringList) &&
               StringArray.EquivalentTo(other.StringArray) &&
               StringMap.EquivalentTo(other.StringMap) &&
               IntStringMap.EquivalentTo(other.IntStringMap) &&
               NullableBytes.EquivalentTo(other.NullableBytes) &&
               Equals(SubType, other.SubType);
    }

    public void AssertEquals(AllTypes other)
    {
        Assert.That(Id, Is.EqualTo(other.Id));
        Assert.That(NullableId, Is.EqualTo(other.NullableId));
        Assert.That(Boolean, Is.EqualTo(other.Boolean));
        Assert.That(Byte, Is.EqualTo(other.Byte));
        Assert.That(Short, Is.EqualTo(other.Short));
        Assert.That(Int, Is.EqualTo(other.Int));
        Assert.That(Long, Is.EqualTo(other.Long));
        Assert.That(UShort, Is.EqualTo(other.UShort));
        Assert.That(Decimal, Is.EqualTo(other.Decimal));
        Assert.That(String, Is.EqualTo(other.String));
        Assert.That(DateTime, Is.EqualTo(other.DateTime));
        Assert.That(DateTimeOffset, Is.EqualTo(other.DateTimeOffset));
#if NET6_0_OR_GREATER
        Assert.That(DateOnly, Is.EqualTo(other.DateOnly));
        Assert.That(TimeOnly, Is.EqualTo(other.TimeOnly));
#endif
        Assert.That(Guid, Is.EqualTo(other.Guid));
        Assert.That(Char, Is.EqualTo(other.Char));
        Assert.That(KeyValuePair, Is.EqualTo(other.KeyValuePair));
        Assert.That(NullableDateTime, Is.EqualTo(other.NullableDateTime));
        Assert.That(NullableTimeSpan, Is.EqualTo(other.NullableTimeSpan));
        if (StringList is not null)
            Assert.That(StringList, Is.EquivalentTo(other.StringList));
        else
            Assert.That(other.StringList, Is.Null);
        if (StringArray is not null)
            Assert.That(StringArray, Is.EquivalentTo(other.StringArray));
        else
            Assert.That(other.StringArray, Is.Null);
        if (IntStringMap is not null)
            Assert.That(IntStringMap, Is.EquivalentTo(other.IntStringMap));
        else
            Assert.That(other.IntStringMap, Is.Null);
        if (NullableBytes is not null)
            Assert.That(NullableBytes, Is.EquivalentTo(other.NullableBytes));
        else
            Assert.That(other.NullableBytes, Is.Null);
        Assert.That(SubType, Is.EqualTo(other.SubType));
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((AllTypes)obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Id);
        hashCode.Add(NullableId);
        hashCode.Add(Boolean);
        hashCode.Add(Byte);
        hashCode.Add(Short);
        hashCode.Add(Int);
        hashCode.Add(Long);
        hashCode.Add(UShort);
        hashCode.Add(UInt);
        hashCode.Add(ULong);
        hashCode.Add(Float);
        hashCode.Add(Double);
        hashCode.Add(Decimal);
        hashCode.Add(String);
        hashCode.Add(DateTime);
        hashCode.Add(TimeSpan);
        hashCode.Add(DateTimeOffset);
        hashCode.Add(Guid);
        hashCode.Add(Char);
        hashCode.Add(KeyValuePair);
        hashCode.Add(NullableDateTime);
        hashCode.Add(NullableTimeSpan);
        hashCode.Add(StringList);
        hashCode.Add(StringArray);
        hashCode.Add(StringMap);
        hashCode.Add(IntStringMap);
        hashCode.Add(SubType);
        hashCode.Add(NullableBytes);
        return hashCode.ToHashCode();
    }
}

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

    protected bool Equals(AllCollectionTypes other)
    {
        return Equals(IntArray, other.IntArray) && Equals(IntList, other.IntList) &&
               Equals(StringArray, other.StringArray) && Equals(StringList, other.StringList) &&
               Equals(FloatArray, other.FloatArray) && Equals(DoubleList, other.DoubleList) &&
               Equals(ByteArray, other.ByteArray) && Equals(CharArray, other.CharArray) &&
               Equals(DecimalList, other.DecimalList) && Equals(PocoArray, other.PocoArray) &&
               Equals(PocoList, other.PocoList) && Equals(PocoLookup, other.PocoLookup) &&
               Equals(PocoLookupMap, other.PocoLookupMap);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((AllCollectionTypes)obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(IntArray);
        hashCode.Add(IntList);
        hashCode.Add(StringArray);
        hashCode.Add(StringList);
        hashCode.Add(FloatArray);
        hashCode.Add(DoubleList);
        hashCode.Add(ByteArray);
        hashCode.Add(CharArray);
        hashCode.Add(DecimalList);
        hashCode.Add(PocoArray);
        hashCode.Add(PocoList);
        hashCode.Add(PocoLookup);
        hashCode.Add(PocoLookupMap);
        return hashCode.ToHashCode();
    }
}

public class HelloAllTypes : IReturn<HelloAllTypesResponse>
{
    public string Name { get; set; }
    public AllTypes AllTypes { get; set; }
    public AllCollectionTypes AllCollectionTypes { get; set; }

    protected bool Equals(HelloAllTypes other) =>
        Name == other.Name && Equals(AllTypes, other.AllTypes) && Equals(AllCollectionTypes, other.AllCollectionTypes);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((HelloAllTypes)obj);
    }

    public override int GetHashCode() => HashCode.Combine(Name, AllTypes, AllCollectionTypes);
}

public class HelloAllTypesResponse
{
    public string Result { get; set; }
    public AllTypes AllTypes { get; set; }
    public AllCollectionTypes AllCollectionTypes { get; set; }

    protected bool Equals(HelloAllTypesResponse other) => Result == other.Result && Equals(AllTypes, other.AllTypes)
        && Equals(AllCollectionTypes, other.AllCollectionTypes);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((HelloAllTypesResponse)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Result, AllTypes, AllCollectionTypes);
    }
}

public class Poco
{
    public string Name { get; set; }

    protected bool Equals(Poco other) => Name == other.Name;

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Poco)obj);
    }

    public override int GetHashCode() => (Name != null ? Name.GetHashCode() : 0);
}

public class SubType
{
    public int Id { get; set; }
    public string Name { get; set; }

    public static SubType Create(int i) => new()
    {
        Id = i,
        Name = $"Name{i}"
    };

    protected bool Equals(SubType other) => Id == other.Id && Name == other.Name;

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SubType)obj);
    }

    public override int GetHashCode() => HashCode.Combine(Id, Name);
}