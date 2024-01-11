using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth;

/// <summary>
/// Enable access to protected Services using JWT Tokens
/// </summary>
public class JwtAuthProviderReader : AuthProvider, IAuthWithRequest, IAuthPlugin
{
    public override string Type => "Bearer";
    public static RsaKeyLengths UseRsaKeyLength = RsaKeyLengths.Bit2048;

    public const string Name = AuthenticateService.JwtProvider;
    public const string Realm = "/auth/" + AuthenticateService.JwtProvider;

    public static readonly HashSet<string> IgnoreForOperationTypes = [
        nameof(StaticFileHandler)
    ];

    /// <summary>
    /// Different HMAC Algorithms supported
    /// </summary>
    public static readonly Dictionary<string, Func<byte[], byte[], byte[]>> HmacAlgorithms = new()
    {
        { "HS256", (key, value) => {
            using var sha = new HMACSHA256(key);
            return sha.ComputeHash(value);
        } },
        { "HS384", (key, value) => {
            using var sha = new HMACSHA384(key);
            return sha.ComputeHash(value);
        } },
        { "HS512", (key, value) => {
            using var sha = new HMACSHA512(key);
            return sha.ComputeHash(value);
        } }
    };

    /// <summary>
    /// Different RSA Signing Algorithms supported
    /// </summary>
    public static readonly Dictionary<string, Func<RSAParameters, byte[], byte[]>> RsaSignAlgorithms = new()
    {
        { "RS256", (key, value) => RsaUtils.Authenticate(value, key, "SHA256", UseRsaKeyLength) },
        { "RS384", (key, value) => RsaUtils.Authenticate(value, key, "SHA384", UseRsaKeyLength) },
        { "RS512", (key, value) => RsaUtils.Authenticate(value, key, "SHA512", UseRsaKeyLength) },
    };

    public static readonly Dictionary<string, Func<RSAParameters, byte[], byte[], bool>> RsaVerifyAlgorithms = new()
    {
        { "RS256", (key, value, sig) => RsaUtils.Verify(value, sig, key, "SHA256", UseRsaKeyLength) },
        { "RS384", (key, value, sig) => RsaUtils.Verify(value, sig, key, "SHA384", UseRsaKeyLength) },
        { "RS512", (key, value, sig) => RsaUtils.Verify(value, sig, key, "SHA512", UseRsaKeyLength) },
    };

    /// <summary>
    /// Whether to only allow access via API Key from a secure connection. (default true)
    /// </summary>
    public bool RequireSecureConnection { get; set; }

    /// <summary>
    /// Run custom filter after JWT Header is created
    /// </summary>
    public Action<JsonObject, IAuthSession> CreateHeaderFilter { get; set; }

    /// <summary>
    /// Run custom filter after JWT Payload is created
    /// </summary>
    public Action<JsonObject, IAuthSession> CreatePayloadFilter { get; set; }

    /// <summary>
    /// Run custom filter after session is restored from a JWT Token
    /// </summary>
    public Action<IAuthSession, JsonObject, IRequest> PopulateSessionFilter { get; set; }

    /// <summary>
    /// Run Async custom filter after session is restored from a JWT Token
    /// </summary>
    public Func<IAuthSession, JsonObject, IRequest, Task> PopulateSessionFilterAsync { get; set; }

    /// <summary>
    /// Whether to encrypt JWE Payload (default false). 
    /// Uses RSA-OAEP for Key Encryption and AES/128/CBC HMAC SHA256 for Content Encryption
    /// </summary>
    public bool EncryptPayload { get; set; }

    /// <summary>
    /// Which Hash Algorithm should be used to sign the JWT Token. (default HS256)
    /// </summary>
    public string HashAlgorithm { get; set; }

    /// <summary>
    /// Whether to only allow processing of JWT Tokens using the configured HashAlgorithm. (default true)
    /// </summary>
    public bool RequireHashAlgorithm { get; set; }

    /// <summary>
    /// The Issuer to embed in the token. (default ssjwt)
    /// </summary>
    public string Issuer { get; set; }

    /// <summary>
    /// The Audience to embed in the token. (default null)
    /// </summary>
    public string Audience
    {
        get => Audiences.Join(",");
        set
        {
            Audiences.Clear();
            if (!string.IsNullOrEmpty(value))
            {
                Audiences.Add(value);
            }
        }
    }
        
    /// <summary>
    /// Embed Multiple Audiences in the token. (default none)
    /// A JWT is valid if it contains ANY audience in this List
    /// </summary>
    public List<string> Audiences { get; set; }
        
    /// <summary>
    /// Tokens must contain aud which is validated
    /// </summary>
    public bool RequiresAudience { get; set; }

    /// <summary>
    /// What Id to use to identify the Key used to sign the token. (default First 3 chars of Base64 Key)
    /// </summary>
    public string KeyId { get; set; }

    /// <summary>
    /// The AuthKey used to sign the JWT Token
    /// </summary>
    public byte[] AuthKey { get; set; }
    public string AuthKeyBase64
    {
        set => AuthKey = Convert.FromBase64String(value);
    }

    public byte[] GetAuthKey(IRequest req = null) => req.GetRuntimeConfig(nameof(AuthKey), AuthKey);

    /// <summary>
    /// Allow verification using multiple Auth keys
    /// </summary>
    public List<byte[]> FallbackAuthKeys { get; set; }

    public List<byte[]> GetFallbackAuthKeys(IRequest req = null) => req.GetRuntimeConfig(nameof(FallbackAuthKeys), FallbackAuthKeys);

    public RSAParameters? GetPrivateKey(IRequest req = null) => req.GetRuntimeConfig(nameof(PrivateKey), PrivateKey);

    /// <summary>
    /// The RSA Private Key used to Sign the JWT Token when RSA is used
    /// </summary>
    private RSAParameters? _privateKey;
    public RSAParameters? PrivateKey
    {
        get => _privateKey;
        set
        {
            _privateKey = value;
            if (_privateKey != null)
                PublicKey = _privateKey.Value.ToPublicRsaParameters();
        }
    }

    /// <summary>
    /// Convenient overload to initialize the Private Key via exported XML
    /// </summary>
    public string PrivateKeyXml
    {
        get => PrivateKey?.FromPrivateRSAParameters();
        set => PrivateKey = value?.ToPrivateRSAParameters();
    }

    public RSAParameters? GetPublicKey(IRequest req = null) => req.GetRuntimeConfig(nameof(PublicKey), PublicKey);

    /// <summary>
    /// The RSA Public Key used to Verify the JWT Token when RSA is used
    /// </summary>
    public RSAParameters? PublicKey { get; set; }

    /// <summary>
    /// Convenient overload to initialize the Public Key via exported XML
    /// </summary>
    public string PublicKeyXml
    {
        get => PublicKey?.FromPublicRSAParameters();
        set => PublicKey = value?.ToPublicRSAParameters();
    }

    /// <summary>
    /// Allow verification using multiple public keys
    /// </summary>
    public List<RSAParameters> FallbackPublicKeys { get; set; }

    public List<RSAParameters> GetFallbackPublicKeys(IRequest req = null) => 
        req.GetRuntimeConfig(nameof(FallbackPublicKeys), FallbackPublicKeys);

    /// <summary>
    /// Allow verification using multiple private keys for JWE tokens
    /// </summary>
    public List<RSAParameters> FallbackPrivateKeys { get; set; }

    public List<RSAParameters> GetFallbackPrivateKeys(IRequest req = null) => 
        req.GetRuntimeConfig(nameof(FallbackPrivateKeys), FallbackPrivateKeys);

    /// <summary>
    /// How long should JWT Tokens be valid for. (default 14 days)
    /// </summary>
    public TimeSpan ExpireTokensIn { get; set; }

    /// <summary>
    /// How long should JWT Refresh Tokens be valid for. (default 365 days)
    /// </summary>
    public TimeSpan ExpireRefreshTokensIn { get; set; }

    /// <summary>
    /// Convenient overload to initialize ExpireTokensIn with an Integer
    /// </summary>
    public int ExpireTokensInDays
    {
        set
        {
            if (value > 0)
                ExpireTokensIn = TimeSpan.FromDays(value);
        }
    }

    /// <summary>
    /// Allow custom logic to invalidate JWT Tokens
    /// </summary>
    public Func<JsonObject, IRequest, bool> ValidateToken { get; set; }

    /// <summary>
    /// Allow custom logic to invalidate Refresh Tokens
    /// </summary>
    public Func<JsonObject, IRequest, bool> ValidateRefreshToken { get; set; }

    /// <summary>
    /// Whether to invalidate all JWT Access Tokens issued before a specified date.
    /// </summary>
    public DateTime? InvalidateTokensIssuedBefore { get; set; }

    /// <summary>
    /// Whether to invalidate all JWT Refresh Tokens issued before a specified date.
    /// </summary>
    public DateTime? InvalidateRefreshTokensIssuedBefore { get; set; }

    /// <summary>
    /// Modify the registration of ConvertSessionToToken Service
    /// </summary>
    public Dictionary<Type, string[]> ServiceRoutes { get; set; }

    /// <summary>
    /// Allow JWT in ?ss-tok=jwt QueryString. (default false)
    /// </summary>
    public bool AllowInQueryString { get; set; }

    /// <summary>
    /// Allow JWT in ss-tok=jwt HTML POST FormData. (default false)
    /// </summary>
    public bool AllowInFormData { get; set; }

    /// <summary>
    /// Whether to automatically remove expired or invalid cookies
    /// </summary>
    public bool RemoveInvalidTokenCookie { get; set; }

    /// <summary>
    /// Whether to also Include Token in ConvertSessionToTokenResponse   
    /// </summary>
    public bool IncludeJwtInConvertSessionToTokenResponse { get; set; }

    /// <summary>
    /// Whether to store JWTs in Cookies (ss-tok) for successful Authentications (default true) 
    /// </summary>
    public bool UseTokenCookie { get; set; } = true;

    /// <summary>
    /// Override conversion to Unix Time used in issuing JWTs and validation
    /// </summary>
    public Func<DateTime,long> ResolveUnixTime { get; set; } = DefaultResolveUnixTime;
    public static long DefaultResolveUnixTime(DateTime dateTime) => dateTime.ToUnixTime();

    /// <summary>
    /// Inspect or modify JWT Payload before validation, return error message if invalid else null 
    /// </summary>
    public Func<Dictionary<string,string>, string> PreValidateJwtPayloadFilter { get; set; }

    /// <summary>
    /// Change resolution for resolving unique jti id for Access Tokens
    /// </summary>
    public Func<IRequest,string> ResolveJwtId { get; set; }

    /// <summary>
    /// Get the next AutoId for usage in jti JWT Access Tokens  
    /// </summary>
    public string NextJwtId() => Interlocked.Increment(ref accessIdCounter).ToString(); 
    private long accessIdCounter;
        
    /// <summary>
    /// Get the last jti AutoId generated  
    /// </summary>
    public string LastJwtId() => Interlocked.Read(ref accessIdCounter).ToString();

    /// <summary>
    /// Change resolution for resolving unique jti id for Refresh Tokens
    /// </summary>
    public Func<IRequest,string> ResolveRefreshJwtId { get; set; }

    /// <summary>
    /// Get the next AutoId for usage in jti JWT Refresh Tokens  
    /// </summary>
    public string NextRefreshJwtId() => Interlocked.Decrement(ref refreshIdCounter).ToString(); 

    private long refreshIdCounter;
    public string LastRefreshJwtId() => Interlocked.Read(ref refreshIdCounter).ToString();

    /// <summary>
    /// Invalidate JWTs with ids
    /// </summary>
    public HashSet<string> InvalidateJwtIds { get; set; } = new();
        
    public JwtAuthProviderReader()
        : base(null, Realm, Name)
    {
        Init();
    }

    public JwtAuthProviderReader(IAppSettings appSettings)
        : base(appSettings, Realm, Name)
    {
        Init(appSettings);
    }

    public virtual void Init(IAppSettings appSettings = null)
    {
        Label = "JWT";
        FormLayout =
        [
            new InputInfo(nameof(IHasBearerToken.BearerToken), Html.Input.Types.Textarea)
            {
                Label = "JWT",
                Placeholder = "JWT Bearer Token",
                Required = true,
            }
        ];
            
        RequireSecureConnection = true;
        EncryptPayload = false;
        HashAlgorithm = "HS256";
        RequireHashAlgorithm = true;
        RemoveInvalidTokenCookie = true;
        Issuer = "ssjwt";
        Audiences = [];
        ExpireTokensIn = TimeSpan.FromDays(14);
        ExpireRefreshTokensIn = TimeSpan.FromDays(365);
        FallbackAuthKeys = [];
        FallbackPublicKeys = [];
        FallbackPrivateKeys = [];

        if (appSettings != null)
        {
            RequireSecureConnection = appSettings.Get("jwt.RequireSecureConnection", RequireSecureConnection);
            RequireHashAlgorithm = appSettings.Get("jwt.RequireHashAlgorithm", RequireHashAlgorithm);
            EncryptPayload = appSettings.Get("jwt.EncryptPayload", EncryptPayload);
            AllowInQueryString = appSettings.Get("jwt.AllowInQueryString", AllowInQueryString);
            AllowInFormData = appSettings.Get("jwt.AllowInFormData", AllowInFormData);
            RequiresAudience = appSettings.Get("jwt.RequiresAudience", RequiresAudience);
            IncludeJwtInConvertSessionToTokenResponse = appSettings.Get("jwt.IncludeJwtInConvertSessionToTokenResponse", IncludeJwtInConvertSessionToTokenResponse);
            UseTokenCookie = appSettings.Get("jwt.UseTokenCookie", UseTokenCookie);

            Issuer = appSettings.GetString("jwt.Issuer");
            KeyId = appSettings.GetString("jwt.KeyId");
            Audience = appSettings.GetString("jwt.Audience");
            if (appSettings.Exists("jwt.Audiences"))
            {
                var audiences = appSettings.GetList("jwt.Audiences");
                if (!audiences.IsEmpty())
                {
                    Audiences = audiences.ToList();
                }
            }

            var hashAlg = appSettings.GetString("jwt.HashAlgorithm");
            if (!string.IsNullOrEmpty(hashAlg))
                HashAlgorithm = hashAlg;

            var privateKeyXml = appSettings.GetString("jwt.PrivateKeyXml");
            if (privateKeyXml != null)
                PrivateKeyXml = privateKeyXml;

            var publicKeyXml = appSettings.GetString("jwt.PublicKeyXml");
            if (publicKeyXml != null)
                PublicKeyXml = publicKeyXml;

            var base64 = appSettings.GetString("jwt.AuthKeyBase64");
            if (base64 != null)
                AuthKeyBase64 = base64;

            var dateStr = appSettings.GetString("jwt.InvalidateTokensIssuedBefore");
            if (!string.IsNullOrEmpty(dateStr))
                InvalidateTokensIssuedBefore = dateStr.FromJsv<DateTime>();

            ExpireTokensIn = appSettings.Get("jwt.ExpireTokensIn", ExpireTokensIn);
            ExpireRefreshTokensIn = appSettings.Get("jwt.ExpireRefreshTokensIn", ExpireRefreshTokensIn);

            var intStr = appSettings.GetString("jwt.ExpireTokensInDays");
            if (intStr != null)
                ExpireTokensInDays = int.Parse(intStr);

            string base64Key;

            var i = 1;
            while ((base64Key = appSettings.GetString("jwt.PrivateKeyXml." + i++)) != null)
            {
                FallbackPrivateKeys.Add(base64Key.ToPrivateRSAParameters());

                var publicKey = base64Key.ToPublicRSAParameters();
                FallbackPublicKeys.Add(publicKey);
            }

            i = 1;
            while ((base64Key = appSettings.GetString("jwt.PublicKeyXml." + i++)) != null)
            {
                var publicKey = base64Key.ToPublicRSAParameters();
                FallbackPublicKeys.Add(publicKey);
            }

            i = 1;
            while ((base64Key = appSettings.GetString("jwt.AuthKeyBase64." + i++)) != null)
            {
                var authKey = Convert.FromBase64String(base64Key);
                FallbackAuthKeys.Add(authKey);
            }
        }
    }

    public virtual string GetKeyId(IRequest req)
    {
        if (KeyId != null)
            return KeyId;

        var authKey = GetAuthKey(req);
        if (HmacAlgorithms.ContainsKey(HashAlgorithm) && authKey != null)
            return Convert.ToBase64String(authKey).Substring(0, 3);


        var publicKey = GetPublicKey(req);
        if (RsaSignAlgorithms.ContainsKey(HashAlgorithm) && publicKey != null)
            return Convert.ToBase64String(publicKey.Value.Modulus).Substring(0, 3);

        return null;
    }

    public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null)
    {
        return session.FromToken && session.IsAuthenticated;
    }

    public override async Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token = default)
    {
        // only allow verification of token
        if (!string.IsNullOrEmpty(request.Password) && string.IsNullOrEmpty(request.UserName))
        {
            AuthenticateResponse toAuthResponse(IAuthSession jwtSession)
            {
                var to = jwtSession.ConvertTo<AuthenticateResponse>();
                to.UserId = jwtSession.UserAuthId;
                return to;
            }

            var req = authService.Request;
                
            var bearerToken = request.Password;
            var parts = bearerToken.Split('.');
            if (parts.Length == 3)
            {
                var jwtPayload = GetVerifiedJwtPayload(req, parts);
                if (jwtPayload == null) //not verified
                    throw HttpError.Forbidden(ErrorMessages.TokenInvalid.Localize(req));
                    
                return (toAuthResponse(await CreateSessionFromPayloadAsync(req, jwtPayload)) as object).InTask();
            }
            if (parts.Length == 5) //Encrypted JWE Token
            {
                var jwtPayload = GetVerifiedJwtPayload(req, parts);
                if (jwtPayload == null) //not verified
                    throw HttpError.Forbidden(ErrorMessages.TokenInvalid.Localize(req));

                if (ValidateToken != null)
                {
                    if (!ValidateToken(jwtPayload, req))
                        throw HttpError.Forbidden(ErrorMessages.TokenInvalid.Localize(req));
                }

                return (toAuthResponse(await CreateSessionFromPayloadAsync(req, jwtPayload)) as object).InTask();
            }
        }
   
        throw new NotImplementedException("JWT Authenticate() should not be called directly");
    }

    public async Task PreAuthenticateAsync(IRequest req, IResponse res)
    {
        if (req.OperationName != null && IgnoreForOperationTypes.Contains(req.OperationName))
            return;

        Exception origException = null;

        string refreshToken = null;
        try
        {
            var bearerToken = req.GetJwtToken();
            if (bearerToken != null)
            {
                if (await AuthenticateBearerTokenAsync(req, res, bearerToken))
                    return;
            }
        }
        catch (Exception e)
        {
            refreshToken = req.GetJwtRefreshToken();
            if (refreshToken == null)
                throw;
                
            origException = e;
        }

        if (origException == null)
            refreshToken = req.GetJwtRefreshToken();
                
        if (refreshToken != null)
        {
            if (await AuthenticateRefreshTokenAsync(req, res, refreshToken).ConfigAwait())
                return;
        }

        if (origException != null)
            throw origException;
    }

    protected virtual async Task<bool> AuthenticateBearerTokenAsync(IRequest req, IResponse res, string bearerToken)
    {
        if (bearerToken == null)
            throw new ArgumentNullException(nameof(bearerToken));
            
        try
        {
            var parts = bearerToken.Split('.');
            if (parts.Length == 3)
            {
                if (!req.AllowConnection(RequireSecureConnection))
                    throw HttpError.Forbidden(ErrorMessages.JwtRequiresSecureConnection.Localize(req));

                var jwtPayload = GetVerifiedJwtPayload(req, parts);
                if (jwtPayload == null) //not verified
                    return false;

                if (ValidateToken != null)
                {
                    if (!ValidateToken(jwtPayload, req))
                        throw HttpError.Forbidden(ErrorMessages.TokenInvalid.Localize(req));
                }

                var session = await CreateSessionFromPayloadAsync(req, jwtPayload);
                req.Items[Keywords.Session] = session;
                return true;
            }

            if (parts.Length == 5) //Encrypted JWE Token
            {
                if (!req.AllowConnection(RequireSecureConnection))
                    throw HttpError.Forbidden(ErrorMessages.JwtRequiresSecureConnection.Localize(req));

                var jwtPayload = GetVerifiedJwtPayload(req, parts);
                if (jwtPayload == null) //not verified
                    return false;

                if (ValidateToken != null)
                {
                    if (!ValidateToken(jwtPayload, req))
                        throw HttpError.Forbidden(ErrorMessages.TokenInvalid.Localize(req));
                }

                var session = await CreateSessionFromPayloadAsync(req, jwtPayload);
                req.Items[Keywords.Session] = session;
                return true;
            }
        }
        catch (Exception)
        {
            if (RemoveInvalidTokenCookie && req.Cookies.ContainsKey(Keywords.TokenCookie))
                (res as IHttpResponse)?.Cookies.DeleteCookie(Keywords.TokenCookie);
            throw;
        }
        return false;
    }

    protected virtual async Task<bool> AuthenticateRefreshTokenAsync(IRequest req, IResponse res, string refreshToken)
    {
        if (refreshToken == null)
            throw new ArgumentNullException(nameof(refreshToken));

        try
        {
            var accessToken = await CreateAccessTokenFromRefreshToken(refreshToken, req).ConfigAwait();
            if (accessToken != null)
            {
                if (await AuthenticateBearerTokenAsync(req, res, accessToken))
                {
                    (res as IHttpResponse)?.SetCookie(new Cookie(Keywords.TokenCookie, accessToken, Cookies.RootPath) {
                        HttpOnly = true,
                        Secure = req.IsSecureConnection,
                        Expires = DateTime.UtcNow.Add(ExpireTokensIn),
                    });
                    return true;
                }
            }
        }
        catch (Exception)
        {
            if (RemoveInvalidTokenCookie && req.Cookies.ContainsKey(Keywords.RefreshTokenCookie))
                (res as IHttpResponse)?.Cookies.DeleteCookie(Keywords.RefreshTokenCookie);
            return false;
        }
        return false;
    }

    public virtual Task<string> CreateAccessTokenFromRefreshToken(string refreshToken, IRequest req) =>
        (null as string).AsTaskResult();

    public bool IsJwtValid(string jwt) => GetValidJwtPayload(jwt) != null;
    public bool IsJwtValid(IRequest req, string jwt) => GetValidJwtPayload(req, jwt) != null;
    public bool IsJwtValid(IRequest req) => GetValidJwtPayload(req, req.GetJwtToken()) != null;

    public JsonObject GetValidJwtPayload(string jwt) =>
        GetValidJwtPayload(null, jwt);

    /// <summary>
    /// Return token payload which is both verified and still valid
    /// </summary>
    public virtual JsonObject GetValidJwtPayload(IRequest req) => GetValidJwtPayload(req, req.GetJwtToken());

    /// <summary>
    /// Return token payload which is both verified and still valid
    /// </summary>
    public virtual JsonObject GetValidJwtPayload(IRequest req, string jwt)
    {
        JsonObject verifiedPayload = null;
        try
        {
            verifiedPayload = GetVerifiedJwtPayload(req, jwt.Split('.'));
            if (verifiedPayload == null)
                return null;
            if (ValidateToken != null && !ValidateToken(verifiedPayload, req)) 
                return null;
        }
        catch (Exception)
        {
            return null;
        }
        var invalidError = GetInvalidJwtPayloadError(verifiedPayload);
        return invalidError != null
            ? null
            : verifiedPayload;
    }

    public static Dictionary<string, object> ExtractHeader(string jwt)
    {
        var headerBase64 = jwt.AsSpan().LeftPart('.');
        var headerBytes = headerBase64.ToString().FromBase64UrlSafe();
        var headerJson = MemoryProvider.Instance.FromUtf8Bytes(headerBytes);
        return (Dictionary<string, object>) JSON.parse(headerJson);
    }

    public static Dictionary<string, object> ExtractPayload(string jwt)
    {
        var payloadBase64 = jwt.AsSpan().RightPart('.').LeftPart('.');
        var payloadBytes = payloadBase64.ToString().FromBase64UrlSafe();
        var payloadJson = MemoryProvider.Instance.FromUtf8Bytes(payloadBytes);
        return (Dictionary<string, object>) JSON.parse(payloadJson);
    }

    public static string Dump(string jwt)
    {
        var sb = StringBuilderCache.Allocate();
        var header = ExtractHeader(jwt);
        sb.AppendLine("[JWT Header]").AppendLine();
        Inspect.printDump(header);
        var payload = ExtractPayload(jwt);
        if (payload.TryGetValue("iat", out var iatObj) && iatObj is int iatEpoch)
            payload["iat"] = $"{iatEpoch} ({iatEpoch.FromUnixTime():R})";
        if (payload.TryGetValue("exp", out var expObj) && expObj is int expEpoch)
            payload["exp"] = $"{expEpoch} ({expEpoch.FromUnixTime():R})";
        sb.AppendLine("[JWT Payload]");
        Inspect.printDump(payload);
        return StringBuilderCache.ReturnAndFree(sb);
    }

    /// <summary>
    /// Return token payload which has been verified to be created using the configured encryption key.
    /// Use GetValidJwtPayload() instead if you also want the payload validated.  
    /// </summary>
    public virtual JsonObject GetVerifiedJwtPayload(string jwt) => GetVerifiedJwtPayload(null, jwt.Split('.'));
        
    /// <summary>
    /// Return token payload which has been verified to be created using the configured encryption key.
    /// Use GetValidJwtPayload() instead if you also want the payload validated.  
    /// </summary>
    public virtual JsonObject GetVerifiedJwtPayload(IRequest req, string[] parts)
    {
        if (parts.Length == 3)
        {
            var header = parts[0];
            var payload = parts[1];
            var signatureBytes = parts[2].FromBase64UrlSafe();

            var headerJson = header.FromBase64UrlSafe().FromUtf8Bytes();
            var payloadBytes = payload.FromBase64UrlSafe();

            var headerData = headerJson.FromJson<Dictionary<string, string>>();

            var bytesToSign = string.Concat(header, ".", payload).ToUtf8Bytes();

            var algorithm = headerData["alg"];

            //Potential Security Risk for relying on user-specified algorithm: https://auth0.com/blog/2015/03/31/critical-vulnerabilities-in-json-web-token-libraries/
            if (RequireHashAlgorithm && algorithm != HashAlgorithm)
                throw new NotSupportedException($"Invalid algorithm '{algorithm}', expected '{HashAlgorithm}'");

            if (!VerifyPayload(req, algorithm, bytesToSign, signatureBytes))
                return null;

            var payloadJson = payloadBytes.FromUtf8Bytes();
            var jwtPayload = JsonObject.Parse(payloadJson);
            return jwtPayload;
        }
        if (parts.Length == 5) //Encrypted JWE Token
        {
            return GetVerifiedJwePayload(req, parts);
        }

        throw new ArgumentException(ErrorMessages.TokenInvalid.Localize(req));
    }

    /// <summary>
    /// Return token payload which has been verified to be created using the configured encryption key.
    /// Use GetValidJwtPayload() instead if you also want the payload validated.  
    /// </summary>
    public virtual JsonObject GetVerifiedJwePayload(string jwt) => GetVerifiedJwePayload(null, jwt.Split('.')); 
    /// <summary>
    /// Return token payload which has been verified to be created using the configured encryption key.
    /// Use GetValidJwePayload() instead if you also want the payload validated.  
    /// </summary>
    public virtual JsonObject GetVerifiedJwePayload(IRequest req, string[] parts)
    {
        if (!VerifyJwePayload(req, parts, out var iv, out var cipherText, out var cryptKey))
            return null;

        var aes = Aes.Create();
        aes.KeySize = 128;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using (aes)
        using (var decryptor = aes.CreateDecryptor(cryptKey, iv))
        using (var ms = MemoryStreamFactory.GetStream(cipherText))
        using (var cryptStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
        {
            var jwtPayloadBytes = cryptStream.ReadFully();
            return JsonObject.Parse(jwtPayloadBytes.FromUtf8Bytes());
        }
    }

    public virtual bool VerifyJwePayload(IRequest req, string[] parts, out byte[] iv, out byte[] cipherText, out byte[] cryptKey)
    {
        var jweHeaderBase64Url = parts[0];
        var jweEncKeyBase64Url = parts[1];
        var ivBase64Url = parts[2];
        var cipherTextBase64Url = parts[3];
        var tagBase64Url = parts[4];

        var sentTag = tagBase64Url.FromBase64UrlSafe();
        var aadBytes = (jweHeaderBase64Url + "." + jweEncKeyBase64Url).ToUtf8Bytes();
        iv = ivBase64Url.FromBase64UrlSafe();
        cipherText = cipherTextBase64Url.FromBase64UrlSafe();
        var jweEncKey = jweEncKeyBase64Url.FromBase64UrlSafe();

        var privateKey = GetPrivateKey(req);
        if (privateKey == null)
            throw new Exception("PrivateKey required to decrypt JWE Token");
            
        var allPrivateKeys = new List<RSAParameters> {
            privateKey.Value
        };
        allPrivateKeys.AddRange(GetFallbackPrivateKeys(req));

        var authKey = new byte[128 / 8];
        cryptKey = new byte[128 / 8];

        foreach (var key in allPrivateKeys)
        {
            var cryptAuthKeys256 = RsaUtils.Decrypt(jweEncKey, key, UseRsaKeyLength);

            Buffer.BlockCopy(cryptAuthKeys256, 0, authKey, 0, authKey.Length);
            Buffer.BlockCopy(cryptAuthKeys256, authKey.Length, cryptKey, 0, cryptKey.Length);

            using var hmac = new HMACSHA256(authKey);
            using var encryptedStream = MemoryStreamFactory.GetStream();
            using var writer = new BinaryWriter(encryptedStream);
            writer.Write(aadBytes);
            writer.Write(iv);
            writer.Write(cipherText);
            writer.Flush();

            var calcTag = hmac.ComputeHash(encryptedStream.GetBuffer(), 0, (int) encryptedStream.Length);

            if (calcTag.EquivalentTo(sentTag))
                return true;
        }

        iv = null;
        cipherText = null;
        cryptKey = null;
        return false;
    }

    public virtual async Task<IAuthSession> ConvertJwtToSessionAsync(IRequest req, string jwt)
    {
        if (jwt == null)
            throw new ArgumentNullException(nameof(jwt));

        var jwtPayload = GetVerifiedJwtPayload(req, jwt.Split('.'));
        if (jwtPayload == null) //not verified
            return null;

        if (ValidateToken != null)
        {
            if (!ValidateToken(jwtPayload, req))
                return null;
        }

        var session = await CreateSessionFromPayloadAsync(req, jwtPayload);
        return session;
    }

    public virtual async Task<IAuthSession> CreateSessionFromPayloadAsync(IRequest req, JsonObject jwtPayload)
    {
        AssertJwtPayloadIsValid(jwtPayload);

        var sessionId = jwtPayload.GetValue("jid", HostContext.AppHost.CreateSessionId);
        var session = SessionFeature.CreateNewSession(req, sessionId);

        session.AuthProvider = Name;
        session.FromToken = true;
        session.PopulateFromMap(jwtPayload);

        PopulateSessionFilter?.Invoke(session, jwtPayload, req);
        if (PopulateSessionFilterAsync != null)
            await PopulateSessionFilterAsync(session, jwtPayload, req);

        HostContext.AppHost.OnSessionFilter(req, session, sessionId);
        return session;
    }

    public static async Task<IAuthSession> CreateSessionFromJwtAsync(IRequest req)
    {
        var jwtProvider = AuthenticateService.GetRequiredJwtAuthProvider();

        var jwtToken = req.GetJwtToken();
        var session = await jwtProvider.ConvertJwtToSessionAsync(req, jwtToken);

        return session;
    }

    public void AssertJwtPayloadIsValid(JsonObject jwtPayload)
    {
        var errorMessage = GetInvalidJwtPayloadError(jwtPayload);
        if (errorMessage != null)
            throw new TokenException(errorMessage);
    }
        
    public virtual string GetInvalidJwtPayloadError(JsonObject jwtPayload)
    {
        if (jwtPayload == null)
            throw new ArgumentNullException(nameof(jwtPayload));

        var errorMsg = PreValidateJwtPayloadFilter?.Invoke(jwtPayload);
        if (errorMsg != null)
            return errorMsg;

        if (HasExpired(jwtPayload))
            return ErrorMessages.TokenExpired;

        if (HasInvalidNotBefore(jwtPayload))
            return ErrorMessages.TokenInvalidNotBefore;
            
        if (HasInvalidatedId(jwtPayload))
            return ErrorMessages.TokenInvalidated;

        if (InvalidateTokensIssuedBefore != null && 
            HasBeenInvalidated(jwtPayload, ResolveUnixTime(InvalidateTokensIssuedBefore.Value)))
            return ErrorMessages.TokenInvalidated;

        if (HasInvalidAudience(jwtPayload, out var audience))
            return ErrorMessages.TokenInvalidAudienceFmt.LocalizeFmt(audience);

        return null;
    }

    public void AssertRefreshJwtPayloadIsValid(JsonObject jwtPayload)
    {
        var errorMessage = GetInvalidRefreshJwtPayloadError(jwtPayload);
        if (errorMessage != null)
            throw new TokenException(errorMessage);
    }
        
    public virtual string GetInvalidRefreshJwtPayloadError(JsonObject jwtPayload)
    {
        if (jwtPayload == null)
            throw new ArgumentNullException(nameof(jwtPayload));

        var errorMsg = PreValidateJwtPayloadFilter?.Invoke(jwtPayload);
        if (errorMsg != null)
            return errorMsg;

        if (HasExpired(jwtPayload))
            return ErrorMessages.TokenExpired;

        if (HasInvalidNotBefore(jwtPayload))
            return ErrorMessages.TokenInvalidNotBefore;
            
        if (HasInvalidatedId(jwtPayload))
            return ErrorMessages.TokenInvalidated;

        if (InvalidateRefreshTokensIssuedBefore != null && 
            HasBeenInvalidated(jwtPayload, ResolveUnixTime(InvalidateRefreshTokensIssuedBefore.Value)))
            return ErrorMessages.TokenInvalidated;

        if (HasInvalidAudience(jwtPayload, out var audience))
            return ErrorMessages.TokenInvalidAudienceFmt.LocalizeFmt(audience);

        return null;
    }

    public virtual bool HasInvalidatedId(JsonObject jwtPayload)
    {
        if (InvalidateJwtIds.Count > 0 && jwtPayload.TryGetValue("jti", out var jti))
            return InvalidateJwtIds.Contains(jti);
        return false;
    }

    public virtual bool HasExpired(JsonObject jwtPayload)
    {
        var expiresAt = GetUnixTime(jwtPayload, "exp");
        var secondsSinceEpoch = ResolveUnixTime(DateTime.UtcNow);
        var hasExpired = secondsSinceEpoch >= expiresAt;
        return hasExpired;
    }

    public virtual bool HasInvalidNotBefore(JsonObject jwtPayload)
    {
        var notValidBefore = GetUnixTime(jwtPayload, "nbf");
        if (notValidBefore != null)
        {
            var secondsSinceEpoch = ResolveUnixTime(DateTime.UtcNow);
            var notValidYet = notValidBefore > secondsSinceEpoch;
            return notValidYet;
        }
        return false;
    }

    public virtual bool HasBeenInvalidated(JsonObject jwtPayload, long unixTime)
    {
        var issuedAt = GetUnixTime(jwtPayload, "iat");
        if (issuedAt == null || issuedAt < unixTime)
            return true;
        return false;
    }

    public virtual bool HasInvalidAudience(JsonObject jwtPayload, out string audience)
    {
        var jwtAudiences = jwtPayload.TryGetValue("aud", out audience)
            ? audience.FromJson<List<string>>()
            : null;
        if (jwtAudiences?.Count > 0 && Audiences.Count > 0)
        {
            var containsMatchingAudience = jwtAudiences.Any(x => Audiences.Contains(x));
            return !containsMatchingAudience;
        }
        return RequiresAudience;
    }

    public virtual bool VerifyPayload(IRequest req, string algorithm, byte[] bytesToSign, byte[] sentSignatureBytes)
    {
        var isHmac = HmacAlgorithms.ContainsKey(algorithm);
        var isRsa = RsaSignAlgorithms.ContainsKey(algorithm);
        if (!isHmac && !isRsa)
            throw new NotSupportedException("Invalid algorithm: " + algorithm);

        if (isHmac)
        {
            var authKey = GetAuthKey(req);
            if (authKey == null)
                throw new NotSupportedException("AuthKey required to use: " + HashAlgorithm);

            var allAuthKeys = new List<byte[]> { authKey };
            allAuthKeys.AddRange(GetFallbackAuthKeys(req));
            foreach (var key in allAuthKeys)
            {
                var calcSignatureBytes = HmacAlgorithms[algorithm](key, bytesToSign);
                if (calcSignatureBytes.EquivalentTo(sentSignatureBytes))
                    return true;
            }
        }
        else
        {
            var publicKey = GetPublicKey(req);
            if (publicKey == null)
                throw new NotSupportedException("PublicKey required to use: " + HashAlgorithm);

            var allPublicKeys = new List<RSAParameters> { publicKey.Value };
            allPublicKeys.AddRange(GetFallbackPublicKeys(req));
            foreach (var key in allPublicKeys)
            {
                var verified = RsaVerifyAlgorithms[algorithm](key, bytesToSign, sentSignatureBytes);
                if (verified)
                    return true;
            }
        }

        return false;
    }

    public static long? GetUnixTime(Dictionary<string, string> jwtPayload, string key)
    {
        if (jwtPayload.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
        {
            try
            {
                return long.Parse(value);
            }
            catch (Exception)
            {
                throw new TokenException($"Claim '{key}' must be a Unix Timestamp");
            }
        }
        return null;
    }

    public override void Configure(IServiceCollection services, AuthFeature feature)
    {
        base.Configure(services, feature);
        services.RegisterServices(ServiceRoutes);
    }

    public override void Register(IAppHost appHost, AuthFeature feature)
    {
        base.Register(appHost, feature);
        var isHmac = HmacAlgorithms.ContainsKey(HashAlgorithm);
        var isRsa = RsaSignAlgorithms.ContainsKey(HashAlgorithm);
        if (!isHmac && !isRsa)
            throw new NotSupportedException("Invalid algorithm: " + HashAlgorithm);

        if (isHmac && AuthKey == null)
            throw new ArgumentNullException(nameof(AuthKey), @"An AuthKey is Required to use JWT, e.g: new JwtAuthProvider { AuthKey = AesUtils.CreateKey() }");
        if (isRsa && PrivateKey == null && PublicKey == null)
            throw new ArgumentNullException(nameof(PrivateKey), @"PrivateKey is Required to use JWT with " + HashAlgorithm);

        KeyId ??= GetKeyId(null);
             
        feature.AuthResponseDecorator = AuthenticateResponseDecorator;
        feature.RegisterResponseDecorator = RegisterResponseDecorator;
    }

    public object AuthenticateResponseDecorator(AuthFilterContext ctx)
    {
        var req = ctx.Request;

        if (ctx.AuthResponse.BearerToken == null || UseTokenCookie != true)
            return ctx.AuthResponse;

        if (!req.IsInProcessRequest()) // Don't invalidate cookies for In Process requests
        {
            req.RemoveSession(req.GetSessionId());
        }

        var httpResult = ctx.AuthResponse.ToTokenCookiesHttpResult(req,
            Keywords.TokenCookie,
            DateTime.UtcNow.Add(ExpireTokensIn),
            Keywords.RefreshTokenCookie,
            ctx.ReferrerUrl);
        return httpResult;
    }

    public object RegisterResponseDecorator(RegisterFilterContext ctx)
    {
        var req = ctx.Request;
        if (ctx.RegisterResponse.BearerToken == null || UseTokenCookie != true)
            return ctx.RegisterResponse;

        var httpResult = ctx.RegisterResponse.ToTokenCookiesHttpResult(req,
            Keywords.TokenCookie,
            DateTime.UtcNow.Add(ExpireTokensIn),
            Keywords.RefreshTokenCookie,
            ctx.ReferrerUrl);
        return httpResult;
    }
}

public static class JwtUtils
{
    public static HttpResult ToTokenCookiesHttpResult(this IHasBearerToken responseDto, IRequest req,
        string tokenCookie,
        DateTime expireTokenIn,
        string refreshTokenCookie,
        string referrerUrl)
    {
        var httpResult = new HttpResult(responseDto);
        httpResult.AddCookie(req,
            new Cookie(tokenCookie, responseDto.BearerToken, Cookies.RootPath) {
                HttpOnly = true,
                Secure = req.IsSecureConnection,
                Expires = expireTokenIn,
            });
        responseDto.BearerToken = null;

        if (responseDto is IHasRefreshTokenExpiry { RefreshToken: not null, RefreshTokenExpiry: not null } hasRefreshToken 
            && hasRefreshToken.RefreshTokenExpiry > DateTime.UtcNow)
        {
            httpResult.AddCookie(req,
                new Cookie(refreshTokenCookie, hasRefreshToken.RefreshToken, Cookies.RootPath) {
                    HttpOnly = true,
                    Secure = req.IsSecureConnection,
                    Expires = hasRefreshToken.RefreshTokenExpiry.Value,
                });
            hasRefreshToken.RefreshToken = null;
            hasRefreshToken.RefreshTokenExpiry = null;
        }

        NotifyJwtCookiesUsed(httpResult);

        var isHtml = req.ResponseContentType.MatchesContentType(MimeTypes.Html);
        if (isHtml && referrerUrl != null)
        {
            httpResult.StatusCode = HttpStatusCode.Redirect;
            httpResult.Location = referrerUrl;
        }

        return httpResult;
    }

    //Notify HttpClients which can't access HttpOnly cookies (i.e. web) that JWT Token Cookies are being used 
    public static void NotifyJwtCookiesUsed(IHttpResult httpResult)
    {
        var cookies = new List<string>();
        foreach (var cookie in httpResult.Cookies)
        {
            cookies.Add(cookie.Name);
        }

        if (cookies.Count > 0)
            httpResult.Headers.Add(Keywords.XCookies, string.Join(",", cookies));
    }
        
}