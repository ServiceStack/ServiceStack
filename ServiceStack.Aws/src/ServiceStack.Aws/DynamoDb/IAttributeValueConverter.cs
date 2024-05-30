// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Globalization;
using Amazon.DynamoDBv2.Model;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;
using ServiceStack.Text.Common;

namespace ServiceStack.Aws.DynamoDb;

public interface IAttributeValueConverter
{
    AttributeValue ToAttributeValue(object value, Type type);
    object FromAttributeValue(AttributeValue attrValue, Type type);
}

public class DateTimeConverter : IAttributeValueConverter
{
    public virtual AttributeValue ToAttributeValue(object value, Type type)
    {
        var iso8601Date = ((DateTime)value).ToString("o", CultureInfo.InvariantCulture);
        return new AttributeValue { S = iso8601Date };
    }

    public virtual object FromAttributeValue(AttributeValue attrValue, Type type)
    {
        var iso8601String = attrValue.S;
        var date = iso8601String == null
            ? null
            : DateTimeSerializer.ParseManual(iso8601String, DateTimeKind.Utc);

        return date;
    }
}

public class EnumConverter : IAttributeValueConverter
{
    public AttributeValue ToAttributeValue(object value, Type type)
    {
        var treatAsInt = JsConfig.TreatEnumAsInteger
                         || type.HasAttribute<EnumAsIntAttribute>()
                         || type.HasAttribute<FlagsAttribute>();

        return treatAsInt
            ? new AttributeValue { N = ((int)value).ToString() }
            : new AttributeValue(value.ToString());
    }

    public object FromAttributeValue(AttributeValue attrValue, Type type)
    {
        if (attrValue.S != null)
            return Enum.Parse(Nullable.GetUnderlyingType(type) ?? type, attrValue.S);

        if (attrValue.N != null)
            return Enum.ToObject(type, int.Parse(attrValue.N));

        return type.GetDefaultValue();
    }
}