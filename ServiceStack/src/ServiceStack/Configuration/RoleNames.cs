namespace ServiceStack.Configuration;

public static class RoleNames
{
    public const string Admin = nameof(Admin);
        
    public const string AllowAnyUser = nameof(AllowAnyUser); // Valid for any Authenticated User, No roles required 

    public const string AllowAnon = nameof(AllowAnon);       // Allow access to all 
}