namespace ServiceStack;

/// <summary>
/// https://www.iana.org/assignments/jwt/jwt.xhtml#claims
/// </summary>
public static class JwtClaimTypes
{
    public const string DefaultProfileUrl = "data:image/svg+xml,%3Csvg width='100' height='100' viewBox='0 0 100 100' xmlns='http://www.w3.org/2000/svg'%3E%3Cstyle%3E .path%7B%7D %3C/style%3E%3Cg id='male-svg'%3E%3Cpath fill='%23556080' d='M1 92.84V84.14C1 84.14 2.38 78.81 8.81 77.16C8.81 77.16 19.16 73.37 27.26 69.85C31.46 68.02 32.36 66.93 36.59 65.06C36.59 65.06 37.03 62.9 36.87 61.6H40.18C40.18 61.6 40.93 62.05 40.18 56.94C40.18 56.94 35.63 55.78 35.45 47.66C35.45 47.66 32.41 48.68 32.22 43.76C32.1 40.42 29.52 37.52 33.23 35.12L31.35 30.02C31.35 30.02 28.08 9.51 38.95 12.54C34.36 7.06 64.93 1.59 66.91 18.96C66.91 18.96 68.33 28.35 66.91 34.77C66.91 34.77 71.38 34.25 68.39 42.84C68.39 42.84 66.75 49.01 64.23 47.62C64.23 47.62 64.65 55.43 60.68 56.76C60.68 56.76 60.96 60.92 60.96 61.2L64.74 61.76C64.74 61.76 64.17 65.16 64.84 65.54C64.84 65.54 69.32 68.61 74.66 69.98C84.96 72.62 97.96 77.16 97.96 81.13C97.96 81.13 99 86.42 99 92.85L1 92.84Z'/%3E%3C/g%3E%3C/svg%3E";

    public const string Type = "typ";
    public const string Algorithm = "alg";
    public const string KeyId = "kid";

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
    public const string Groups = "groups";
    public const string IdentityProvider = "idp";
    public const string Role = "role";
    public const string Permission = "perm";
    public const string Roles = "roles";
    public const string Permissions = "perms";
    public const string IdentityRole = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
    public const string ApiKey = "apikey";
}
