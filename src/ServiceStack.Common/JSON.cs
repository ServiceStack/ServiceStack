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
            json.ToStringSegment().ParseNextToken(out object value, out JsBinding binding);
            return value;
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
            JsonTypeSerializer.Instance.ObjectDeserializer = segment =>
            {
                segment.ParseNextToken(out object value, out _);
                return value;
            };
        }

        public static void UnConfigure() => JsonTypeSerializer.Instance.ObjectDeserializer = null;

        public static TemplateScopeContext CreateScope(Dictionary<string, object> args = null, TemplateFilter functions = null)
        {
            var context = new TemplateContext();
            if (functions != null)
                context.TemplateFilters.Add(functions);

            context.Init();
            return new TemplateScopeContext(new PageResult(context.OneTimePage("")), null, args);
        }

        public static object eval(string js) => eval(js, CreateScope());
        public static object eval(string js, TemplateScopeContext scope)
        {
            js.ToStringSegment().ParseNextToken(out object value, out JsBinding binding);
            var result = scope.Evaluate(value, binding);
            return result;
        }

        public static object value(string js)
        {
            js.ToStringSegment().ParseNextToken(out object value, out JsBinding binding);
            return value;
        }
    }
}