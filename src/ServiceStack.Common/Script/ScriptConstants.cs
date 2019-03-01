namespace ServiceStack.Script
{
    public static class ScriptConstants
    {
        public const string MaxQuota = nameof(ScriptConfig.MaxQuota);
        public const string DefaultCulture = nameof(ScriptConfig.DefaultCulture);
        public const string DefaultDateFormat = nameof(ScriptConfig.DefaultDateFormat);
        public const string DefaultDateTimeFormat = nameof(ScriptConfig.DefaultDateTimeFormat);
        public const string DefaultTimeFormat = nameof(ScriptConfig.DefaultTimeFormat);
        public const string DefaultFileCacheExpiry = nameof(ScriptConfig.DefaultFileCacheExpiry);
        public const string DefaultUrlCacheExpiry = nameof(ScriptConfig.DefaultUrlCacheExpiry);
        public const string DefaultIndent = nameof(ScriptConfig.DefaultIndent);
        public const string DefaultNewLine = nameof(ScriptConfig.DefaultNewLine);
        public const string DefaultJsConfig = nameof(ScriptConfig.DefaultJsConfig);
        public const string DefaultStringComparison = nameof(ScriptConfig.DefaultStringComparison);
        public const string DefaultTableClassName = nameof(ScriptConfig.DefaultTableClassName);
        public const string DefaultErrorClassName = nameof(ScriptConfig.DefaultErrorClassName);

        public const string Debug = "debug";
        public const string AssignError = "assignError";
        public const string CatchError = "catchError"; //assigns error and continues
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