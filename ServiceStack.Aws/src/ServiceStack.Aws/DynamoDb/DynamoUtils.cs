// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;

namespace ServiceStack.Aws.DynamoDb;

public static class DynamoUtils
{
    public static Dictionary<string, DynamoDBEntry> ToDynamoDbEntryMap(this Type type)
    {
        return new Dictionary<string, DynamoDBEntry>();
    }

    public static Dictionary<Type, string> Set<T>(this Dictionary<Type, string> map, string value)
    {
        map[typeof(T)] = value;
        return map;
    }

    public static List<string> AllFields(this Type type)
    {
        return type.GetPublicProperties().Select(x => x.Name).ToList();
    }

    public static bool IsGlobalIndex(this Type indexType)
    {
        return indexType.GetTypeWithGenericInterfaceOf(typeof(IGlobalIndex<>)) != null;
    }

    public static DynamoMetadataType GetIndexTable(this Type indexType)
    {
        var genericIndex = indexType.GetTypeWithGenericInterfaceOf(typeof(IDynamoIndex<>));
        if (genericIndex == null)
            return null;

        var tableType = genericIndex.GetGenericArguments().FirstOrDefault();
        return tableType != null
            ? DynamoMetadata.GetTable(tableType)
            : null;
    }

    public static ReturnValue ToReturnValue(this ReturnItem returnItem)
    {
        return returnItem == ReturnItem.New
            ? ReturnValue.ALL_NEW
            : returnItem == ReturnItem.Old
                ? ReturnValue.ALL_OLD
                : ReturnValue.NONE;
    }

    public static HashSet<string> ToStrings(this ScanResponse response, string fieldName)
    {
        var to = new HashSet<string>();
        foreach (Dictionary<string, AttributeValue> values in response.Items)
        {
            values.TryGetValue(fieldName, out var attrId);

            if (attrId?.S != null)
                to.Add(attrId.S);
        }
        return to;
    }

    public static T ConvertTo<T>(this DynamoMetadataType table,
        Dictionary<string, AttributeValue> attributeValues)
    {
        return DynamoMetadata.Converters.FromAttributeValues<T>(table, attributeValues);
    }

    public static List<T> ConvertAll<T>(this ScanResponse response)
    {
        return response.Items
            .Select(values => DynamoMetadata.GetType<T>().ConvertTo<T>(values))
            .ToList();
    }

    public static List<T> ConvertAll<T>(this QueryResponse response)
    {
        return response.Items
            .Select(values => DynamoMetadata.GetType<T>().ConvertTo<T>(values))
            .ToList();
    }

    public static List<KeySchemaElement> ToKeySchemas(this DynamoIndex index)
    {
        var to = new List<KeySchemaElement> {
            new KeySchemaElement(index.HashKey.Name, DynamoKey.Hash),
        };

        if (index.RangeKey != null)
            to.Add(new KeySchemaElement(index.RangeKey.Name, DynamoKey.Range));

        return to;
    }
}