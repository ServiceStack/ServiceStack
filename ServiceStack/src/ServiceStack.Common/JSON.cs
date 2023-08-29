using System;
using System.Collections.Generic;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Text.Json;

namespace ServiceStack;

public static class JSON
{
    public static object parse(string json)
    {
        json.AsSpan().ParseJsToken(out var token);
        return token?.Evaluate(JS.CreateScope());
    }

    public static object parseSpan(ReadOnlySpan<char> json)
    {
        if (json.Length == 0) 
            return null;
        var firstChar = json[0];

        bool isEscapedJsonString(ReadOnlySpan<char> js) =>
            js.StartsWith(@"{\") || js.StartsWith(@"[{\");

        if (firstChar >= '0' && firstChar <= '9')
        {
            try {
                var longValue = MemoryProvider.Instance.ParseInt64(json);
                return longValue >= int.MinValue && longValue <= int.MaxValue
                    ? (int) longValue
                    : longValue;
            } catch {}

            if (json.TryParseDouble(out var doubleValue))
                return doubleValue;
        }
        else if (firstChar == '{' || firstChar == '[' 
                 && !isEscapedJsonString(json.TrimStart())) 
        {
            json.ParseJsToken(out var token);
            return token.Evaluate(JS.CreateScope());
        }
        else if (json.Length == 4)
        {
            if (firstChar == 't' && json[1] == 'r' && json[2] == 'u' && json[3] == 'e')
                return true;
            if (firstChar == 'n' && json[1] == 'u' && json[2] == 'l' && json[3] == 'l')
                return null;
        }
        else if (json.Length == 5 && firstChar == 'f' && json[1] == 'a' && json[2] == 'l' && json[3] == 's' && json[4] == 'e')
        {
            return false;
        }
                
        var unescapedString = JsonTypeSerializer.Unescape(json);
        return unescapedString.ToString();
    }

    public static string stringify(object value) => value.ToJson();
}

public static class JS
{
    public const string EvalCacheKeyPrefix = "scriptvalue:";
    public const string EvalScriptCacheKeyPrefix = "scriptvalue.script:";
    public const string EvalAstCacheKeyPrefix = "scriptvalue.ast:";
        
    /// <summary>
    /// Configure ServiceStack.Text JSON Serializer to use Templates JS parsing
    /// </summary>
    public static void Configure()
    {
        JsonTypeSerializer.Instance.ObjectDeserializer = JSON.parseSpan;
    }

    public static void UnConfigure() => JsonTypeSerializer.Instance.ObjectDeserializer = null;

    public static ScriptScopeContext CreateScope(Dictionary<string, object> args = null, ScriptMethods functions = null)
    {
        var context = new ScriptContext();
        if (functions != null)
            context.ScriptMethods.Insert(0, functions);

        context.Init();
        return new ScriptScopeContext(new PageResult(context.EmptyPage), null, args);
    }

    /// <summary>
    /// Parse JS Expression into an AST Token
    /// </summary>
    public static JsToken expression(string js)
    {
        js.ParseJsExpression(out var token);
        return token;
    }

    /// <summary>
    /// Returns cached AST of a single expression
    /// </summary>
    public static JsToken expressionCached(ScriptContext context, string expr)
    {
        var evalAstCacheKey = EvalAstCacheKeyPrefix + expr;
        var ret = (JsToken)context.Cache.GetOrAdd(evalAstCacheKey, key =>
            expression(expr));
        return ret;
    }

    /// <summary>
    /// Returns cached AST of a script
    /// </summary>
    public static SharpPage scriptCached(ScriptContext context, string evalCode)
    {
        var evalScriptCacheKey = EvalScriptCacheKeyPrefix + evalCode;
        var ret = (SharpPage)context.Cache.GetOrAdd(evalScriptCacheKey, key =>
            context.CodeSharpPage(evalCode));
        return ret;
    }

    public static object eval(string js) => eval(js, CreateScope());
    public static object eval(string js, ScriptScopeContext scope) => eval(js.AsSpan(), scope);
    public static object eval(ReadOnlySpan<char> js, ScriptScopeContext scope)
    {
        js.ParseJsExpression(out var token);
        return ScriptLanguage.UnwrapValue(token.Evaluate(scope));
    }

    public static object eval(ScriptContext context, string expr, Dictionary<string, object> args = null) =>
        eval(context, expr.AsSpan(), args);
    public static object eval(ScriptContext context, ReadOnlySpan<char> expr, Dictionary<string, object> args=null)
    {
        return eval(expr, new ScriptScopeContext(new PageResult(context.EmptyPage), null, args));
    }
    public static object eval(ScriptContext context, JsToken token, Dictionary<string, object> args=null)
    {
        return ScriptLanguage.UnwrapValue(token.Evaluate(new ScriptScopeContext(new PageResult(context.EmptyPage), null, args)));
    }
        
    /// <summary>
    /// Lightweight expression evaluator of a single JS Expression with results cached in global context cache
    /// </summary>
    public static object evalCached(ScriptContext context, string expr)
    {
        var evalValue = context.Cache.GetOrAdd(EvalCacheKeyPrefix + expr,
            key => eval(context, expr.AsSpan()));
        return evalValue;
    }

    public static Dictionary<string, object> ParseObject(string js)
    {
        return eval(js) as Dictionary<string,object>;
    }
}
