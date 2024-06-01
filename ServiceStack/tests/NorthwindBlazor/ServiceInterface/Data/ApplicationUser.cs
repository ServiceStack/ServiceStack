using System;
using Microsoft.AspNetCore.Identity;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
[Alias("AspNetUsers")]
public class ApplicationUser : IdentityUser, IRequireRefreshToken
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? ProfileUrl { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
}

