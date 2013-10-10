using System;
using System.Collections.Generic;
using ServiceStack.Common;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface.Auth
{
    public class UserAuth
    {
        public UserAuth()
        {
            this.Roles = new List<string>();
            this.Permissions = new List<string>();
        }

        [AutoIncrement]
        public virtual int Id { get; set; }
        public virtual string UserName { get; set; }
        public virtual string Email { get; set; }
        public virtual string PrimaryEmail { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual string DisplayName { get; set; }
        public virtual DateTime? BirthDate { get; set; }
        public virtual string BirthDateRaw { get; set; }
        public virtual string Country { get; set; }
        public virtual string Culture { get; set; }
        public virtual string FullName { get; set; }
        public virtual string Gender { get; set; }
        public virtual string Language { get; set; }
        public virtual string MailAddress { get; set; }
        public virtual string Nickname { get; set; }
        public virtual string PostalCode { get; set; }
        public virtual string TimeZone { get; set; }
        public virtual string Salt { get; set; }
        public virtual string PasswordHash { get; set; }
        public virtual string DigestHa1Hash { get; set; }
        public virtual List<string> Roles { get; set; }
        public virtual List<string> Permissions { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual DateTime ModifiedDate { get; set; }

        //Custom Reference Data
        public virtual int? RefId { get; set; }
        public virtual string RefIdStr { get; set; }
        public virtual Dictionary<string, string> Meta { get; set; }

        public virtual T Get<T>()
        {
            string str = null;
            if (Meta != null) Meta.TryGetValue(typeof(T).Name, out str);
            return str == null ? default(T) : TypeSerializer.DeserializeFromString<T>(str);
        }

        public virtual void Set<T>(T value)
        {
            if (Meta == null) Meta = new Dictionary<string, string>();
            Meta[typeof(T).Name] = TypeSerializer.SerializeToString(value);
        }

        public virtual void PopulateMissing(UserOAuthProvider authProvider)
        {
            //Don't explicitly override after if values exist
            if (!authProvider.DisplayName.IsNullOrEmpty() && this.DisplayName.IsNullOrEmpty())
                this.DisplayName = authProvider.DisplayName;
            if (!authProvider.Email.IsNullOrEmpty() && this.PrimaryEmail.IsNullOrEmpty())
                this.PrimaryEmail = authProvider.Email;

            if (!authProvider.FirstName.IsNullOrEmpty())
                this.FirstName = authProvider.FirstName;
            if (!authProvider.LastName.IsNullOrEmpty())
                this.LastName = authProvider.LastName;
            if (!authProvider.FullName.IsNullOrEmpty())
                this.FullName = authProvider.FullName;
            if (authProvider.BirthDate != null)
                this.BirthDate = authProvider.BirthDate;
            if (!authProvider.BirthDateRaw.IsNullOrEmpty())
                this.BirthDateRaw = authProvider.BirthDateRaw;
            if (!authProvider.Country.IsNullOrEmpty())
                this.Country = authProvider.Country;
            if (!authProvider.Culture.IsNullOrEmpty())
                this.Culture = authProvider.Culture;
            if (!authProvider.Gender.IsNullOrEmpty())
                this.Gender = authProvider.Gender;
            if (!authProvider.MailAddress.IsNullOrEmpty())
                this.MailAddress = authProvider.MailAddress;
            if (!authProvider.Nickname.IsNullOrEmpty())
                this.Nickname = authProvider.Nickname;
            if (!authProvider.PostalCode.IsNullOrEmpty())
                this.PostalCode = authProvider.PostalCode;
            if (!authProvider.TimeZone.IsNullOrEmpty())
                this.TimeZone = authProvider.TimeZone;
        }
    }

    public class UserOAuthProvider : IOAuthTokens
    {
        public UserOAuthProvider()
        {
            this.Items = new Dictionary<string, string>();
        }

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
        public virtual string Email { get; set; }

        public virtual DateTime? BirthDate { get; set; }
        public virtual string BirthDateRaw { get; set; }
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
        public virtual Dictionary<string, string> Items { get; set; }
        public virtual string AccessToken { get; set; }
        public virtual string AccessTokenSecret { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual DateTime ModifiedDate { get; set; }

        //Custom Reference Data
        public virtual int? RefId { get; set; }
        public virtual string RefIdStr { get; set; }
        public virtual Dictionary<string, string> Meta { get; set; }

        public virtual T Get<T>()
        {
            string str = null;
            if (Meta != null) Meta.TryGetValue(typeof(T).Name, out str);
            return str == null ? default(T) : TypeSerializer.DeserializeFromString<T>(str);
        }

        public virtual void Set<T>(T value)
        {
            if (Meta == null) Meta = new Dictionary<string, string>();
            Meta[typeof(T).Name] = TypeSerializer.SerializeToString(value);
        }

        public virtual void PopulateMissing(IOAuthTokens withTokens)
        {
            if (!withTokens.UserId.IsNullOrEmpty())
                this.UserId = withTokens.UserId;
            if (!withTokens.UserName.IsNullOrEmpty())
                this.UserName = withTokens.UserName;
            if (!withTokens.RefreshToken.IsNullOrEmpty())
                this.RefreshToken = withTokens.RefreshToken;
            if (withTokens.RefreshTokenExpiry.HasValue)
                this.RefreshTokenExpiry = withTokens.RefreshTokenExpiry;
            if (!withTokens.RequestToken.IsNullOrEmpty())
                this.RequestToken = withTokens.RequestToken;
            if (!withTokens.RequestTokenSecret.IsNullOrEmpty())
                this.RequestTokenSecret = withTokens.RequestTokenSecret;
            if (!withTokens.AccessToken.IsNullOrEmpty())
                this.AccessToken = withTokens.AccessToken;
            if (!withTokens.AccessTokenSecret.IsNullOrEmpty())
                this.AccessTokenSecret = withTokens.AccessTokenSecret;
            if (!withTokens.DisplayName.IsNullOrEmpty())
                this.DisplayName = withTokens.DisplayName;
            if (!withTokens.FirstName.IsNullOrEmpty())
                this.FirstName = withTokens.FirstName;
            if (!withTokens.LastName.IsNullOrEmpty())
                this.LastName = withTokens.LastName;
            if (!withTokens.Email.IsNullOrEmpty())
                this.Email = withTokens.Email;
            if (!withTokens.FullName.IsNullOrEmpty())
                this.FullName = withTokens.FullName;
            if (withTokens.BirthDate != null)
                this.BirthDate = withTokens.BirthDate;
            if (!withTokens.BirthDateRaw.IsNullOrEmpty())
                this.BirthDateRaw = withTokens.BirthDateRaw;
            if (!withTokens.Country.IsNullOrEmpty())
                this.Country = withTokens.Country;
            if (!withTokens.Culture.IsNullOrEmpty())
                this.Culture = withTokens.Culture;
            if (!withTokens.Gender.IsNullOrEmpty())
                this.Gender = withTokens.Gender;
            if (!withTokens.MailAddress.IsNullOrEmpty())
                this.MailAddress = withTokens.MailAddress;
            if (!withTokens.Nickname.IsNullOrEmpty())
                this.Nickname = withTokens.Nickname;
            if (!withTokens.PostalCode.IsNullOrEmpty())
                this.PostalCode = withTokens.PostalCode;
            if (!withTokens.TimeZone.IsNullOrEmpty())
                this.TimeZone = withTokens.TimeZone;
        }
    }

}
