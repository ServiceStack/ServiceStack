namespace ServiceStack.Templates
{
    public static class TemplateConstants
    {
        public const string MaxQuota = nameof(TemplateConfig.MaxQuota);
        public const string DefaultCulture = nameof(TemplateConfig.DefaultCulture);
        public const string DefaultDateFormat = nameof(TemplateConfig.DefaultDateFormat);
        public const string DefaultDateTimeFormat = nameof(TemplateConfig.DefaultDateTimeFormat);
        public const string DefaultTimeFormat = nameof(TemplateConfig.DefaultTimeFormat);
        public const string DefaultFileCacheExpiry = nameof(TemplateConfig.DefaultFileCacheExpiry);
        public const string DefaultUrlCacheExpiry = nameof(TemplateConfig.DefaultUrlCacheExpiry);
        public const string DefaultIndent = nameof(TemplateConfig.DefaultIndent);
        public const string DefaultNewLine = nameof(TemplateConfig.DefaultNewLine);
        public const string DefaultJsConfig = nameof(TemplateConfig.DefaultJsConfig);
        public const string DefaultStringComparison = nameof(TemplateConfig.DefaultStringComparison);
        public const string DefaultTableClassName = nameof(TemplateConfig.DefaultTableClassName);
        public const string DefaultErrorClassName = nameof(TemplateConfig.DefaultErrorClassName);

        public const string Return = "return";
        public const string ReturnArgs = "returnArgs";
        public const string Debug = "debug";
        public const string AssignError = "assignError";
        public const string HtmlEncode = "htmlencode";
        public const string Model = "model";
        public const string Page = "page";
        public const string Partial = "partial";
        public const string TempFilePath = "/dev/null";
        public const string Index = "index";
        public const string Comparer = "comparer";
        public const string Map = "map";
        public const string Request = "Request";
        public const string PathInfo = "PathInfo";
        public const string PathArgs = "PathArgs";
        public const string AssetsBase = "assetsBase";
        public const string Format = "format";

        public static IRawString EmptyRawString { get; } = new RawString("");
        public static IRawString TrueRawString { get; } = new RawString("true");
        public static IRawString FalseRawString { get; } = new RawString("false");
    }
}