// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;

namespace ServiceStack.Aws.DynamoDb;

//http://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_AttributeValue.html
public static class DynamoType
{
    public const string String = "S";
    public const string Number = "N";
    public const string Binary = "B";
    public const string Bool = "BOOL";

    public const string StringSet = "SS";
    public const string NumberSet = "NS";
    public const string BinarySet = "BS";

    public const string List = "L";
    public const string Map = "M";
    public const string Null = "Null";
}

//http://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_AttributeValueUpdate.html
public static class DynamoAction
{
    public const string Put = "PUT";
    public const string Delete = "DELETE";
    public const string Add = "ADD";
}

public static class DynamoUpdateAction
{
    public const string Set = "SET";
    public const string Remove = "REMOVE";
    public const string Add = "ADD";
    public const string Delete = "DELETE";
}

//http://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_UpdateItem.html
public static class DynamoReturn
{
    public const string None = "NONE";
    public const string AllOld = "ALL_OLD";
    public const string UpdatedOld = "UPDATED_OLD";
    public const string AllNew = "ALL_NEW";
    public const string UpdatedNew = "UPDATED_NEW";
}

public enum ReturnItem
{
    None,
    Old,
    New
}

public class DynamoExpr
{
    public const string Equal = "EQ";
    public const string NotEqual = "NE";
    public const string LessThanOrEqual = "LE";
    public const string LessThan = "LT";
    public const string GreaterThanOrEqual = "GE";
    public const string GreaterThan = "GT";
    public const string NotNull = "NOT_NULL";
    public const string Null = "NULL";
    public const string Contains = "CONTAINS";
    public const string NotContains = "NOT_CONTAINS";
    public const string BeginsWith = "BEGINS_WITH";
    public const string In = "IN";
    public const string Between = "BETWEEN";
}

public class DynamoConditionExpr
{
    public const string Equal = "=";
    public const string NotEqual = "<>";
    public const string LessThanOrEqual = "<=";
    public const string LessThan = "<";
    public const string GreaterThanOrEqual = ">=";
    public const string GreaterThan = ">";
    public const string In = "IN";
    public const string Between = "BETWEEN";
}

public class DynamoConditionFn
{
    public const string AttributeExists = "attribute_exists";
    public const string AttributeNotExists = "attribute_not_exists";
    public const string AttributeType = "attribute_type";
    public const string Contains = "contains";
    public const string BeginsWith = "begins_with";
    public const string Size = "size";
}

//http://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_CreateTable.html
public static class DynamoStatus
{
    public const string Creating = "CREATING";
    public const string Active = "ACTIVE";
    public const string Updating = "UPDATING";
    public const string Deleting = "DELETING";
}

//KeyType
public static class DynamoKey
{
    public const string Hash = "HASH";
    public const string Range = "RANGE";
}

public static class DynamoProperty
{
    public const string HashKey = "HashKey";
    public const string RangeKey = "RangeKey";
}

public static class DynamoErrors
{
    public const string NotFound = "ResourceNotFoundException";
    public const string AlreadyExists = "ResourceInUseException";
}

public static class DynamoAttributeAction
{
    public const string Put = "PUT";
    public const string Delete = "DELETE";
    public const string Add = "ADD";
}

public interface IDynamoIndex { }
public interface IDynamoIndex<T> : IDynamoIndex { }

public interface ILocalIndex<T> : IDynamoIndex<T> { }

public interface IGlobalIndex<T> : IDynamoIndex<T> { }


[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ProvisionedThroughputAttribute : AttributeBase
{
    public int ReadCapacityUnits { get; set; }
    public int WriteCapacityUnits { get; set; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class ProjectionTypeAttribute : AttributeBase
{
    public string ProjectionType { get; private set; }

    public ProjectionTypeAttribute(string projectionType)
    {
        ProjectionType = projectionType;
    }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class ExcludeNullValueAttribute : AttributeBase {}

public class DynamoId
{
    public DynamoId() {}
    public DynamoId(object hash, object range)
    {
        Hash = hash;
        Range = range;
    }

    public object Hash { get; set; }

    public object Range { get; set; }

    protected bool Equals(DynamoId other)
    {
        return Equals(Hash, other.Hash) && Equals(Range, other.Range);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DynamoId) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Hash?.GetHashCode() ?? 0)*397) ^ (Range?.GetHashCode() ?? 0);
        }
    }
}

public class DynamoUpdateItem
{
    public object Hash { get; set; }
    public object Range { get; set; }

    public Dictionary<string, object> Put { get; set; } 
    public Dictionary<string, object> Add { get; set; } 
    public string[] Delete { get; set; } 
}