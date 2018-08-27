namespace ServiceStack.Configuration
{
    public static class RoleNames
    {
        public static string Admin = nameof(Admin);
        
        public static string AllowAnyUser = nameof(AllowAnyUser); // Valid for any Authenticated User, No roles required 

        public static string AllowAnon = nameof(AllowAnon);       // Allow access to all 
    }
}