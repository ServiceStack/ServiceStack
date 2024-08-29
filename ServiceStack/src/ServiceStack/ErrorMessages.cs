namespace ServiceStack;

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
    public static string ApiKeyRequiresSecureConnection = "Sending API Key over insecure connection forbidden when RequireSecureConnection=true";
    public static string ApiKeyDoesNotExist = "API Key does not exist";
    public static string ApiKeyHasBeenCancelled = "API Key has been cancelled";
    public static string ApiKeyHasExpired = "API Key has expired";
    public static string ApiKeyInvalid = "Invalid API Key";
    public static string UserForApiKeyDoesNotExist = "User for API Key does not exist";
    public static string JwtRequiresSecureConnection = "Sending JWT over insecure connection forbidden when RequireSecureConnection=true";
    public static string TokenInvalidated = "Token has been invalidated";
    public static string TokenExpired = "Token has expired";
    public static string TokenInvalidNotBefore = "Token not valid yet";
    public static string TokenInvalid = "Token is invalid";
    public static string TokenInvalidAudienceFmt = "Invalid Audience: {0}";
    public static string RefreshTokenInvalid = "RefreshToken is Invalid";
    public static string PrimaryKeyRequired = "Primary Key is Required";
    public static string InvalidAccessToken = "AccessToken is Invalid";
    public static string SessionIdEmpty = "Session not set. Is Session being set in RequestFilters?";
    public static string Requires2FA = "Session not set. Is Session being set in RequestFilters?";

    public static string AccessDenied = "Access Denied";
    public static string InvalidRole = "Invalid Role";
    public static string InvalidPermission = "Invalid Permission";
    public static string WebSudoRequired = "Web Sudo Required";

    public static string ClaimDoesNotExistFmt = "Claim '{0}' with '{1}' does not exist";


    //Register
    public static string UserNotExists = "User does not exist";
    public static string AlreadyRegistered = "You're already registered";
    public static string AuthRepositoryNotExists = "No IAuthRepository registered in IoC or failed to resolve.";
    public static string UsernameAlreadyExists = "Username already exists";
    public static string EmailAlreadyExists = "Email already exists";
    public static string RegisterUpdatesDisabled = "Updating existing User is not enabled. Sign out to register a new User.";
    public static string PasswordsShouldMatch = "Passwords should match!";

    //AuthRepo
    public static string UserAlreadyExistsFmt = "User '{0}' already exists";
    public static string EmailAlreadyExistsFmt = "Email '{0}' already exists";


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
    public static string CacheFeatureMustBeEnabled = "HttpCacheFeature Plugin must be registered to use {0}";
        
    //Request
    public static string ContentTypeNotSupportedFmt = "ContentType not supported '{0}'";

    //Configuration
    public static string AppSettingNotFoundFmt = "Unable to find App Setting: {0}";
    public static string ConnectionStringNotFoundFmt = "Unable to find Connection String: {0}";
}

public static class HelpMessages
{
    public static string NativeTypesDtoOptionsTip =
        "To override a DTO option, remove \"{0}\" prefix before updating";

    public static string DefaultRedirectMessage = "Moved Temporarily";
}

public static class StrictModeCodes
{
    public const string CyclicalUserSession = nameof(CyclicalUserSession);
    public const string ReturnsValueType = nameof(ReturnsValueType);
}