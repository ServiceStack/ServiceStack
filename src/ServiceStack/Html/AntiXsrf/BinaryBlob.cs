// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
#if NET_4_0
using System.Diagnostics.Contracts;
#endif
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace ServiceStack.Html.AntiXsrf
{
    // Represents a binary blob (token) that contains random data.
    // Useful for binary data inside a serialized stream.
    [DebuggerDisplay("{DebuggerString}")]
    internal sealed class BinaryBlob : IEquatable<BinaryBlob>
    {
        private static readonly RNGCryptoServiceProvider _prng = new RNGCryptoServiceProvider();

        private readonly byte[] _data;

        // Generates a new token using a specified bit length.
        public BinaryBlob(int bitLength)
            : this(bitLength, GenerateNewToken(bitLength))
        {
        }

        // Generates a token using an existing binary value.
        public BinaryBlob(int bitLength, byte[] data)
        {
            if (bitLength < 32 || bitLength % 8 != 0) {
                throw new ArgumentOutOfRangeException("bitLength");
            }
            if (data == null || data.Length != bitLength / 8) {
                throw new ArgumentOutOfRangeException("data");
            }

            _data = data;
        }

        public int BitLength
        {
            get
            {
                return checked(_data.Length * 8);
            }
        }

        private string DebuggerString
        {
            get
            {
                StringBuilder sb = new StringBuilder("0x", 2 + (_data.Length * 2));
                for (int i = 0; i < _data.Length; i++) {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", _data[i]);
                }
                return sb.ToString();
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BinaryBlob);
        }

        public bool Equals(BinaryBlob other)
        {
            if (other == null) {
                return false;
            }
#if NET_4_0
            Contract.Assert(this._data.Length == other._data.Length);
#endif
            return CryptoUtil.AreByteArraysEqual(this._data, other._data);
        }

        public byte[] GetData()
        {
            return _data;
        }

        public override int GetHashCode()
        {
            // Since data should contain uniformly-distributed entropy, the
            // first 32 bits can serve as the hash code.
#if NET_4_0
            Contract.Assert(_data != null && _data.Length >= (32 / 8));
#endif
            return BitConverter.ToInt32(_data, 0);
        }

        private static byte[] GenerateNewToken(int bitLength)
        {
            byte[] data = new byte[bitLength / 8];
            _prng.GetBytes(data);
            return data;
        }
    }

    internal static class CryptoUtil
    {
        private static readonly Func<SHA256> _sha256Factory = GetSHA256Factory();

        // This method is specially written to take the same amount of time
        // regardless of where 'a' and 'b' differ. Please do not optimize it.
        public static bool AreByteArraysEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) {
                return false;
            }

            bool areEqual = true;
            for (int i = 0; i < a.Length; i++) {
                areEqual &= (a[i] == b[i]);
            }
            return areEqual;
        }

        // Computes a SHA256 hash over all of the input parameters.
        // Each parameter is UTF8 encoded and preceded by a 7-bit encoded
        // integer describing the encoded byte length of the string.
        public static byte[] ComputeSHA256(IList<string> parameters)
        {
            using (MemoryStream ms = new MemoryStream()) {
                using (BinaryWriter bw = new BinaryWriter(ms)) {
                    foreach (string parameter in parameters) {
                        bw.Write(parameter); // also writes the length as a prefix; unambiguous
                    }
                    bw.Flush();

                    using (SHA256 sha256 = _sha256Factory()) {
                        byte[] retVal = sha256.ComputeHash(ms.GetBuffer(), 0, checked((int)ms.Length));
                        return retVal;
                    }
                }
            }
        }

        private static Func<SHA256> GetSHA256Factory()
        {
            // Note: ASP.NET 4.5 always prefers CNG, but the CNG algorithms are not that
            // performant on 4.0 and below. The following list is optimized for speed
            // given our scenarios.
#if NET_4_0
            if (!CryptoConfig.AllowOnlyFipsAlgorithms) {
                // This provider is not FIPS-compliant, so we can't use it if FIPS compliance
                // is mandatory.
                return () => new SHA256Managed();
            }
#endif
            try {
                using (SHA256Cng sha256 = new SHA256Cng()) {
                    return () => new SHA256Cng();
                }
            } catch (PlatformNotSupportedException) {
                // CNG not supported (perhaps because we're not on Windows Vista or above); move on
            }

            // If all else fails, fall back to CAPI.
            return () => new SHA256CryptoServiceProvider();
        }
    }

}
