using System;
using System.Collections.Generic;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

/// <summary>
/// Raw SQL created from an interpolated string where every interpolated value is sent as a db param, e.g:
/// <para>db.SqlList&lt;Order&gt;(Sql.Fmt($"SELECT * FROM Orders WHERE CustomerId = {customerId} AND Id IN ({ids})"))</para>
/// Collections are expanded into a param per value, <see cref="Type"/>, <see cref="ModelDefinition"/> and
/// <see cref="TableRef"/> values are embedded as quoted table names and <see cref="PartialSqlString"/> values
/// (e.g. from Sql.Raw()) are embedded verbatim. Create with <see cref="Sql.Fmt(FormattableString)"/>.
/// </summary>
public sealed class SqlFormattable
{
    public string Format { get; }
    public object[] Args { get; }

    public SqlFormattable(FormattableString sql)
    {
        if (sql == null)
            throw new ArgumentNullException(nameof(sql));
        Format = sql.Format;
        Args = sql.GetArguments();
    }

    /// <summary>
    /// Returns the SQL using dialect named params (p0, p1, ...) populated in dbParams
    /// </summary>
    public string ToSql(IOrmLiteDialectProvider dialect, out Dictionary<string, object> dbParams)
    {
        var args = new Dictionary<string, object>();
        var sql = Build(dialect, arg => {
            var name = "p" + args.Count;
            // Convert values like enums into their db representation, collections are converted when expanded
            args[name] = arg == null || OrmLiteReadCommandExtensions.GetMultiValues(arg) != null
                ? arg
                : dialect.GetFieldValue(arg.GetType(), arg);
            return dialect.ParamString + name;
        });
        dbParams = args;
        return sql;
    }

    /// <summary>
    /// Returns the SQL, calling paramFn for each interpolated value to add it as a db param and return its placeholder.
    /// Table references are quoted with the dialect.
    /// </summary>
    public string Build(IOrmLiteDialectProvider dialect, Func<object, string> paramFn)
    {
        if (dialect == null)
            throw new ArgumentNullException(nameof(dialect));

        var format = Format;
        var sb = StringBuilderCache.Allocate();
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];
            if (c == '{')
            {
                if (i + 1 < format.Length && format[i + 1] == '{')
                {
                    sb.Append('{');
                    i++;
                    continue;
                }

                var end = format.IndexOf('}', i + 1);
                if (end < 0)
                    throw new FormatException("Unclosed placeholder in: " + format);

                var hole = format.Substring(i + 1, end - i - 1);
                if (!int.TryParse(hole, out var index) || index < 0 || index >= Args.Length)
                    throw new FormatException($"Sql.Fmt() does not support alignment or format specifiers in '{{{hole}}}', " +
                        "format the value before interpolating it");

                sb.Append(Args[index] switch {
                    PartialSqlString partialSql => partialSql.Text,
                    // Quoted table names, e.g. {typeof(Order)}, {ModelDefinition<T>.Definition} or {db.TableRef<Order>()}
                    Type type => QuoteTable(dialect, new TableRef(type)),
                    ModelDefinition modelDef => QuoteTable(dialect, new TableRef(modelDef)),
                    TableRef tableRef => QuoteTable(dialect, tableRef),
                    var arg => paramFn(arg),
                });
                i = end;
                continue;
            }
            if (c == '}' && i + 1 < format.Length && format[i + 1] == '}')
                i++;
            sb.Append(c);
        }
        return StringBuilderCache.ReturnAndFree(sb);
    }

    private static string QuoteTable(IOrmLiteDialectProvider dialect, TableRef tableRef) =>
        dialect.QuoteTable(tableRef)
        ?? throw new ArgumentException("TableRef in Sql.Fmt() doesn't reference a table");

    public override string ToString() => Format;
}

public static partial class Sql
{
    /// <summary>
    /// Create parameterized SQL from an interpolated string, e.g:
    /// <para>db.Select&lt;Person&gt;(Sql.Fmt($"Age &gt; {age} AND LastName IN ({names})"))</para>
    /// <para>q.Where(Sql.Fmt($"{Sql.Raw(q.Column&lt;Person&gt;(x =&gt; x.Age))} &gt; {age}"))</para>
    /// Interpolated values are sent as db params (collections are expanded into an IN list of params).
    /// Types, ModelDefinitions and TableRefs are embedded as quoted table names, e.g:
    /// <para>db.SqlList&lt;Order&gt;(Sql.Fmt($"SELECT * FROM {typeof(Order)} WHERE Id = {id}"))</para>
    /// Use <see cref="Raw(string)"/> to embed other trusted SQL, e.g. column names.
    /// </summary>
    public static SqlFormattable Fmt(FormattableString sql) => new(sql);

    /// <summary>
    /// Embed trusted SQL verbatim in a <see cref="Fmt(FormattableString)"/> query, e.g. a quoted column name.
    /// Never use with user input.
    /// </summary>
    public static PartialSqlString Raw(string sql) => new(sql);
}
