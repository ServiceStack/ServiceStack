using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;
using ServiceStack.Text;

namespace ServiceStack.Auth;

public class DigestAuthFunctions
{
    public string PrivateHashEncode(string TimeStamp, string IPAddress, string PrivateKey)
    {
        using var hashing = MD5.Create();
        return ConvertToHexString(hashing.ComputeHash(Encoding.UTF8.GetBytes($"{TimeStamp}:{IPAddress}:{PrivateKey}")));
    }

    public string Base64Encode(string StringToEncode)
    {
        return StringToEncode != null ? Convert.ToBase64String(Encoding.UTF8.GetBytes(StringToEncode)) : null;
    }

    public string Base64Decode(string StringToDecode)
    {
        if (StringToDecode == null) return null;
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(StringToDecode));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public string[] GetNonceParts(string nonce)
    {
        return Base64Decode(nonce)?.Split(':') ?? TypeConstants.EmptyStringArray;
    }

    public string GetNonce(string IPAddress, string PrivateKey)
    {
        double dateTimeInMilliSeconds = (DateTime.UtcNow - DateTime.MinValue).TotalMilliseconds;
        string dateTimeInMilliSecondsString = dateTimeInMilliSeconds.ToString(CultureInfo.InvariantCulture);
        string privateHash = PrivateHashEncode(dateTimeInMilliSecondsString, IPAddress, PrivateKey);
        return Base64Encode($"{dateTimeInMilliSecondsString}:{privateHash}");
    }

    public bool ValidateNonce(string nonce, string IPAddress, string PrivateKey)
    { 
        var nonceparts = GetNonceParts(nonce);
        if (nonceparts.Length < 2) return false;
        string privateHash = PrivateHashEncode(nonceparts[0], IPAddress, PrivateKey);
        return string.CompareOrdinal(privateHash, nonceparts[1]) == 0;
    }

    public bool StaleNonce(string nonce, int Timeout)
    {
        var nonceparts = GetNonceParts(nonce);
        if (nonceparts.Length < 1) return true;
        return TimeStampAsDateTime(nonceparts[0]).AddSeconds(Timeout) < DateTime.UtcNow;
    }

    private DateTime TimeStampAsDateTime(string TimeStamp)
    {
        double nonceTimeStampDouble;
        if (double.TryParse(TimeStamp, NumberStyles.Float, CultureInfo.InvariantCulture, out nonceTimeStampDouble))
            return DateTime.MinValue.AddMilliseconds(nonceTimeStampDouble);

        throw new ArgumentException("The given nonce time stamp was not valid");
    }

    public string ConvertToHexString(IEnumerable<byte> hash)
    {
        var hexString = StringBuilderCache.Allocate();
        foreach (byte byteFromHash in hash)
        {
            hexString.Append($"{byteFromHash:x2}");
        }
        return StringBuilderCache.ReturnAndFree(hexString);
    }

    public string CreateAuthResponse(Dictionary<string, string> digestHeaders, string Ha1)
    {
        string Ha2 = CreateHa2(digestHeaders);
        return CreateAuthResponse(digestHeaders, Ha1, Ha2);
    }

    public string CreateAuthResponse(Dictionary<string, string> digestHeaders, string Ha1, string Ha2)
    {
        var nonce = digestHeaders.TryGetValue("nonce", out var n) ? n : "";
        var nc = digestHeaders.TryGetValue("nc", out var c) ? c : "";
        var cnonce = digestHeaders.TryGetValue("cnonce", out var cn) ? cn : "";
        var qop = digestHeaders.TryGetValue("qop", out var q) ? q.ToLower() : "";
        string response = $"{Ha1}:{nonce}:{nc}:{cnonce}:{qop}:{Ha2}";
        using var md5 = MD5.Create();
        return ConvertToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(response)));
    }

    public string CreateHa1(Dictionary<string,string> digestHeaders, string password)
    {
        var username = digestHeaders.TryGetValue("username", out var u) ? u : "";
        var realm = digestHeaders.TryGetValue("realm", out var r) ? r : "";
        return CreateHa1(username, realm, password);
    }

    public string CreateHa1(string Username, string Realm, string Password)
    {
        using var md5 = MD5.Create();
        return ConvertToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes($"{Username}:{Realm}:{Password}")));
    }

    public string CreateHa2(Dictionary<string, string> digestHeaders)
    {
        var method = digestHeaders.TryGetValue("method", out var m) ? m : "";
        var uri = digestHeaders.TryGetValue("uri", out var u) ? u : "";
        using var md5 = MD5.Create();
        return ConvertToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes($"{method}:{uri}")));
    }

    public bool ValidateResponse(Dictionary<string, string> digestInfo, string PrivateKey, int NonceTimeOut, string DigestHA1, string sequence)
    {
        if (string.IsNullOrEmpty(DigestHA1) || digestInfo == null)
            return false;

        if (!digestInfo.TryGetValue("nonce", out var nonce) ||
            !digestInfo.TryGetValue("userhostaddress", out var userHostAddress) ||
            !digestInfo.TryGetValue("response", out var clientResponse) ||
            !digestInfo.TryGetValue("nc", out var nc))
        {
            return false;
        }

        var noncevalid = ValidateNonce(nonce, userHostAddress, PrivateKey);
        var noncestale = StaleNonce(nonce, NonceTimeOut);
        var authResponse = CreateAuthResponse(digestInfo, DigestHA1);
        var uservalid = CryptUtils.FixedTimeEquals(authResponse, clientResponse);
        var sequencevalid = sequence != nc;
        return noncevalid && !noncestale && uservalid && sequencevalid;
    }
}