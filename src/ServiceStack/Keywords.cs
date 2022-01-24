namespace ServiceStack
{
    public static class Keywords
    {
        public static string Callback = "callback";
        public static string Format = "format";
        public static string AuthSecret = "authsecret";
        public static string RequestInfo = "requestinfo";
        public static string Debug = "debug";
        public static string Version = "version";
        public static string VersionAbbr = "v";
        public static string Ignore = "ignore";
        public static string IgnorePlaceHolder = "_";
        public static string Bare = "bare";
        public static string JsConfig = "jsconfig";
        public static string SessionId = "ss-id";
        public static string PermanentSessionId = "ss-pid";
        public static string SessionOptionsKey = "ss-opt";
        public static string TokenCookie = "ss-tok";
        public static string RefreshTokenCookie = "ss-reftok";
        public static string XCookies = "X-Cookies";
        public static string ApiKeyParam = "apikey";
        public static string Continue = "continue";
        public static string Redirect = "redirect";
        public static string NoRedirect = "noredirect";
        public static string ReturnUrl = nameof(ReturnUrl); //.NET Core default convention
        
        public const string AutoBatchIndex = nameof(AutoBatchIndex);
        public const string SoapMessage = nameof(SoapMessage);
        public const string WithoutOptions = nameof(WithoutOptions);
        public const string SessionState = "session_state";
        public const string OAuthSuccess = "s";
        public const string OAuthFailed = "f";
        public const string Route = "__route";
        public const string InvokeVerb = "__verb";
        public const string DbInfo = "__dbinfo";
        public const string CacheInfo = "__cacheinfo";
        public const string ApiKey = "__apikey";
        public const string Session = "__session";
        public const string HasPreAuthenticated = "__haspreauth";
        public const string HasGlobalHeaders = "__global_headers";
        public const string HasLogged = "_logged";
        public const string DidAuthenticate = "__didauth";
        public const string IgnoreEvent = "__ignoreevent";
        public const string EventModelId = "__eventmodelid";
        public const string IRequest = "__irequest";
        public const string Attributes = "__attrs";
        public const string RequestDuration = "_requestDurationStopwatch";
        public const string Code = "code";
        public const string State = "state";
        public const string View = "View";
        public const string ErrorView = "ErrorView";
        public const string Template = "Template";
        public const string Error = "__error";
        public const string ErrorStatus = "__errorStatus";
        public const string Authorization = "__authorization";
        public const string Model = "Model";
        public const string HttpStatus = "httpstatus";
        public const string GrpcResponseStatus = "responsestatus-bin";
        public const string Dynamic = nameof(Dynamic);
        public const string Id = nameof(Id);
        public const string Result = nameof(Result);
        public const string RowVersion = nameof(RowVersion);
        public const string Reset = nameof(Reset);
        public const string reset = nameof(reset);
        public const string Count = nameof(Count);
        
        public const string Allows = "allows";
        public const string Embed = "embed";
        public const string AccessTokenAuth = "accessTokenAuth";
    }
}