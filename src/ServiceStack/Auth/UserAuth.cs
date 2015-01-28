using System;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public class UserAuth : IUserAuth
    {
        public UserAuth()
        {
            Roles = new List<string>();
            Permissions = new List<string>();
        }

        [AutoIncrement]
        public virtual int Id { get; set; }

        public virtual string UserName { get; set; }
        public virtual string Email { get; set; }
        public virtual string PrimaryEmail { get; set; }
        public virtual string PhoneNumber { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual string DisplayName { get; set; }
        public virtual string Company { get; set; }
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
        public virtual string Salt { get; set; }
        public virtual string PasswordHash { get; set; }
        public virtual string DigestHa1Hash { get; set; }
        public virtual List<string> Roles { get; set; }
        public virtual List<string> Permissions { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual DateTime ModifiedDate { get; set; }
        public virtual int InvalidLoginAttempts { get; set; }
        public virtual DateTime? LastLoginAttempt { get; set; }
        public virtual DateTime? LockedDate { get; set; }
        public virtual string RecoveryToken { get; set; }

        //Custom Reference Data
        public virtual int? RefId { get; set; }
        public virtual string RefIdStr { get; set; }
        public virtual Dictionary<string, string> Meta { get; set; }
    }

    public class UserAuthDetails : IUserAuthDetails
    {
        public UserAuthDetails() { Items = new Dictionary<string, string>(); }

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
        public virtual Dictionary<string, string> Items { get; set; }
        public virtual string AccessToken { get; set; }
        public virtual string AccessTokenSecret { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual DateTime ModifiedDate { get; set; }

        //Custom Reference Data
        public virtual int? RefId { get; set; }
        public virtual string RefIdStr { get; set; }
        public virtual Dictionary<string, string> Meta { get; set; }
    }

    public class UserAuthRole
    {
        [AutoIncrement]
        public virtual int Id { get; set; }

        public virtual int UserAuthId { get; set; }

        public virtual string Role { get; set; }

        public virtual string Permission { get; set; }

        public virtual DateTime CreatedDate { get; set; }

        public virtual DateTime ModifiedDate { get; set; }

        //Custom Reference Data
        public virtual int? RefId { get; set; }
        public virtual string RefIdStr { get; set; }
        public virtual Dictionary<string, string> Meta { get; set; }
    }
    
    public static class UserAuthExtensions
    {
        public static void PopulateMissing(this IUserAuthDetails instance, IAuthTokens tokens, bool overwriteReserved = false)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");

            if (tokens == null)
                throw new ArgumentNullException("tokens");

            if (!tokens.UserId.IsNullOrEmpty())
                instance.UserId = tokens.UserId;

            if (!tokens.RefreshToken.IsNullOrEmpty())
                instance.RefreshToken = tokens.RefreshToken;

            if (tokens.RefreshTokenExpiry.HasValue)
                instance.RefreshTokenExpiry = tokens.RefreshTokenExpiry;

            if (!tokens.RequestToken.IsNullOrEmpty())
                instance.RequestToken = tokens.RequestToken;

            if (!tokens.RequestTokenSecret.IsNullOrEmpty())
                instance.RequestTokenSecret = tokens.RequestTokenSecret;

            if (!tokens.AccessToken.IsNullOrEmpty())
                instance.AccessToken = tokens.AccessToken;

            if (!tokens.AccessTokenSecret.IsNullOrEmpty())
                instance.AccessTokenSecret = tokens.AccessTokenSecret;

            if (tokens.Items != null)
            {
                var items = instance.Items ?? (instance.Items = new Dictionary<string, string>());
                tokens.Items.ForEach((x,y) => items[x] = y);
            }

            PopulateMissingExtended(instance, tokens, overwriteReserved);
        }

        public static void PopulateMissingExtended(this IUserAuthDetailsExtended instance, 
            IUserAuthDetailsExtended other, bool overwriteReserved = false)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");

            if (other == null)
                throw new ArgumentNullException("other");

            // Don't override the Master UserAuth table's reserved fields if they already exists
            if (!other.UserName.IsNullOrEmpty() && (overwriteReserved || instance.UserName.IsNullOrEmpty()))
                instance.UserName = other.UserName;

            if (!other.DisplayName.IsNullOrEmpty() && (overwriteReserved || instance.DisplayName.IsNullOrEmpty()))
                instance.DisplayName = other.DisplayName;

            if (!other.Email.IsNullOrEmpty() && (overwriteReserved || instance.Email.IsNullOrEmpty()))
                instance.Email = other.Email;

            var userAuth = instance as IUserAuth;
            if (userAuth != null)
            {
                if (!other.Email.IsNullOrEmpty() && (overwriteReserved || userAuth.PrimaryEmail.IsNullOrEmpty()))
                    userAuth.PrimaryEmail = other.Email;
            }


            if (!other.PhoneNumber.IsNullOrEmpty())
                instance.PhoneNumber = other.PhoneNumber;

            if (!other.FirstName.IsNullOrEmpty())
                instance.FirstName = other.FirstName;

            if (!other.LastName.IsNullOrEmpty())
                instance.LastName = other.LastName;

            if (!other.FullName.IsNullOrEmpty())
                instance.FullName = other.FullName;

            if (!other.Company.IsNullOrEmpty())
                instance.Company = other.Company;

            if (other.BirthDate != null)
                instance.BirthDate = other.BirthDate;

            if (!other.BirthDateRaw.IsNullOrEmpty())
                instance.BirthDateRaw = other.BirthDateRaw;

            if (!other.Address.IsNullOrEmpty())
                instance.Address = other.Address;

            if (!other.Address2.IsNullOrEmpty())
                instance.Address2 = other.Address2;

            if (!other.City.IsNullOrEmpty())
                instance.City = other.City;

            if (!other.State.IsNullOrEmpty())
                instance.State = other.State;

            if (!other.Country.IsNullOrEmpty())
                instance.Country = other.Country;

            if (!other.Culture.IsNullOrEmpty())
                instance.Culture = other.Culture;

            if (!other.Gender.IsNullOrEmpty())
                instance.Gender = other.Gender;

            if (!other.MailAddress.IsNullOrEmpty())
                instance.MailAddress = other.MailAddress;

            if (!other.Nickname.IsNullOrEmpty())
                instance.Nickname = other.Nickname;

            if (!other.PostalCode.IsNullOrEmpty())
                instance.PostalCode = other.PostalCode;

            if (!other.TimeZone.IsNullOrEmpty())
                instance.TimeZone = other.TimeZone;
        }

        public static T Get<T>(this IMeta instance)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");
 
            if (instance.Meta == null)
                return default(T);

            string str;
            instance.Meta.TryGetValue(typeof(T).GetOperationName(), out str);
            return str == null ? default(T) : TypeSerializer.DeserializeFromString<T>(str);
        }

        public static bool TryGet<T>(this IMeta instance, out T value)
        {
            value = default(T);
            if (instance == null)
                throw new ArgumentNullException("instance");

            string str;
            if (!instance.Meta.TryGetValue(typeof(T).GetOperationName(), out str))
                return false;

            value = TypeSerializer.DeserializeFromString<T>(str);
            return true;
        }

        public static T Set<T>(this IMeta instance, T value)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");

            if (instance.Meta == null)
                instance.Meta = new Dictionary<string, string>();

            instance.Meta[typeof(T).GetOperationName()] = TypeSerializer.SerializeToString(value);
            return value;
        }

        public static AuthTokens ToAuthTokens(this IAuthTokens from)
        {
            return new AuthTokens {
                Provider = from.Provider,
                UserId = from.UserId,
                AccessToken = from.AccessToken,
                AccessTokenSecret = from.AccessTokenSecret,
                RefreshToken = from.RefreshToken,
                RefreshTokenExpiry = from.RefreshTokenExpiry,
                RequestToken = from.RequestToken,
                RequestTokenSecret = from.RequestTokenSecret,
                Items = from.Items,
            };
        }
    }

}