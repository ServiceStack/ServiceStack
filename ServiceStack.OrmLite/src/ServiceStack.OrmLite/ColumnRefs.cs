using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace ServiceStack.OrmLite;

/// <summary>
/// Quoted column names that can be deconstructed into variables, e.g:
/// <para>var (Id, Name) = db.ColumnRefs&lt;Person&gt;(x =&gt; new { x.Id, x.Name });</para>
/// Each column is a <see cref="PartialSqlString"/> which is embedded verbatim in <see cref="Sql.Fmt(FormattableString)"/>.
/// </summary>
public sealed class ColumnRefs(PartialSqlString[] columns) : IReadOnlyList<PartialSqlString>
{
    public int Count => columns.Length;
    public PartialSqlString this[int index] => columns[index];
    public IEnumerator<PartialSqlString> GetEnumerator() => ((IEnumerable<PartialSqlString>)columns).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void AssertCount(int count)
    {
        if (columns.Length != count)
            throw new ArgumentException($"Can't deconstruct {columns.Length} columns into {count} variables");
    }

    public void Deconstruct(out PartialSqlString c1, out PartialSqlString c2)
    {
        AssertCount(2);
        (c1, c2) = (columns[0], columns[1]);
    }

    public void Deconstruct(out PartialSqlString c1, out PartialSqlString c2, out PartialSqlString c3)
    {
        AssertCount(3);
        (c1, c2, c3) = (columns[0], columns[1], columns[2]);
    }

    public void Deconstruct(out PartialSqlString c1, out PartialSqlString c2, out PartialSqlString c3,
        out PartialSqlString c4)
    {
        AssertCount(4);
        (c1, c2, c3, c4) = (columns[0], columns[1], columns[2], columns[3]);
    }

    public void Deconstruct(out PartialSqlString c1, out PartialSqlString c2, out PartialSqlString c3,
        out PartialSqlString c4, out PartialSqlString c5)
    {
        AssertCount(5);
        (c1, c2, c3, c4, c5) = (columns[0], columns[1], columns[2], columns[3], columns[4]);
    }

    public void Deconstruct(out PartialSqlString c1, out PartialSqlString c2, out PartialSqlString c3,
        out PartialSqlString c4, out PartialSqlString c5, out PartialSqlString c6)
    {
        AssertCount(6);
        (c1, c2, c3, c4, c5, c6) = (columns[0], columns[1], columns[2], columns[3], columns[4], columns[5]);
    }

    public void Deconstruct(out PartialSqlString c1, out PartialSqlString c2, out PartialSqlString c3,
        out PartialSqlString c4, out PartialSqlString c5, out PartialSqlString c6, out PartialSqlString c7)
    {
        AssertCount(7);
        (c1, c2, c3, c4, c5, c6, c7) = (columns[0], columns[1], columns[2], columns[3], columns[4], columns[5], columns[6]);
    }

    public void Deconstruct(out PartialSqlString c1, out PartialSqlString c2, out PartialSqlString c3,
        out PartialSqlString c4, out PartialSqlString c5, out PartialSqlString c6, out PartialSqlString c7,
        out PartialSqlString c8)
    {
        AssertCount(8);
        (c1, c2, c3, c4, c5, c6, c7, c8) =
            (columns[0], columns[1], columns[2], columns[3], columns[4], columns[5], columns[6], columns[7]);
    }

    public override string ToString() => string.Join(", ", (IEnumerable<PartialSqlString>)columns);
}

public static class OrmLiteColumnRefApi
{
    /// <summary>
    /// The dialect-quoted name of a column, embedded verbatim in Sql.Fmt(), e.g:
    /// <para>var Name = db.ColumnRef&lt;Person&gt;(x =&gt; x.Name);</para>
    /// <para>db.SqlList&lt;Person&gt;(Sql.Fmt($"SELECT * FROM {typeof(Person)} WHERE {Name} = {name}"))</para>
    /// </summary>
    /// <param name="prefixTable">Prefix the column with its quoted table name, e.g. for queries with joins</param>
    public static PartialSqlString ColumnRef<T>(this IDbConnection db, Expression<Func<T, object>> column, bool prefixTable = false) =>
        db.GetDialectProvider().ColumnRef(column, prefixTable);

    /// <summary>
    /// The dialect-quoted name of a column, embedded verbatim in Sql.Fmt()
    /// </summary>
    public static PartialSqlString ColumnRef<T>(this IOrmLiteDialectProvider dialect, Expression<Func<T, object>> column, bool prefixTable = false)
    {
        if (column == null)
            throw new ArgumentNullException(nameof(column));
        var body = StripConvert(column.Body);
        if (body is not MemberExpression member)
            throw new ArgumentException($"Expected a column like x => x.Name but was '{column}'", nameof(column));
        return QuoteColumn<T>(dialect, member.Member.Name, prefixTable);
    }

    /// <summary>
    /// The dialect-quoted names of multiple columns that can be deconstructed into variables, e.g:
    /// <para>var (Id, Name) = db.ColumnRefs&lt;Person&gt;(x =&gt; new { x.Id, x.Name });</para>
    /// </summary>
    /// <param name="prefixTable">Prefix the columns with their quoted table name, e.g. for queries with joins</param>
    public static ColumnRefs ColumnRefs<T>(this IDbConnection db, Expression<Func<T, object>> columns, bool prefixTable = false) =>
        db.GetDialectProvider().ColumnRefs(columns, prefixTable);

    /// <summary>
    /// The dialect-quoted names of multiple columns that can be deconstructed into variables
    /// </summary>
    public static ColumnRefs ColumnRefs<T>(this IOrmLiteDialectProvider dialect, Expression<Func<T, object>> columns, bool prefixTable = false)
    {
        if (columns == null)
            throw new ArgumentNullException(nameof(columns));

        var body = StripConvert(columns.Body);
        var members = body switch {
            NewExpression newExpr => newExpr.Arguments,
            MemberExpression => (IReadOnlyList<Expression>)[body],
            _ => throw new ArgumentException($"Expected columns like x => new {{ x.Id, x.Name }} but was '{columns}'", nameof(columns)),
        };

        var refs = new PartialSqlString[members.Count];
        for (var i = 0; i < refs.Length; i++)
        {
            if (StripConvert(members[i]) is not MemberExpression member || member.Expression is not ParameterExpression)
                throw new ArgumentException($"Expected only columns like x => new {{ x.Id, x.Name }} but was '{columns}'", nameof(columns));
            refs[i] = QuoteColumn<T>(dialect, member.Member.Name, prefixTable);
        }
        return new ColumnRefs(refs);
    }

    private static Expression StripConvert(Expression expr)
    {
        while (expr is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            expr = unary.Operand;
        return expr;
    }

    /// <summary>
    /// The same quoted column name as q.Column&lt;T&gt;(x =&gt; x.Name), wrapped to be embedded verbatim like Sql.Raw()
    /// </summary>
    private static PartialSqlString QuoteColumn<T>(IOrmLiteDialectProvider dialect, string propertyName, bool prefixTable)
    {
        if (typeof(T).GetModelDefinition().GetFieldDefinition(propertyName) == null)
            throw new ArgumentException($"'{propertyName}' isn't a column of {typeof(T).Name}");
        return new PartialSqlString(dialect.Column<T>(propertyName, prefixTable));
    }
}
