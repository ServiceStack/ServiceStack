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
        public static string SoapMessage = "SoapMessage";
        public static string Route = "__route";
        public static string InvokeVerb = "__verb";
        public static string DbInfo = "__dbinfo";
        public static string CacheInfo = "__cacheinfo";
        public static string ApiKey = "__apikey";
        public static string ApiKeyParam = "apikey";
        public static string Session = "__session";
        public static string JsConfig = "jsconfig";
        public static string SessionId = "ss-id";
        public static string PermanentSessionId = "ss-pid";
        public static string SessionOptionsKey = "ss-opt";
        public static string TokenCookie = "ss-tok";
        public static string HasPreAuthenticated = "__haspreauth";
        public static string HasLogged = "_logged";
        public static string DidAuthenticate = "__didauth";
        public static string IRequest = "__irequest";
        public static string RequestDuration = "_requestDurationStopwatch";
        public static string Code = "code";
        public static string View = "View";
        public static string Template = "Template";
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

        public static string InvalidBasicAuthCredentials = "Invalid BasicAuth Credentials";
        public static string WindowsAuthFailed = "Windows Auth Failed";
        public static string NotAuthenticated = "Not Authenticated";
        public static string InvalidUsernameOrPassword = "Invalid UserName or Password";
        public static string UsernameOrEmailRequired = "UserName or Email is required";
        public static string UserAccountLocked = "This account has been locked";
        public static string IllegalUsername = "UserName contains invalid characters";
        public static string ShouldNotRegisterAuthSession = "AuthSession's are rehydrated from ICacheClient and should not be registered in IOC's when not in HostContext.TestMode";
        public static string ApiKeyRequiresSecureConnection = "Sending ApiKey over insecure connection forbidden when RequireSecureConnection=true";
        public static string JwtRequiresSecureConnection = "Sending JWT over insecure connection forbidden when RequireSecureConnection=true";
        public static string TokenInvalidated = "Token has been invalidated";
        public static string TokenExpired = "Token has expired";
        public static string TokenInvalid = "Token is invalid";
        public static string RefreshTokenInvalid = "RefreshToken is Invalid";

        public static string InvalidRole = "Invalid Role";
        public static string InvalidPermission = "Invalid Permission";

        //Register
        public static string UserNotExists = "User does not exist";
        public static string AuthRepositoryNotExists = "No IAuthRepository registered or failed to resolve. Check your IoC registrations.";
        public static string UsernameAlreadyExists = "Username already exists";
        public static string EmailAlreadyExists = "Email already exists";
        public static string RegisterUpdatesDisabled = "Updating User Info is not enabled";

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
