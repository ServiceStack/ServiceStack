using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public class DigestAuthFunctions
    {
        public string PrivateHashEncode(string TimeStamp, string IPAddress, string PrivateKey)
        {
            var hashing = MD5.Create();
            return ConvertToHexString(hashing.ComputeHash(Encoding.UTF8.GetBytes($"{TimeStamp}:{IPAddress}:{PrivateKey}")));

        }
        public string Base64Encode(string StringToEncode)
        {
            return StringToEncode != null ? Convert.ToBase64String(Encoding.UTF8.GetBytes(StringToEncode)) : null;
        }

        public string Base64Decode(string StringToDecode)
        {
            return StringToDecode != null ? Encoding.UTF8.GetString(Convert.FromBase64String(StringToDecode)) : null;
        }

        public string[] GetNonceParts(string nonce)
        {
            return Base64Decode(nonce).Split(':');
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
            string privateHash = PrivateHashEncode(nonceparts[0], IPAddress, PrivateKey);
            return string.CompareOrdinal(privateHash, nonceparts[1]) == 0;
        }

        public bool StaleNonce(string nonce, int Timeout)
        {
            var nonceparts = GetNonceParts(nonce);
            return TimeStampAsDateTime(nonceparts[0]).AddSeconds(Timeout) < DateTime.UtcNow;
        }

        private DateTime TimeStampAsDateTime(string TimeStamp)
        {
            double nonceTimeStampDouble;
            if (double.TryParse(TimeStamp, NumberStyles.Float, CultureInfo.InvariantCulture, out nonceTimeStampDouble))
                return DateTime.MinValue.AddMilliseconds(nonceTimeStampDouble);

            throw new ArgumentException($"The given nonce time stamp {TimeStamp} was not valid");
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
            string response = $"{Ha1}:{digestHeaders["nonce"]}:{digestHeaders["nc"]}:{digestHeaders["cnonce"]}:{digestHeaders["qop"].ToLower()}:{Ha2}";
            return ConvertToHexString(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(response)));
        }

        public string CreateHa1(Dictionary<string,string> digestHeaders, string password)
        {
            return CreateHa1(digestHeaders["username"],digestHeaders["realm"],password);
        }

        public string CreateHa1(string Username, string Realm, string Password)
        {
            return ConvertToHexString(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes($"{Username}:{Realm}:{Password}")));
        }

        public string CreateHa2(Dictionary<string, string> digestHeaders)
        {
            return ConvertToHexString(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes($"{digestHeaders["method"]}:{digestHeaders["uri"]}")));
        }

        public bool ValidateResponse(Dictionary<string, string> digestInfo, string PrivateKey, int NonceTimeOut, string DigestHA1, string sequence)
        {
            var noncevalid = ValidateNonce(digestInfo["nonce"], digestInfo["userhostaddress"], PrivateKey);
            var noncestale = StaleNonce(digestInfo["nonce"], NonceTimeOut);
            var uservalid = CreateAuthResponse(digestInfo, DigestHA1) == digestInfo["response"];
            var sequencevalid = sequence != digestInfo["nc"];
            return noncevalid && !noncestale && uservalid && sequencevalid;
        }
    }
}
