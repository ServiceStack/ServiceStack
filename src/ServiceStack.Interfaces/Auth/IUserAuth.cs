using System;
using System.Collections.Generic;

namespace ServiceStack.Auth
{
    public interface IUserAuth : IUserAuthDetailsExtended, IMeta
    {
        int Id { get; set; }
        string PrimaryEmail { get; set; }
        string Salt { get; set; }
        string PasswordHash { get; set; }
        string DigestHa1Hash { get; set; }
        List<string> Roles { get; set; }
        List<string> Permissions { get; set; }
        //Custom reference data
        int? RefId { get; set; }
        string RefIdStr { get; set; }

        int InvalidLoginAttempts { get; set; }
        DateTime? LastLoginAttempt { get; set; }
        DateTime? LockedDate { get; set; }

        DateTime CreatedDate { get; set; }
        DateTime ModifiedDate { get; set; }
    }

    public interface IUserAuthDetails : IAuthTokens, IMeta
    {
        int Id { get; set; }
        int UserAuthId { get; set; }
        DateTime CreatedDate { get; set; }
        DateTime ModifiedDate { get; set; }
        int? RefId { get; set; }
        string RefIdStr { get; set; }
    }

    public interface IUserAuthDetailsExtended
    {
        string UserName { get; set; }
        string DisplayName { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string Company { get; set; }
        string Email { get; set; }
        string PhoneNumber { get; set; }
        DateTime? BirthDate { get; set; }
        string BirthDateRaw { get; set; }
        string Address { get; set; }
        string Address2 { get; set; }
        string City { get; set; }
        string State { get; set; }
        string Country { get; set; }
        string Culture { get; set; }
        string FullName { get; set; }
        string Gender { get; set; }
        string Language { get; set; }
        string MailAddress { get; set; }
        string Nickname { get; set; }
        string PostalCode { get; set; }
        string TimeZone { get; set; }
    }

    public interface IAuthTokens : IUserAuthDetailsExtended
    {
        string Provider { get; set; }
        string UserId { get; set; }
        string AccessToken { get; set; }
        string AccessTokenSecret { get; set; }
        string RefreshToken { get; set; }
        DateTime? RefreshTokenExpiry { get; set; }
        string RequestToken { get; set; }
        string RequestTokenSecret { get; set; }
        Dictionary<string, string> Items { get; set; }
    }
}