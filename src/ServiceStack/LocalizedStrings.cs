namespace ServiceStack
{
    public static class LocalizedStrings
    {
        public const string Login = "login";
        public const string Auth = "auth";
        public const string Authenticate = "authenticate";
        public const string Redirect = "redirect";
        public const string AssignRoles = "assignroles";
        public const string UnassignRoles = "unassignroles";
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
        public static string IllegalUsername = "UserName contains invalid characters";

        public static string InvalidRole = "Invalid Role";
        public static string InvalidPermission = "Invalid Permission";

        //Register
        public static string UserNotExists = "User does not exist";
        public static string AuthRepositoryNotExists = "No IAuthRepository registered or failed to resolve. Check your IoC registrations.";

        //StaticFileHandler
        public static string FileNotExistsFmt = "Static File '{0}' not found";

        //Server Events
        public static string SubscriptionNotExistsFmt = "Subscription '{0}' does not exist";

        //Validation
        public static string RequestAlreadyProcessedFmt = "Request '{0}' has already been processed";
        
        //Hosts
        public static string OnlyAllowedInAspNetHosts = "Only ASP.NET Requests accessible via Singletons are supported";
        public static string HostDoesNotSupportSingletonRequest = "This AppHost does not support accessing the current Request via a Singleton";
    }
}
