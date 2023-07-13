using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public class UserAuth : IUserAuth
    {
        [AutoIncrement]
        public virtual int Id { get; set; }

        [Index]
        public virtual string UserName { get; set; }
        [Index]
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
        public virtual List<string> Roles { get; set; } = new List<string>();
        public virtual List<string> Permissions { get; set; } = new List<string>();
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
        public virtual Dictionary<string, string> Items { get; set; } = new Dictionary<string, string>();
        public virtual string AccessToken { get; set; }
        public virtual string AccessTokenSecret { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual DateTime ModifiedDate { get; set; }

        //Custom Reference Data
        public virtual int? RefId { get; set; }
        public virtual string RefIdStr { get; set; }
        public virtual Dictionary<string, string> Meta { get; set; }
    }

    public class UserAuthRole : IMeta
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
                throw new ArgumentNullException(nameof(instance));

            if (tokens == null)
                throw new ArgumentNullException(nameof(tokens));

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
                if (instance.Items == null)
                    instance.Items = new Dictionary<string, string>();

                if (instance.Items != tokens.Items)
                {
                    foreach (var entry in tokens.Items)
                    {
                        instance.Items[entry.Key] = entry.Value;
                    }
                }
            }

            PopulateMissingExtended(instance, tokens, overwriteReserved);
        }

        public static void PopulateMissingExtended(this IUserAuthDetailsExtended instance, 
            IUserAuthDetailsExtended other, bool overwriteReserved = false)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (other == null)
                throw new ArgumentNullException(nameof(other));

            // Don't override the Master UserAuth table's reserved fields if they already exists
            if (!other.UserName.IsNullOrEmpty() && (overwriteReserved || instance.UserName.IsNullOrEmpty()))
                instance.UserName = other.UserName;

            if (!other.DisplayName.IsNullOrEmpty() && (overwriteReserved || instance.DisplayName.IsNullOrEmpty()))
                instance.DisplayName = other.DisplayName;

            if (!other.Email.IsNullOrEmpty() && (overwriteReserved || instance.Email.IsNullOrEmpty()))
                instance.Email = other.Email;

            if (instance is IUserAuth userAuth)
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
                throw new ArgumentNullException(nameof(instance));
 
            if (instance.Meta == null)
                return default(T);

            instance.Meta.TryGetValue(typeof(T).GetOperationName(), out var str);
            return str == null ? default(T) : TypeSerializer.DeserializeFromString<T>(str);
        }

        public static bool TryGet<T>(this IMeta instance, out T value)
        {
            value = default(T);
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (!instance.Meta.TryGetValue(typeof(T).GetOperationName(), out var str))
                return false;

            value = TypeSerializer.DeserializeFromString<T>(str);
            return true;
        }

        public static T Set<T>(this IMeta instance, T value)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

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

        public static void RecordSuccessfulLogin(this IUserAuthRepository repo, IUserAuth userAuth)
        {
            repo.RecordSuccessfulLogin(userAuth, rehashPassword:false, password:null);
        }

        public static async Task RecordSuccessfulLoginAsync(this IUserAuthRepositoryAsync repo, IUserAuth userAuth, CancellationToken token=default)
        {
            await repo.RecordSuccessfulLoginAsync(userAuth, rehashPassword:false, password:null, token: token);
        }
        
        public static void RecordSuccessfulLogin(this IUserAuthRepository repo, IUserAuth userAuth, bool rehashPassword, string password)
        {
            var recordLoginAttempts = HostContext.GetPlugin<AuthFeature>()?.MaxLoginAttempts != null;
            if (recordLoginAttempts)
            {
                userAuth.InvalidLoginAttempts = 0;
                userAuth.LastLoginAttempt = userAuth.ModifiedDate = DateTime.UtcNow;
            }

            if (rehashPassword)
            {
                userAuth.PopulatePasswordHashes(password);
            }

            if (recordLoginAttempts || rehashPassword)
            {
                repo.SaveUserAuth(userAuth);
            }
        }
        
        public static async Task RecordSuccessfulLoginAsync(this IUserAuthRepositoryAsync repo, IUserAuth userAuth, bool rehashPassword, string password, CancellationToken token=default)
        {
            var recordLoginAttempts = HostContext.GetPlugin<AuthFeature>()?.MaxLoginAttempts != null;
            if (recordLoginAttempts)
            {
                userAuth.InvalidLoginAttempts = 0;
                userAuth.LastLoginAttempt = userAuth.ModifiedDate = DateTime.UtcNow;
            }

            if (rehashPassword)
            {
                userAuth.PopulatePasswordHashes(password);
            }

            if (recordLoginAttempts || rehashPassword)
            {
                await repo.SaveUserAuthAsync(userAuth, token);
            }
        }

        public static void RecordInvalidLoginAttempt(this IUserAuthRepository repo, IUserAuth userAuth)
        {
            var feature = HostContext.GetPlugin<AuthFeature>();
            if (feature?.MaxLoginAttempts == null) return;

            userAuth.InvalidLoginAttempts += 1;
            userAuth.LastLoginAttempt = userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.InvalidLoginAttempts >= feature.MaxLoginAttempts.Value)
            {
                userAuth.LockedDate = userAuth.LastLoginAttempt;
            }
            repo.SaveUserAuth(userAuth);
        }

        public static async Task RecordInvalidLoginAttemptAsync(this IUserAuthRepositoryAsync repo, IUserAuth userAuth, CancellationToken token=default)
        {
            var feature = HostContext.GetPlugin<AuthFeature>();
            if (feature?.MaxLoginAttempts == null) return;

            userAuth.InvalidLoginAttempts += 1;
            userAuth.LastLoginAttempt = userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.InvalidLoginAttempts >= feature.MaxLoginAttempts.Value)
            {
                userAuth.LockedDate = userAuth.LastLoginAttempt;
            }
            await repo.SaveUserAuthAsync(userAuth, token);
        }

        public static void PopulateFromSession(this IAuthSession session, IAuthSession from)
        {
            session.Id = from.Id;
            session.ReferrerUrl = from.ReferrerUrl;
            session.UserAuthId = from.UserAuthId;
            session.UserAuthName = from.UserAuthName;
            session.UserName = from.UserName;
            session.DisplayName = from.DisplayName;
            session.FirstName = from.FirstName;
            session.LastName = from.LastName;
            session.Email = from.Email;
            session.ProviderOAuthAccess = new(from.ProviderOAuthAccess);
            session.CreatedAt = from.CreatedAt;
            session.LastModified = from.LastModified;
            session.Roles = from.Roles != null ? new(from.Roles) : session.Roles;
            session.Permissions = from.Permissions != null ? new(from.Permissions) : from.Permissions;
            session.IsAuthenticated = from.IsAuthenticated;
            session.FromToken = from.FromToken;
            session.AuthProvider = from.AuthProvider;
            session.ProfileUrl = from.ProfileUrl;
            session.Sequence = from.Sequence;

            if (session is IAuthSessionExtended extended && from is IAuthSessionExtended other)
            {
                extended.Company = other.Company;
                extended.PrimaryEmail = other.PrimaryEmail;
                extended.BirthDate = other.BirthDate;
                extended.Address = other.Address;
                extended.Address2 = other.Address2;
                extended.City = other.City;
                extended.State = other.State;
                extended.PostalCode = other.PostalCode;
                extended.Country = other.Country;
                extended.PhoneNumber = other.PhoneNumber;
                extended.Country = other.Country;
                extended.PhoneNumber = other.PhoneNumber;
                extended.BirthDateRaw = other.BirthDateRaw;
                extended.Gender = other.Gender;
                extended.Audiences = other.Audiences != null ? new(other.Audiences) : other.Audiences;
                extended.Scopes = other.Scopes != null ? new(other.Scopes) : other.Scopes;
                extended.Dns = other.Dns;
                extended.Rsa = other.Rsa;
                extended.Sid = other.Sid;
                extended.Hash = other.Hash;
                extended.HomePhone = other.HomePhone;
                extended.MobilePhone = other.MobilePhone;
                extended.Webpage = other.Webpage;
                extended.EmailConfirmed = other.EmailConfirmed;
                extended.PhoneNumberConfirmed = other.PhoneNumberConfirmed;
                extended.TwoFactorEnabled = other.TwoFactorEnabled;
                extended.SecurityStamp = other.SecurityStamp;
                extended.Type = other.Type;
                extended.RecoveryToken = other.RecoveryToken;
                extended.RefId = other.RefId;
                extended.RefIdStr = other.RefIdStr;
            }
        }
        
        public static void PopulateFromMap(this IAuthSession session, IDictionary<string, string> map)
        {
            var authSession = session as AuthUserSession ?? new AuthUserSession(); //Null Object Pattern
            session.IsAuthenticated = true;
            var jsonObj = map as JsonObject;

            foreach (var entry in map)
            {
                switch (entry.Key)
                {
                    case "jid":
                    case "Id":
                        session.Id = entry.Value;
                        break;
                    case "IsAuthenticated":
                        session.IsAuthenticated = entry.Value.FromJsv<bool>();
                        break;
                    case "FromToken":
                        session.FromToken = entry.Value.FromJsv<bool>();
                        break;
                    case JwtClaimTypes.Subject:
                        session.UserAuthId = entry.Value.LastRightPart('|'); //in-case of multi-components, last should contain userId
                        break;
                    case "UserAuthId":
                        session.UserAuthId = entry.Value;
                        break;
                    case JwtClaimTypes.Email:
                    case "Email":
                        session.Email = entry.Value;
                        break;
                    case "UserName":
                    case JwtClaimTypes.PreferredUserName:
                        session.UserName = entry.Value;
                        break;
                    case JwtClaimTypes.Name:
                    case "DisplayName":
                        session.DisplayName = entry.Value;
                        break;
                    case JwtClaimTypes.Picture:
                    case "ProfileUrl":
                        session.ProfileUrl = entry.Value;
                        break;
                    case JwtClaimTypes.Roles:
                    case "Roles":
                        var jsonRoles = jsonObj != null
                            ? jsonObj.GetUnescaped("roles") ?? jsonObj.GetUnescaped("Roles")
                            : entry.Value;
                        session.Roles = jsonRoles.FromJson<List<string>>();
                        break;
                    case JwtClaimTypes.Permissions:
                    case "Permissions":
                        var jsonPerms = jsonObj != null
                            ? jsonObj.GetUnescaped("perms") ?? jsonObj.GetUnescaped("Perms") ?? jsonObj.GetUnescaped("Permissions")
                            : entry.Value;
                        session.Permissions = jsonPerms.FromJson<List<string>>();
                        break;
                    case JwtClaimTypes.IssuedAt:
                    case "CreatedAt":
                        session.CreatedAt = long.Parse(entry.Value).FromUnixTime();
                        break;
                    case "ReferrerUrl":
                        session.ReferrerUrl = entry.Value;
                        break;
                    case "UserAuthName":
                        session.UserAuthName = entry.Value;
                        break;
                    case "TwitterUserId":
                        authSession.TwitterUserId = entry.Value;
                        break;
                    case "TwitterScreenName":
                        authSession.TwitterScreenName = entry.Value;
                        break;
                    case "FacebookUserId":
                        authSession.FacebookUserId = entry.Value;
                        break;
                    case "FacebookUserName":
                        authSession.FacebookUserName = entry.Value;
                        break;
                    case JwtClaimTypes.GivenName:
                    case "GivenName":
                    case "FirstName":
                        session.FirstName = entry.Value;
                        break;
                    case JwtClaimTypes.FamilyName:
                    case "Surname":
                    case "LastName":
                        session.LastName = entry.Value;
                        break;
                    case "Company":
                        authSession.Company = entry.Value;
                        break;
                    case "PrimaryEmail":
                        authSession.PrimaryEmail = entry.Value;
                        break;
                    case "PhoneNumber":
                        authSession.PhoneNumber = entry.Value;
                        break;
                    case "BirthDate":
                        authSession.BirthDate = long.Parse(entry.Value).FromUnixTime();
                        break;
                    case "Address":
                        authSession.Address = entry.Value;
                        break;
                    case "Address2":
                        authSession.Address2 = entry.Value;
                        break;
                    case "City":
                        authSession.City = entry.Value;
                        break;
                    case "State":
                        authSession.State = entry.Value;
                        break;
                    case "Country":
                        authSession.Country = entry.Value;
                        break;
                    case "Culture":
                        authSession.Culture = entry.Value;
                        break;
                    case "FullName":
                        authSession.FullName = entry.Value;
                        break;
                    case "Gender":
                        authSession.Gender = entry.Value;
                        break;
                    case "Language":
                        authSession.Language = entry.Value;
                        break;
                    case "MailAddress":
                        authSession.MailAddress = entry.Value;
                        break;
                    case "Nickname":
                        authSession.Nickname = entry.Value;
                        break;
                    case "PostalCode":
                        authSession.PostalCode = entry.Value;
                        break;
                    case "TimeZone":
                        authSession.TimeZone = entry.Value;
                        break;
                    case "RequestTokenSecret":
                        authSession.RequestTokenSecret = entry.Value;
                        break;
                    case "LastModified":
                        session.LastModified = long.Parse(entry.Value).FromUnixTime();
                        break;
                    case "Sequence":
                        session.Sequence = entry.Value;
                        break;
                    case "Tag":
                        authSession.Tag = long.Parse(entry.Value);
                        break;
                    case "Dns":
                        authSession.Dns = entry.Value;
                        break;
                    case "Rsa":
                        authSession.Rsa = entry.Value;
                        break;
                    case "Sid":
                        authSession.Sid = entry.Value;
                        break;
                    case "Hash":
                        authSession.Hash = entry.Value;
                        break;
                    case "HomePhone":
                        authSession.HomePhone = entry.Value;
                        break;
                    case "MobilePhone":
                        authSession.MobilePhone = entry.Value;
                        break;
                    case "Webpage":
                        authSession.Webpage = entry.Value;
                        break;
                    case "EmailConfirmed":
                        authSession.EmailConfirmed = entry.Value.FromJsv<bool>();
                        break;
                    case "PhoneNumberConfirmed":
                        authSession.PhoneNumberConfirmed = entry.Value.FromJsv<bool>();
                        break;
                    case "TwoFactorEnabled":
                        authSession.TwoFactorEnabled = entry.Value.FromJsv<bool>();
                        break;
                    case "SecurityStamp":
                        authSession.SecurityStamp = entry.Value;
                        break;
                }
            }
            authSession.UserAuthName ??= authSession.UserName ?? authSession.Email;
        }
        
        public static List<Claim> ConvertSessionToClaims(this IAuthSession session,
            string issuer = null, string roleClaimType=ClaimTypes.Role, string permissionClaimType=JwtClaimTypes.Permissions)
        {
            var claims = new List<Claim>();

            void addClaim(string type, string value)
            {
                if (value == null)
                    return;

                claims.Add(new Claim(type, value, ClaimValueTypes.String, issuer));
            }

            addClaim(ClaimTypes.NameIdentifier, session.Id);
            addClaim(ClaimTypes.Email, session.Email);
            addClaim(ClaimTypes.Name, session.UserAuthName);
            addClaim(ClaimTypes.GivenName, session.FirstName);
            addClaim(ClaimTypes.Surname, session.LastName);
            addClaim(ClaimTypes.AuthenticationMethod, session.AuthProvider);

            if (session is IAuthSessionExtended sessionExt)
            {
                addClaim(ClaimTypes.StreetAddress, sessionExt.Address);
                addClaim(ClaimTypes.Locality, sessionExt.City);
                addClaim(ClaimTypes.StateOrProvince, sessionExt.State);
                addClaim(ClaimTypes.PostalCode, sessionExt.PostalCode);
                addClaim(ClaimTypes.Country, sessionExt.Country);
                addClaim(ClaimTypes.HomePhone, sessionExt.HomePhone);
                addClaim(ClaimTypes.MobilePhone, sessionExt.MobilePhone);
                addClaim(ClaimTypes.DateOfBirth, sessionExt.BirthDateRaw ?? sessionExt.BirthDate?.ToShortDateString());
                addClaim(ClaimTypes.Gender, sessionExt.Gender);
                addClaim(ClaimTypes.Dns, sessionExt.Dns);
                addClaim(ClaimTypes.Rsa, sessionExt.Rsa);
                addClaim(ClaimTypes.Sid, sessionExt.Sid);
                addClaim(ClaimTypes.Hash, sessionExt.Hash);
                addClaim(ClaimTypes.HomePhone, sessionExt.HomePhone);
                addClaim(ClaimTypes.MobilePhone, sessionExt.MobilePhone);
                addClaim(ClaimTypes.OtherPhone, sessionExt.PhoneNumber);
                addClaim(ClaimTypes.Webpage, sessionExt.Webpage);
            }

            if (session.Roles != null)
            {
                foreach (var role in session.Roles)
                {
                    addClaim(roleClaimType, role);
                }
            }

            if (session.Permissions != null)
            {
                foreach (var permission in session.Permissions)
                {
                    addClaim(permissionClaimType, permission);
                }
            }
 
            return claims;
        }
    }

}