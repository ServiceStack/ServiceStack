using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Text;

namespace ServiceStack.Script;

public class JsVariableDeclaration : JsExpression
{
    public JsVariableDeclarationKind Kind { get; }
    public JsDeclaration[] Declarations { get; }

    public JsVariableDeclaration(JsVariableDeclarationKind kind, params JsDeclaration[] declarations)
    {
        Kind = kind;
        Declarations = declarations;
    }

    protected bool Equals(JsVariableDeclaration other) => 
        Kind == other.Kind && Declarations.SequenceEqual(other.Declarations);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((JsVariableDeclaration) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int) Kind * 397) ^ (Declarations != null ? Declarations.GetHashCode() : 0);
        }
    }

    public override string ToRawString()
    {
        var sb = StringBuilderCache.Allocate();
        sb.Append(Kind.ToString().ToLower()).Append(" ");

        for (var i = 0; i < Declarations.Length; i++)
        {
            var dec = Declarations[i];
            if (i > 0)
                sb.Append(", ");
            sb.Append(dec.ToRawString());
        }
        return StringBuilderCache.ReturnAndFree(sb);
    }

    public override object Evaluate(ScriptScopeContext scope)
    {
        foreach (var declaration in Declarations)
        {
            scope.ScopedParams[declaration.Id.Name] = declaration.Init?.Evaluate(scope);
        }
        return JsNull.Value;
    }

    public override Dictionary<string, object> ToJsAst()
    {
        var to = new Dictionary<string, object>
        {
            ["type"] = ToJsAstType(),
            ["declarations"] = Declarations.Map(x => (object)x.ToJsAst()),
            ["kind"] = Kind.ToString().ToLower(),
        };
        return to;
    }
}
    

public enum JsVariableDeclarationKind
{
    Var,
    Let,
    Const,
}

public class JsDeclaration : JsExpression
{
    public JsIdentifier Id { get; set; }
        
    public JsToken Init { get; set; }

    public JsDeclaration(JsIdentifier id, JsToken init)
    {
        Id = id;
        Init = init;
    }

    protected bool Equals(JsDeclaration other) => Equals(Id, other.Id) && Equals(Init, other.Init);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((JsDeclaration) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Id != null ? Id.GetHashCode() : 0) * 397) ^ (Init != null ? Init.GetHashCode() : 0);
        }
    }

    public override string ToRawString() => Init != null
        ? $"{Id.Name} = {Init.ToRawString()}"
        : Id.Name;

    public override object Evaluate(ScriptScopeContext scope)
    {
        throw new System.NotImplementedException();
    }

    public override Dictionary<string, object> ToJsAst()
    {
        var to = new Dictionary<string, object>
        {
            ["type"] = ToJsAstType(),
            ["id"] = Id.Name,
            ["init"] = Init?.ToJsAst(),
        };
        return to;
    }
}