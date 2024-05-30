// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Aws.DynamoDb;

public interface IDynamoCommonQuery
{
    string FilterExpression { get; set; }
    string ProjectionExpression { get; set; }
    Dictionary<string, string> ExpressionAttributeNames { get; }

    void AddArguments(Dictionary<string, object> args);
}

public static class DynamoQueryUtils
{
    public static void SelectFields(this IDynamoCommonQuery q, IEnumerable<string> fields)
    {
        var fieldLabels = fields.Select(field => GetFieldLabel(q, field)).ToArray();
        q.ProjectionExpression = fieldLabels.Length > 0
            ? string.Join(", ", fieldLabels)
            : null;
    }

    public static string GetFieldLabel(this IDynamoCommonQuery q, string field)
    {
        if (!DynamoConfig.IsReservedWord(field))
            return field;

        var alias = "#" + field.Substring(0, 2).ToUpper();
        bool aliasExists = false;

        foreach (var entry in q.ExpressionAttributeNames)
        {
            if (entry.Value == field)
                return entry.Key;

            if (entry.Key == alias)
                aliasExists = true;
        }

        if (aliasExists)
            alias += q.ExpressionAttributeNames.Count;

        q.ExpressionAttributeNames[alias] = field;
        return alias;
    }

    public static void AddFilter(this IDynamoCommonQuery q, string filterExpression, Dictionary<string, object> args)
    {
        if (q.FilterExpression == null)
            q.FilterExpression = filterExpression;
        else
            q.FilterExpression += " AND " + filterExpression;

        q.AddArguments(args);
    }
}