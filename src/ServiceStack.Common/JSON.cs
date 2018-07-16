using System;
using System.Collections.Generic;
using ServiceStack.Templates;
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

        public static string stringify(object value) => value.ToJson();
    }

    public static class JS
    {
        /// <summary>
        /// Configure ServiceStack.Text JSON Serializer to use Templates JS parsing
        /// </summary>
        public static void Configure()
        {
            JsonTypeSerializer.Instance.ObjectDeserializer = span =>
            {
                span.ParseJsExpression(out var token);
                return token.Evaluate(CreateScope());
            };
        }

        public static void UnConfigure() => JsonTypeSerializer.Instance.ObjectDeserializer = null;

        public static TemplateScopeContext CreateScope(Dictionary<string, object> args = null, TemplateFilter functions = null)
        {
            var context = new TemplateContext();
            if (functions != null)
                context.TemplateFilters.Insert(0, functions);

            context.Init();
            return new TemplateScopeContext(new PageResult(context.OneTimePage("")), null, args);
        }

        public static object eval(string js) => eval(js, CreateScope());
        public static object eval(string js, TemplateScopeContext scope)
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