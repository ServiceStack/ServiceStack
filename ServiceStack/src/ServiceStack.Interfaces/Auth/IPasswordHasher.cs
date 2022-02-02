namespace ServiceStack.Auth
{
    /// <summary>
    /// The Password Hasher provider used to hash users passwords, by default uses the same algorithm used by ASP.NET Identity v3:
    /// PBKDF2 with HMAC-SHA256, 128-bit salt, 256-bit subkey, 10000 iterations.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// The first byte marker used to specify the format used. The default implementation uses the following format:
        /// { 0x01, prf (UInt32), iter count (UInt32), salt length (UInt32), salt, subkey }
        /// </summary>
        byte Version { get; }

        /// <summary>
        /// Returns a boolean indicating whether the <paramref name="providedPassword"/> matches the <paramref name="hashedPassword"/>.
        /// The <paramref name="needsRehash"/> out parameter indicates whether the password should be re-hashed.
        /// </summary>
        /// <param name="hashedPassword">The hash value for a user's stored password.</param>
        /// <param name="providedPassword">The password supplied for comparison.</param>
        /// <remarks>Implementations of this method should be time consistent.</remarks>
        bool VerifyPassword(string hashedPassword, string providedPassword, out bool needsRehash);

        /// <summary>
        /// Returns a hashed representation of the supplied <paramref name="password"/>.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <returns>A hashed representation of the supplied <paramref name="password"/>.</returns>
        string HashPassword(string password);
    }
}