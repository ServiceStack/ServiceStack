// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using ServiceStack.Logging;

#if NETCORE
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
#endif

namespace ServiceStack.Auth
{
    public delegate byte[] Pbkdf2DeriveKeyDelegate(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested);

    /// <summary>
    /// Allow utilizing an alternative PBKDF2 implementation.
    /// </summary>
    public static class Pbkdf2Provider
    {
        /// <summary>
        /// The PBKDF2 strategy PasswordHasher implementation that's used for hashing PBKDF2 passwords.
        /// </summary>
        public static Pbkdf2DeriveKeyDelegate DeriveKey { get; set; }
#if NETCORE
            = KeyDerivation.Pbkdf2; // .NET Core uses the most optimal implementation available for Windows
#else
            = new ManagedPbkdf2Provider().DeriveKey; // Slowest managed implementation used by .NET Framework and all non-Windows OS's
#endif
    }

    /// <summary>
    /// The Password Hasher provider used to hash users passwords which uses the same algorithm used by ASP.NET Identity v3:
    /// PBKDF2 with HMAC-SHA256, 128-bit salt, 256-bit subkey, 10000 iterations.
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        //from https://github.com/aspnet/Identity/blob/dev/src/Microsoft.Extensions.Identity.Core/PasswordHasher.cs
        /* =======================
         * HASHED PASSWORD FORMATS
         * =======================
         * 
         * Version 3:
         * PBKDF2 with HMAC-SHA256, 128-bit salt, 256-bit subkey, 10000 iterations.
         * Format: { 0x01, prf (UInt32), iter count (UInt32), salt length (UInt32), salt, subkey }
         * (All UInt32s are stored big-endian.)
         */

        public static ILog Log = LogManager.GetLogger(typeof(PasswordHasher));

        public const int DefaultIterationCount = 10000;

        /// <summary>
        /// Gets the number of iterations used when hashing passwords using PBKDF2. Default is 10,000.
        /// </summary>
        public int IterationCount { get; }

        public PasswordHasher() : this(DefaultIterationCount) {}

        /// <summary>
        /// The number of iterations used when hashing passwords using PBKDF2. Default is 10,000.
        /// </summary>
        public PasswordHasher(int iterationCount)
        {
            if (iterationCount < 1)
                throw new InvalidOperationException("Invalid iterationCount: " + iterationCount);

            this.IterationCount = iterationCount;
        }

        /// <summary>
        /// The Format Version specifier for this PasswordHasher embedded as the first byte in password hashes.
        /// </summary>
        public byte Version => 0x01;

        public bool VerifyPassword(string hashedPassword, string providedPassword, out bool needsRehash)
        {
            needsRehash = false;

            if (hashedPassword == null)
                throw new ArgumentNullException(nameof(hashedPassword));

            if (providedPassword == null)
                throw new ArgumentNullException(nameof(providedPassword));

            byte[] decodedHashedPassword = Convert.FromBase64String(hashedPassword);

            // read the format marker from the hashed password
            if (decodedHashedPassword.Length == 0)
            {
                if (Log.IsDebugEnabled)
                    Log.Debug("hashedPassword is empty");

                return false;
            }

            var formatMarker = decodedHashedPassword[0];
            switch (formatMarker)
            {
                case 0x01:
                    if (VerifyHashedPasswordV3(decodedHashedPassword, providedPassword, out int embeddedIterCount))
                    {
                        // If this hasher was configured with a higher iteration count, change the entry now.
                        if (embeddedIterCount < IterationCount)
                            needsRehash = true;

                        return true;
                    }
                    else
                    {
                        return false;
                    }

                default:

                    if (Log.IsDebugEnabled)
                        Log.Debug($"Unknown Password Format Marker '{formatMarker}'");

                    return false;
            }
        }

        /// <summary>
        /// Returns a hashed representation of the supplied <paramref name="password"/> for the specified user.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <returns>A hashed representation of the supplied <paramref name="password"/> for the specified user.</returns>
        public virtual string HashPassword(string password)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            return Convert.ToBase64String(HashPasswordV3(password, _rng));
        }

        private static readonly RandomNumberGenerator _defaultRng = RandomNumberGenerator.Create(); // secure PRNG
        private readonly RandomNumberGenerator _rng = _defaultRng;

        // Compares two byte arrays for equality. The method is specifically written so that the loop is not optimized.
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static bool ByteArraysEqual(byte[] a, byte[] b)
        {
            if (a == null && b == null)
            {
                return true;
            }
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }
            var areSame = true;
            for (var i = 0; i < a.Length; i++)
            {
                areSame &= (a[i] == b[i]);
            }
            return areSame;
        }

        private byte[] HashPasswordV3(string password, RandomNumberGenerator rng)
        {
            return HashPasswordV3(password, rng,
                prf: KeyDerivationPrf.HMACSHA256,
                iterCount: IterationCount,
                saltSize: 128 / 8,
                numBytesRequested: 256 / 8);
        }

        private static byte[] HashPasswordV3(string password, RandomNumberGenerator rng, KeyDerivationPrf prf, int iterCount, int saltSize, int numBytesRequested)
        {
            // Produce a version 3 (see comment above) text hash.
            byte[] salt = new byte[saltSize];
            rng.GetBytes(salt);
            byte[] subkey = KeyDerivation.Pbkdf2(password, salt, prf, iterCount, numBytesRequested);

            var outputBytes = new byte[13 + salt.Length + subkey.Length];
            outputBytes[0] = 0x01; // format marker
            WriteNetworkByteOrder(outputBytes, 1, (uint)prf);
            WriteNetworkByteOrder(outputBytes, 5, (uint)iterCount);
            WriteNetworkByteOrder(outputBytes, 9, (uint)saltSize);
            Buffer.BlockCopy(salt, 0, outputBytes, 13, salt.Length);
            Buffer.BlockCopy(subkey, 0, outputBytes, 13 + saltSize, subkey.Length);
            return outputBytes;
        }

        private static uint ReadNetworkByteOrder(byte[] buffer, int offset)
        {
            return ((uint)(buffer[offset + 0]) << 24)
                   | ((uint)(buffer[offset + 1]) << 16)
                   | ((uint)(buffer[offset + 2]) << 8)
                   | ((uint)(buffer[offset + 3]));
        }

        private static bool VerifyHashedPasswordV3(byte[] hashedPassword, string password, out int iterCount)
        {
            iterCount = default(int);

            try
            {
                // Read header information
                KeyDerivationPrf prf = (KeyDerivationPrf)ReadNetworkByteOrder(hashedPassword, 1);
                iterCount = (int)ReadNetworkByteOrder(hashedPassword, 5);
                int saltLength = (int)ReadNetworkByteOrder(hashedPassword, 9);

                // Read the salt: must be >= 128 bits
                if (saltLength < 128 / 8)
                {
                    return false;
                }
                byte[] salt = new byte[saltLength];
                Buffer.BlockCopy(hashedPassword, 13, salt, 0, salt.Length);

                // Read the subkey (the rest of the payload): must be >= 128 bits
                int subkeyLength = hashedPassword.Length - 13 - salt.Length;
                if (subkeyLength < 128 / 8)
                {
                    return false;
                }
                byte[] expectedSubkey = new byte[subkeyLength];
                Buffer.BlockCopy(hashedPassword, 13 + salt.Length, expectedSubkey, 0, expectedSubkey.Length);

                // Hash the incoming password and verify it
                byte[] actualSubkey = KeyDerivation.Pbkdf2(password, salt, prf, iterCount, subkeyLength);
                return ByteArraysEqual(actualSubkey, expectedSubkey);
            }
            catch
            {
                // This should never occur except in the case of a malformed payload, where
                // we might go off the end of the array. Regardless, a malformed payload
                // implies verification failed.
                return false;
            }
        }

        private static void WriteNetworkByteOrder(byte[] buffer, int offset, uint value)
        {
            buffer[offset + 0] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)(value >> 0);
        }
    }

#if NETFX || NET472

    //From: https://github.com/aspnet/DataProtection/
    /// <summary>
    /// Specifies the PRF which should be used for the key derivation algorithm.
    /// </summary>
    public enum KeyDerivationPrf
    {
        /// <summary>
        /// The HMAC algorithm (RFC 2104) using the SHA-1 hash function (FIPS 180-4).
        /// </summary>
        HMACSHA1,

        /// <summary>
        /// The HMAC algorithm (RFC 2104) using the SHA-256 hash function (FIPS 180-4).
        /// </summary>
        HMACSHA256,

        /// <summary>
        /// The HMAC algorithm (RFC 2104) using the SHA-512 hash function (FIPS 180-4).
        /// </summary>
        HMACSHA512,
    }

    /// <summary>
    /// Provides algorithms for performing key derivation.
    /// </summary>
    public static class KeyDerivation
    {
        /// <summary>
        /// Performs key derivation using the PBKDF2 algorithm.
        /// </summary>
        /// <param name="password">The password from which to derive the key.</param>
        /// <param name="salt">The salt to be used during the key derivation process.</param>
        /// <param name="prf">The pseudo-random function to be used in the key derivation process.</param>
        /// <param name="iterationCount">The number of iterations of the pseudo-random function to apply
        /// during the key derivation process.</param>
        /// <param name="numBytesRequested">The desired length (in bytes) of the derived key.</param>
        /// <returns>The derived key.</returns>
        /// <remarks>
        /// The PBKDF2 algorithm is specified in RFC 2898.
        /// </remarks>
        public static byte[] Pbkdf2(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            if (salt == null)
            {
                throw new ArgumentNullException(nameof(salt));
            }

            // parameter checking
            if (prf < KeyDerivationPrf.HMACSHA1 || prf > KeyDerivationPrf.HMACSHA512)
            {
                throw new ArgumentOutOfRangeException(nameof(prf));
            }
            if (iterationCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(iterationCount));
            }
            if (numBytesRequested <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numBytesRequested));
            }

            return Pbkdf2Provider.DeriveKey(password, salt, prf, iterationCount, numBytesRequested);
        }
    }

    /// <summary>
    /// Internal interface used for abstracting away the PBKDF2 implementation since the implementation is OS-specific.
    /// </summary>
    internal interface IPbkdf2Provider
    {
        byte[] DeriveKey(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested);
    }

    
    /// <summary>
    /// A PBKDF2 provider which utilizes the managed hash algorithm classes as PRFs.
    /// This isn't the preferred provider since the implementation is slow, but it is provided as a fallback.
    /// </summary>
    internal sealed class ManagedPbkdf2Provider : IPbkdf2Provider
    {
        public byte[] DeriveKey(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested)
        {
            Debug.Assert(password != null);
            Debug.Assert(salt != null);
            Debug.Assert(iterationCount > 0);
            Debug.Assert(numBytesRequested > 0);

            // PBKDF2 is defined in NIST SP800-132, Sec. 5.3.
            // http://csrc.nist.gov/publications/nistpubs/800-132/nist-sp800-132.pdf

            byte[] retVal = new byte[numBytesRequested];
            int numBytesWritten = 0;
            int numBytesRemaining = numBytesRequested;

            // For each block index, U_0 := Salt || block_index
            byte[] saltWithBlockIndex = new byte[checked(salt.Length + sizeof(uint))];
            Buffer.BlockCopy(salt, 0, saltWithBlockIndex, 0, salt.Length);

            using (var hashAlgorithm = PrfToManagedHmacAlgorithm(prf, password))
            {
                for (uint blockIndex = 1; numBytesRemaining > 0; blockIndex++)
                {
                    // write the block index out as big-endian
                    saltWithBlockIndex[saltWithBlockIndex.Length - 4] = (byte)(blockIndex >> 24);
                    saltWithBlockIndex[saltWithBlockIndex.Length - 3] = (byte)(blockIndex >> 16);
                    saltWithBlockIndex[saltWithBlockIndex.Length - 2] = (byte)(blockIndex >> 8);
                    saltWithBlockIndex[saltWithBlockIndex.Length - 1] = (byte)blockIndex;

                    // U_1 = PRF(U_0) = PRF(Salt || block_index)
                    // T_blockIndex = U_1
                    byte[] U_iter = hashAlgorithm.ComputeHash(saltWithBlockIndex); // this is U_1
                    byte[] T_blockIndex = U_iter;

                    for (int iter = 1; iter < iterationCount; iter++)
                    {
                        U_iter = hashAlgorithm.ComputeHash(U_iter);
                        XorBuffers(src: U_iter, dest: T_blockIndex);
                        // At this point, the 'U_iter' variable actually contains U_{iter+1} (due to indexing differences).
                    }

                    // At this point, we're done iterating on this block, so copy the transformed block into retVal.
                    int numBytesToCopy = Math.Min(numBytesRemaining, T_blockIndex.Length);
                    Buffer.BlockCopy(T_blockIndex, 0, retVal, numBytesWritten, numBytesToCopy);
                    numBytesWritten += numBytesToCopy;
                    numBytesRemaining -= numBytesToCopy;
                }
            }

            // retVal := T_1 || T_2 || ... || T_n, where T_n may be truncated to meet the desired output length
            return retVal;
        }

        private static KeyedHashAlgorithm PrfToManagedHmacAlgorithm(KeyDerivationPrf prf, string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            try
            {
                switch (prf)
                {
                    case KeyDerivationPrf.HMACSHA1:
                        return new HMACSHA1(passwordBytes);
                    case KeyDerivationPrf.HMACSHA256:
                        return new HMACSHA256(passwordBytes);
                    case KeyDerivationPrf.HMACSHA512:
                        return new HMACSHA512(passwordBytes);
                    default:
                        throw CryptoUtil.Fail("Unrecognized PRF.");
                }
            }
            finally
            {
                // The HMAC ctor makes a duplicate of this key; we clear original buffer to limit exposure to the GC.
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }
        }

        private static void XorBuffers(byte[] src, byte[] dest)
        {
            // Note: dest buffer is mutated.
            Debug.Assert(src.Length == dest.Length);
            for (int i = 0; i < src.Length; i++)
            {
                dest[i] ^= src[i];
            }
        }
    }
#endif


    internal static class CryptoUtil
    {
        // This isn't a typical Debug.Fail; an error always occurs, even in retail builds.
        // This method doesn't return, but since the CLR doesn't allow specifying a 'never'
        // return type, we mimic it by specifying our return type as Exception. That way
        // callers can write 'throw Fail(...);' to make the C# compiler happy, as the
        // throw keyword is implicitly of type O.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception Fail(string message)
        {
            Debug.Fail(message);
            throw new CryptographicException("Assertion failed: " + message);
        }

        // Allows callers to write "var x = Method() ?? Fail<T>(message);" as a convenience to guard
        // against a method returning null unexpectedly.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T Fail<T>(string message) where T : class
        {
            throw Fail(message);
        }
    }    
}