using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.DataModel;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Aws.DynamoDbTests.Shared
{
    public class Customer
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public int? Age { get; set; }
        public string Nationality { get; set; }

        public CustomerAddress PrimaryAddress { get; set; }

        public List<Order> Orders { get; set; }

        protected bool Equals(Customer other)
        {
            return Id == other.Id 
                && string.Equals(Name, other.Name) 
                && Age == other.Age 
                && string.Equals(Nationality, other.Nationality) 
                && Equals(PrimaryAddress, other.PrimaryAddress) 
                && Orders.EquivalentTo(other.Orders);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Customer) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode*397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ Age.GetHashCode();
                hashCode = (hashCode*397) ^ (Nationality != null ? Nationality.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (PrimaryAddress != null ? PrimaryAddress.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Orders != null ? Orders.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class CustomerAddress
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }

        protected bool Equals(CustomerAddress other)
        {
            return Id == other.Id &&
                CustomerId == other.CustomerId &&
                string.Equals(AddressLine1, other.AddressLine1) &&
                string.Equals(AddressLine2, other.AddressLine2) &&
                string.Equals(City, other.City) &&
                string.Equals(State, other.State) &&
                string.Equals(Country, other.Country);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CustomerAddress)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ CustomerId;
                hashCode = (hashCode * 397) ^ (AddressLine1 != null ? AddressLine1.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (AddressLine2 != null ? AddressLine2.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (City != null ? City.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (State != null ? State.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Country != null ? Country.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class Order
    {
        [AutoIncrement]
        public int Id { get; set; }

        [References(typeof(Customer))]
        public int CustomerId { get; set; }

        public string LineItem { get; set; }

        public int Qty { get; set; }

        public virtual decimal Cost { get; set; }

        protected bool Equals(Order other)
        {
            return Id == other.Id &&
                CustomerId == other.CustomerId &&
                string.Equals(LineItem, other.LineItem) &&
                Qty == other.Qty &&
                Cost == other.Cost;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Order)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ CustomerId;
                hashCode = (hashCode * 397) ^ (LineItem != null ? LineItem.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Qty;
                hashCode = (hashCode * 397) ^ Cost.GetHashCode();
                return hashCode;
            }
        }
    }

    public class OrderWithFieldIndex : Order
    {
        [Index]
        public override decimal Cost { get; set; }
    }

    [Alias("CustomCostIndex")]
    public class OrderCostIndex : ILocalIndex<OrderWithLocalTypedIndex>
    {
        public int CustomerId { get; set; }
        [Index]
        public decimal Cost { get; set; }
        public int Id { get; set; }
        public int Qty { get; set; }
    }

    [References(typeof(OrderCostIndex))]
    public class OrderWithLocalTypedIndex : Order { }

    [CompositeKey("ProductId", "Cost")]
    public class OrderGlobalCostIndex : IGlobalIndex<OrderWithGlobalTypedIndex>
    {
        public int ProductId { get; set; }
        public decimal Cost { get; set; }
        public int Qty { get; set; }
        public int Id { get; set; }
        public string LineItem { get; set; }
    }

    [References(typeof(OrderGlobalCostIndex))]
    public class OrderWithGlobalTypedIndex : Order
    {
        public int ProductId { get; set; }
    }

    public class Country
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string CountryName { get; set; }
        public string CountryCode { get; set; }

        protected bool Equals(Country other)
        {
            return Id == other.Id
                && string.Equals(CountryName, other.CountryName)
                && string.Equals(CountryCode, other.CountryCode);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Country)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (CountryName != null ? CountryName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CountryCode != null ? CountryCode.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class Node
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Node> Children { get; set; }

        public Node() { }

        public Node(int id, string name, IEnumerable<Node> children = null)
        {
            Id = id;
            Name = name;
            if (children != null)
                Children = children.ToList();
        }

        protected bool Equals(Node other)
        {
            return Id == other.Id &&
                string.Equals(Name, other.Name) &&
                Children.EquivalentTo(other.Children);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Node)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Children != null ? Children.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class Poco
    {
        public int Id { get; set; }

        public string Title { get; set; }

        protected bool Equals(Poco other)
        {
            return Id == other.Id && string.Equals(Title, other.Title);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Poco)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ (Title != null ? Title.GetHashCode() : 0);
            }
        }
    }

    public class AllTypes<T>
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
        public DateTime? NullableDateTime { get; set; }
        public TimeSpan? NullableTimeSpan { get; set; }
        public List<string> StringList { get; set; }
        public string[] StringArray { get; set; }
        public Dictionary<string, string> StringMap { get; set; }
        public Dictionary<int, string> IntStringMap { get; set; }
        public SubType SubType { get; set; }
        public T GenericType { get; set; }

        protected bool Equals(AllTypes<T> other)
        {
            return Id == other.Id && NullableId == other.NullableId
                && Byte == other.Byte
                && Short == other.Short
                && Int == other.Int
                && Long == other.Long
                && UShort == other.UShort
                && UInt == other.UInt
                && ULong == other.ULong
                && Float.Equals(other.Float)
                && Double.Equals(other.Double)
                && Decimal == other.Decimal
                && string.Equals(String, other.String)
                && DateTime.Equals(other.DateTime)
                && TimeSpan.Equals(other.TimeSpan)
                && DateTimeOffset.Equals(other.DateTimeOffset)
                && Guid.Equals(other.Guid)
                && Char == other.Char
                && NullableDateTime.Equals(other.NullableDateTime)
                && NullableTimeSpan.Equals(other.NullableTimeSpan)
                && StringList.EquivalentTo(other.StringList)
                && StringArray.EquivalentTo(other.StringArray)
                && StringMap.EquivalentTo(other.StringMap)
                && IntStringMap.EquivalentTo(other.IntStringMap)
                && Equals(SubType, other.SubType)
                && Equals(GenericType, other.GenericType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AllTypes<T>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ NullableId.GetHashCode();
                hashCode = (hashCode * 397) ^ Byte.GetHashCode();
                hashCode = (hashCode * 397) ^ Short.GetHashCode();
                hashCode = (hashCode * 397) ^ Int;
                hashCode = (hashCode * 397) ^ Long.GetHashCode();
                hashCode = (hashCode * 397) ^ UShort.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)UInt;
                hashCode = (hashCode * 397) ^ ULong.GetHashCode();
                hashCode = (hashCode * 397) ^ Float.GetHashCode();
                hashCode = (hashCode * 397) ^ Double.GetHashCode();
                hashCode = (hashCode * 397) ^ Decimal.GetHashCode();
                hashCode = (hashCode * 397) ^ (String != null ? String.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ DateTime.GetHashCode();
                hashCode = (hashCode * 397) ^ TimeSpan.GetHashCode();
                hashCode = (hashCode * 397) ^ DateTimeOffset.GetHashCode();
                hashCode = (hashCode * 397) ^ Guid.GetHashCode();
                hashCode = (hashCode * 397) ^ Char.GetHashCode();
                hashCode = (hashCode * 397) ^ NullableDateTime.GetHashCode();
                hashCode = (hashCode * 397) ^ NullableTimeSpan.GetHashCode();
                hashCode = (hashCode * 397) ^ (StringList != null ? StringList.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StringArray != null ? StringArray.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StringMap != null ? StringMap.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (IntStringMap != null ? IntStringMap.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SubType != null ? SubType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (GenericType != null ? GenericType.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class AllTypes
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
        public DateTime? NullableDateTime { get; set; }
        public TimeSpan? NullableTimeSpan { get; set; }
        public List<string> StringList { get; set; }
        public string[] StringArray { get; set; }
        public Dictionary<string, string> StringMap { get; set; }
        public Dictionary<int, string> IntStringMap { get; set; }
        public SubType SubType { get; set; }

        protected bool Equals(AllTypes other)
        {
            return Id == other.Id && NullableId == other.NullableId
                && Byte == other.Byte
                && Short == other.Short
                && Int == other.Int
                && Long == other.Long
                && UShort == other.UShort
                && UInt == other.UInt
                && ULong == other.ULong
                && Float.Equals(other.Float)
                && Double.Equals(other.Double)
                && Decimal == other.Decimal
                && string.Equals(String, other.String)
                && DateTime.Equals(other.DateTime)
                && TimeSpan.Equals(other.TimeSpan)
                && DateTimeOffset.Equals(other.DateTimeOffset)
                && Guid.Equals(other.Guid)
                && Char == other.Char
                && NullableDateTime.Equals(other.NullableDateTime)
                && NullableTimeSpan.Equals(other.NullableTimeSpan)
                && StringList.EquivalentTo(other.StringList)
                && StringArray.EquivalentTo(other.StringArray)
                && StringMap.EquivalentTo(other.StringMap)
                && IntStringMap.EquivalentTo(other.IntStringMap)
                && Equals(SubType, other.SubType);
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
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ NullableId.GetHashCode();
                hashCode = (hashCode * 397) ^ Byte.GetHashCode();
                hashCode = (hashCode * 397) ^ Short.GetHashCode();
                hashCode = (hashCode * 397) ^ Int;
                hashCode = (hashCode * 397) ^ Long.GetHashCode();
                hashCode = (hashCode * 397) ^ UShort.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)UInt;
                hashCode = (hashCode * 397) ^ ULong.GetHashCode();
                hashCode = (hashCode * 397) ^ Float.GetHashCode();
                hashCode = (hashCode * 397) ^ Double.GetHashCode();
                hashCode = (hashCode * 397) ^ Decimal.GetHashCode();
                hashCode = (hashCode * 397) ^ (String != null ? String.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ DateTime.GetHashCode();
                hashCode = (hashCode * 397) ^ TimeSpan.GetHashCode();
                hashCode = (hashCode * 397) ^ DateTimeOffset.GetHashCode();
                hashCode = (hashCode * 397) ^ Guid.GetHashCode();
                hashCode = (hashCode * 397) ^ Char.GetHashCode();
                hashCode = (hashCode * 397) ^ NullableDateTime.GetHashCode();
                hashCode = (hashCode * 397) ^ NullableTimeSpan.GetHashCode();
                hashCode = (hashCode * 397) ^ (StringList != null ? StringList.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StringArray != null ? StringArray.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StringMap != null ? StringMap.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (IntStringMap != null ? IntStringMap.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SubType != null ? SubType.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class SubType
    {
        public int Id { get; set; }
        public string Name { get; set; }

        protected bool Equals(SubType other)
        {
            return Id == other.Id
                && string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SubType)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }
    }

    public class Collection
    {
        public int Id { get; set; }
        public string Title { get; set; }

        public string[] ArrayStrings { get; set; }
        public HashSet<string> SetStrings { get; set; }
        public List<string> ListStrings { get; set; }

        public int[] ArrayInts { get; set; }
        public HashSet<int> SetInts { get; set; }
        public List<int> ListInts { get; set; }

        public Poco[] ArrayPocos { get; set; }
        public List<Poco> ListPocos { get; set; }

        public Dictionary<int, int> DictionaryInts { get; set; }
        public Dictionary<string, string> DictionaryStrings { get; set; }

        public Dictionary<string, List<Poco>> PocoLookup { get; set; }
        public Dictionary<string, List<Dictionary<string, Poco>>> PocoLookupMap { get; set; }

        public Collection InitStrings(params string[] strings)
        {
            ArrayStrings = strings;
            SetStrings = new HashSet<string>(strings);
            ListStrings = new List<string>(strings);
            DictionaryStrings = new Dictionary<string, string>();
            strings.Each(x => DictionaryStrings[x] = x);
            PocoLookup = new Dictionary<string, List<Poco>>();
            strings.Each(x => PocoLookup[x] = new List<Poco> { new Poco { Id = 1, Title = x } });
            PocoLookupMap = new Dictionary<string, List<Dictionary<string, Poco>>>();
            strings.Each(x => PocoLookupMap[x] = new List<Dictionary<string, Poco>> {
                new Dictionary<string, Poco> { { x, new Poco { Id = 1, Title = x } } }
            });
            return this;
        }

        public Collection InitInts(params int[] ints)
        {
            ArrayInts = ints;
            SetInts = new HashSet<int>(ints);
            ListInts = new List<int>(ints);
            DictionaryInts = new Dictionary<int, int>();
            ints.Each(x => DictionaryInts[x] = x);
            return this;
        }

        protected bool Equals(Collection other)
        {
            return Id == other.Id
                && ArrayStrings.EquivalentTo(other.ArrayStrings)
                && SetStrings.EquivalentTo(other.SetStrings)
                && ListStrings.EquivalentTo(other.ListStrings)
                && ArrayInts.EquivalentTo(other.ArrayInts)
                && SetInts.EquivalentTo(other.SetInts)
                && ListInts.EquivalentTo(other.ListInts)
                && DictionaryInts.EquivalentTo(other.DictionaryInts)
                && DictionaryStrings.EquivalentTo(other.DictionaryStrings)
                && PocoLookup.EquivalentTo(other.PocoLookup, (a, b) => a.EquivalentTo(b))
                && PocoLookupMap.EquivalentTo(other.PocoLookupMap, (a, b) => a.EquivalentTo(b, (m1, m2) => m1.EquivalentTo(m2)));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Collection)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (ArrayStrings != null ? ArrayStrings.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SetStrings != null ? SetStrings.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ListStrings != null ? ListStrings.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ArrayInts != null ? ArrayInts.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SetInts != null ? SetInts.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ListInts != null ? ListInts.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DictionaryInts != null ? DictionaryInts.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DictionaryStrings != null ? DictionaryStrings.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PocoLookup != null ? PocoLookup.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PocoLookupMap != null ? PocoLookupMap.GetHashCode() : 0);
                return hashCode;
            }
        }
    }



    public class TableWithDynamoAttributes
    {
        public string A { get; set; }
        public string B { get; set; }

        [DynamoDBRangeKey]
        public string C { get; set; }

        [DynamoDBHashKey]
        public string D { get; set; }

        public string E { get; set; }
    }

    [CompositeKey("D", "C")]
    public class TableWithCompositeKey
    {
        public string A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
        public string D { get; set; }
        public string E { get; set; }
    }

    public class GlobalIndexWithInterfaceAttrs : IGlobalIndex<TableWithTypedGlobalIndex>
    {
        public string A { get; set; }
        [PrimaryKey]
        public string B { get; set; }
        public string C { get; set; }
        [RangeKey]
        public string D { get; set; }
    }

    [References(typeof(GlobalIndexWithInterfaceAttrs))]
    public class TableWithTypedGlobalIndex
    {
        public string A { get; set; }
        public string B { get; set; }
        [RangeKey]
        public string C { get; set; }
        [HashKey]
        public string D { get; set; }
        public string E { get; set; }
    }

    public class TableWithConventionNames
    {
        public string A { get; set; }
        public string HashKey { get; set; }
        public string RangeKey { get; set; }
    }

    public class TableWithIdConvention
    {
        public string A { get; set; }
        public string Id { get; set; }
        public string RangeKey { get; set; }
    }

    [ProvisionedThroughput(ReadCapacityUnits = 100, WriteCapacityUnits = 50)]
    public class TableWithProvision
    {
        public string Id { get; set; }
        public string A { get; set; }
    }

    [ProvisionedThroughput(ReadCapacityUnits = 100, WriteCapacityUnits = 50)]
    public class GlobalIndexProvision : IGlobalIndex<TableWithGlobalIndexProvision>
    {
        [PrimaryKey]
        public string A { get; set; }
        [Index]
        public string Id { get; set; }
    }

    [References(typeof(GlobalIndexProvision))]
    public class TableWithGlobalIndexProvision
    {
        public string Id { get; set; }
        public string A { get; set; }
    }
}