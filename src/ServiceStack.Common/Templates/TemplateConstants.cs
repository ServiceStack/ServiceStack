namespace ServiceStack.Templates
{
    public static class TemplateConstants
    {
        public const string DefaultDateFormat = nameof(DefaultDateFormat);
        public const string DefaultDateTimeFormat = nameof(DefaultDateTimeFormat);
        public const string DefaultTimeFormat = nameof(DefaultTimeFormat);
        public const string DefaultCulture = nameof(DefaultCulture);
        public const string DefaultIndent = nameof(DefaultIndent);
        public const string DefaultNewLine = nameof(DefaultNewLine);
        public const string DefaultJsConfig = nameof(DefaultJsConfig);
        public const string DefaultStringComparison = nameof(DefaultStringComparison);
        public const string MaxQuota = nameof(MaxQuota);
        public const string HtmlEncode = "htmlencode";
        public const string Model = "model";
        public const string Page = "page";
        public const string Partial = "partial";
        public const string TempFilePath = "/dev/null";
        public const string Index = "index";
        public const string Comparer = "comparer";
        public const string Map = "map";
        public const string Request = "Request";
        public const string DefaultCacheExpiry = nameof(DefaultCacheExpiry);

        public static IRawString EmptyRawString { get; } = new RawString("");
        public static IRawString TrueRawString { get; } = new RawString("true");
        public static IRawString FalseRawString { get; } = new RawString("false");
    }
}