using System;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;

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

    public class UserAuthBase : IUserAuth
    {
        [AutoIncrement]
        public virtual int Id { get; set; }

        [Index]
        public virtual string UserName { get; set; }
        [Index]
        public virtual string Email { get; set; }

        public virtual string DisplayName { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual string Company { get; set; }
        public virtual string PhoneNumber { get; set; }
        public virtual DateTime? BirthDate { get; set; }
        public virtual string BirthDateRaw { get; set; }
        public virtual string Address { get; set; }
        public virtual string Address2 { get; set; }
        public virtual string City { get; set; }
        public virtual string State { get; set; }
        public virtual string Country { get; set; }
        public virtual string Culture { get; set; }
        public virtual string FullName { get; set; }
        public virtual string Gender { get; set; }
        public virtual string Language { get; set; }
        public virtual string MailAddress { get; set; }
        public virtual string Nickname { get; set; }
        public virtual string PostalCode { get; set; }
        public virtual string TimeZone { get; set; }
        
        public virtual string PrimaryEmail { get; set; }
        public virtual string Salt { get; set; }
        public virtual string PasswordHash { get; set; }
        public virtual string DigestHa1Hash { get; set; }
        public virtual List<string> Roles { get; set; } = new();
        public virtual List<string> Permissions { get; set; } = new();
        //Custom reference data
        public virtual int? RefId { get; set; }
        public virtual string RefIdStr { get; set; }

        public virtual int InvalidLoginAttempts { get; set; }
        public virtual DateTime? LastLoginAttempt { get; set; }
        public virtual DateTime? LockedDate { get; set; }

        public virtual DateTime CreatedDate { get; set; }
        public virtual DateTime ModifiedDate { get; set; }
        public virtual Dictionary<string, string> Meta { get; set; }
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
    
    public class UserAuthDetailsBase : IUserAuthDetails
    {
        [AutoIncrement]
        public virtual int Id { get; set; }

        public virtual int UserAuthId { get; set; }
        public virtual string Provider { get; set; }
        public virtual string UserId { get; set; }
        public virtual string UserName { get; set; }
        public virtual string FullName { get; set; }
        public virtual string DisplayName { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual string Company { get; set; }
        public virtual string Email { get; set; }
        public virtual string PhoneNumber { get; set; }

        public virtual DateTime? BirthDate { get; set; }
        public virtual string BirthDateRaw { get; set; }
        public virtual string Address { get; set; }
        public virtual string Address2 { get; set; }
        public virtual string City { get; set; }
        public virtual string State { get; set; }
        public virtual string Country { get; set; }
        public virtual string Culture { get; set; }
        public virtual string Gender { get; set; }
        public virtual string Language { get; set; }
        public virtual string MailAddress { get; set; }
        public virtual string Nickname { get; set; }
        public virtual string PostalCode { get; set; }
        public virtual string TimeZone { get; set; }

        public virtual string RefreshToken { get; set; }
        public virtual DateTime? RefreshTokenExpiry { get; set; }
        public virtual string RequestToken { get; set; }
        public virtual string RequestTokenSecret { get; set; }
        public virtual Dictionary<string, string> Items { get; set; } = new();
        public virtual string AccessToken { get; set; }
        public virtual string AccessTokenSecret { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual DateTime ModifiedDate { get; set; }

        //Custom Reference Data
        public virtual int? RefId { get; set; }
        public virtual string RefIdStr { get; set; }
        public virtual Dictionary<string, string> Meta { get; set; }
    }
    
}