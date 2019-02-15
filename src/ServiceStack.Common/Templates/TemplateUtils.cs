namespace ServiceStack.Templates
{
    public static class TemplateUtils
    {
        public static bool IsNull(object test) => test == null || test == JsNull.Value;
    }
}