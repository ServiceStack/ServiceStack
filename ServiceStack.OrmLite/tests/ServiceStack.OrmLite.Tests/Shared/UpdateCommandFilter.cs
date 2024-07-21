using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Shared;

public static class UpdateCommandFilter
{
    public static void SetUpdateDate<T>(this IDbCommand cmd, string fieldName, IOrmLiteDialectProvider dialectProvider) where T : new()
    {
        var field = typeof(T).GetProperty(fieldName);
        var alias = field.GetCustomAttribute(typeof(AliasAttribute)) as AliasAttribute;
        var columnName = dialectProvider.GetQuotedColumnName(dialectProvider.NamingStrategy.GetColumnName(alias?.Name ?? field.Name));
        var columnEqual = columnName + "=";

        var defaultValue = dialectProvider.GetDefaultValue(typeof(T), fieldName);
        var regex = new Regex(columnEqual + "(" + dialectProvider.ParamString + @"\w*\b)(,|\s)");
        var match = regex.Match(cmd.CommandText);
        if (match.Success)
        {
            cmd.CommandText = regex.Replace(cmd.CommandText, columnEqual + defaultValue + "$2", 1);
            cmd.Parameters.RemoveAt(match.Groups[1].Value);
        }
        else
        {
            cmd.CommandText = Regex.Replace(cmd.CommandText, @"(^|\s)SET ", "$1SET " + columnEqual + defaultValue + ", ");
        }
    }
}