using System;
using System.Collections.Generic;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Text.Json;

namespace ServiceStack
{
    public static class JSON
    {
        public static object parse(string json)
        {
            json.AsSpan().ParseJsToken(out var token);
            return token.Evaluate(JS.CreateScope());
        }

        public static object parseSpan(ReadOnlySpan<char> json)
        {
            if (json.Length == 0) 
                return null;
            var firstChar = json[0];

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
            else if (firstChar == '{' || firstChar == '[')
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
            return new ScriptScopeContext(new PageResult(context.OneTimePage("")), null, args);
        }

        public static object eval(string js) => eval(js, CreateScope());
        public static object eval(string js, ScriptScopeContext scope)
        {
            js.ParseJsExpression(out var token);
            var result = token.Evaluate(scope);

            return result;
        }

        public static JsToken expression(string js)
        {
            js.ParseJsExpression(out var token);
            return token;
        }
        
    }
}