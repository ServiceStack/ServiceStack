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
        public static string ApiKeyParam = "apikey";
        public static string Redirect = "redirect";
        public static string Continue = "continue";
        public static string ReturnUrl = nameof(ReturnUrl); //.NET Core default convention
        public static string AutoBatchIndex = nameof(AutoBatchIndex);
        public static string SoapMessage = nameof(SoapMessage);

        public const string Route = "__route";
        public const string InvokeVerb = "__verb";
        public const string DbInfo = "__dbinfo";
        public const string CacheInfo = "__cacheinfo";
        public const string ApiKey = "__apikey";
        public const string Session = "__session";
        public const string HasPreAuthenticated = "__haspreauth";
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
        public const string Count = nameof(Count);
    }

    public static class LocalizedStrings
    {
        public const string Login = "login";
        public const string Auth = "auth";
        public const string Authenticate = "authenticate";
        public const string Redirect = "redirect";
        public const string AssignRoles = "assignroles";
        public const string UnassignRoles = "unassignroles";
        public const string NotModified = "Not Modified";
    }

    public static class ErrorMessages
    {
        //Auth Errors
        public static string UnknownAuthProviderFmt = "No configuration was added for OAuth provider '{0}'";
        public static string NoExternalRedirects = "External Redirects are not permitted";

        public static string InvalidBasicAuthCredentials = "Invalid BasicAuth Credentials";
        public static string WindowsAuthFailed = "Windows Auth Failed";
        public static string NotAuthenticated = "Not Authenticated";
        public static string InvalidUsernameOrPassword = "Invalid Username or Password";
        public static string UsernameOrEmailRequired = "Username or Email is required";
        public static string UserAccountLocked = "This account has been locked";
        public static string IllegalUsername = "Username contains invalid characters";
        public static string ShouldNotRegisterAuthSession = "AuthSession's are rehydrated from ICacheClient and should not be registered in IOC's when not in HostContext.TestMode";
        public static string ApiKeyRequiresSecureConnection = "Sending ApiKey over insecure connection forbidden when RequireSecureConnection=true";
        public static string ApiKeyDoesNotExist = "ApiKey does not exist";
        public static string ApiKeyHasBeenCancelled = "ApiKey has been cancelled";
        public static string ApiKeyHasExpired = "ApiKey has expired";
        public static string UserForApiKeyDoesNotExist = "User for ApiKey does not exist";
        public static string JwtRequiresSecureConnection = "Sending JWT over insecure connection forbidden when RequireSecureConnection=true";
        public static string TokenInvalidated = "Token has been invalidated";
        public static string TokenExpired = "Token has expired";
        public static string TokenInvalid = "Token is invalid";
        public static string RefreshTokenInvalid = "RefreshToken is Invalid";

        public static string InvalidRole = "Invalid Role";
        public static string InvalidPermission = "Invalid Permission";

        public static string ClaimDoesNotExistFmt = "Claim '{0}' with '{1}' does not exist";


        //Register
        public static string UserNotExists = "User does not exist";
        public static string AuthRepositoryNotExists = "No IAuthRepository registered in IoC or failed to resolve.";
        public static string UsernameAlreadyExists = "Username already exists";
        public static string EmailAlreadyExists = "Email already exists";
        public static string RegisterUpdatesDisabled = "Updating existing User is not enabled. Sign out to register a new User.";
        public static string PasswordsShouldMatch = "Passwords should match!";

        //AuthRepo
        public static string UserAlreadyExistsTemplate1 = "User '{0}' already exists";
        public static string EmailAlreadyExistsTemplate1 = "Email '{0}' already exists";


        //StaticFileHandler
        public static string FileNotExistsFmt = "Static File '{0}' not found";

        //Server Events
        public static string SubscriptionNotExistsFmt = "Subscription '{0}' does not exist";
        public static string SubscriptionForbiddenFmt = "Access to Subscription '{0}' is forbidden";

        //Validation
        public static string RequestAlreadyProcessedFmt = "Request '{0}' has already been processed";

        //Hosts
        public static string OnlyAllowedInAspNetHosts = "Only ASP.NET Requests accessible via Singletons are supported";
        public static string HostDoesNotSupportSingletonRequest = "This AppHost does not support accessing the current Request via a Singleton";

        //Invalid State
        public static string ConstructorNotFoundForType = "Constructor not found for Type '{0}'";
        public static string ServiceNotFoundForType = "Service not found for Type '{0}'";
        public static string CacheFeatureMustBeEnabled = "HttpCacheFeature Plugin must be registered to use {0}";
        
        //Request
        public static string ContentTypeNotSupported = "ContentType not supported '{0}'";

        //Configuration
        public static string AppsettingNotFound = "Unable to find App Setting: {0}";
        public static string ConnectionStringNotFound = "Unable to find Connection String: {0}";
    }

    public static class HelpMessages
    {
        public static string NativeTypesDtoOptionsTip =
            "To override a DTO option, remove \"{0}\" prefix before updating";
    }

    public static class StrictModeCodes
    {
        public const string CyclicalUserSession = nameof(CyclicalUserSession);
        public const string ReturnsValueType = nameof(ReturnsValueType);
    }
}
