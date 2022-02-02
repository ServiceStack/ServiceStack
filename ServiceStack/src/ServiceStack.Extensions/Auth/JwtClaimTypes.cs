using Microsoft.IdentityModel.Tokens;

namespace ServiceStack.Auth
{
    /// <summary>
    /// https://www.iana.org/assignments/jwt/jwt.xhtml#claims
    /// </summary>
    public static class JwtClaimTypes
    {
        public const string Issuer = "iss";
        public const string Subject = "sub";
        public const string Audience = "aud";
        public const string Expiration = "exp";
        public const string NotBefore = "nbf";
        public const string IssuedAt = "iat";
        public const string JwtId = "jti";

        /// <summary>
        /// Full name
        /// </summary>
        public const string Name = "name";
        /// <summary>
        /// Given name(s) or first name(s)
        /// </summary>
        public const string GivenName = "given_name";
        /// <summary>
        /// Surname(s) or last name(s)	
        /// </summary>
        public const string FamilyName = "family_name";
        public const string MiddleName = "middle_name";
        public const string NickName = "nickname";
        public const string PreferredUserName = "preferred_username";
        public const string Profile = "profile";
        public const string Picture = "picture";
        public const string WebSite = "website";
        public const string Email = "email";
        public const string EmailVerified = "email_verified";
        public const string Gender = "gender";
        public const string BirthDate = "birthdate";
        public const string ZoneInfo = "zoneinfo";
        public const string Locale = "locale";
        public const string PhoneNumber = "phone_number";
        public const string PhoneNumberVerified = "phone_number_verified";
        public const string Address = "address";
        public const string UpdatedAt = "updated_at";
        public const string AuthorizedParty = "azp";
        public const string Nonce = "nonce";
        public const string AuthTime = "auth_time";
        public const string AccessTokenHash = "at_hash";
        public const string CodeHash = "c_hash";
        public const string AuthClass = "acr";
        public const string AuthMethod = "amr";
        public const string Confirmation = "cnf";
        public const string OriginIdentity = "orig";
        public const string DestinationIdentity = "dest";
        public const string Events = "events";
        public const string TimeOfEvent = "toe";
        public const string TransactionIdentifier = "txn";
        public const string SessionId = "sid";
        public const string Actor = "act";
        public const string Scope = "scope";
        public const string ClientId = "client_id";
        public const string MayAct = "may_act";
        public const string Roles = "roles";
        public const string Groups = "groups";
        public const string IdentityProvider = "idp";
        public const string Permissions = "perms";

        public static TokenValidationParameters UseStandardJwtClaims(this TokenValidationParameters options)
        {
            options.NameClaimType = Name;
            options.RoleClaimType = Roles;
            return options;
        }
    }
}