using System;
using System.Collections.Generic;

namespace ServiceStack.Script;

public abstract class JsStatement
{
}

public class JsFilterExpressionStatement : JsStatement
{
    public PageVariableFragment FilterExpression { get; }
    public JsFilterExpressionStatement(ReadOnlyMemory<char> originalText, JsToken expr, List<JsCallExpression> filters)
    {
        FilterExpression = new PageVariableFragment(originalText, expr, filters);
    }
    public JsFilterExpressionStatement(string originalText, JsToken expr, params JsCallExpression[] filters)
    {
        FilterExpression = new PageVariableFragment(originalText.AsMemory(), expr, new List<JsCallExpression>(filters));
    }

    protected bool Equals(JsFilterExpressionStatement other) => Equals(FilterExpression, other.FilterExpression);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((JsFilterExpressionStatement) obj);
    }

    public override int GetHashCode() => (FilterExpression != null ? FilterExpression.GetHashCode() : 0);
}

public class JsBlockStatement : JsStatement
{
    public JsStatement[] Statements { get; }
    public JsBlockStatement(JsStatement[] statements) => Statements = statements;
    public JsBlockStatement(JsStatement statement) => Statements = new[]{ statement };

    protected bool Equals(JsBlockStatement other) => Statements.EquivalentTo(other.Statements);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((JsBlockStatement) obj);
    }

    public override int GetHashCode() => (Statements != null ? Statements.GetHashCode() : 0);
}

public class JsExpressionStatement : JsStatement
{
    public JsToken Expression { get; }
    public JsExpressionStatement(JsToken expression) => Expression = expression;

    protected bool Equals(JsExpressionStatement other) => Equals(Expression, other.Expression);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((JsExpressionStatement) obj);
    }

    public override int GetHashCode() => (Expression != null ? Expression.GetHashCode() : 0);
}

public class JsPageBlockFragmentStatement : JsStatement
{
    public PageBlockFragment Block { get; }
    public JsPageBlockFragmentStatement(PageBlockFragment block) => Block = block;

    protected bool Equals(JsPageBlockFragmentStatement other) => Equals(Block, other.Block);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((JsPageBlockFragmentStatement) obj);
    }

    public override int GetHashCode() => (Block != null ? Block.GetHashCode() : 0);
}