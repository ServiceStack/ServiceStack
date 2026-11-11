namespace ServiceStack.Redis;

/// <summary>
/// Optional client capability for atomic key commands. All operations require a writer connection.
/// Client support does not establish server support: GETEX/GETDEL require Redis 6.2+, UNLINK requires 4.0+.
/// Existing native/client interfaces intentionally do not inherit this contract.
/// </summary>
public interface IRedisNativeKeyCommands
{
    /// <summary>Returns the value while updating its expiry atomically, or null for a missing key.</summary>
    byte[] GetEx(string key, RedisGetExOptions options = null);

    /// <summary>Returns and deletes a string value atomically, or returns null for a missing key.</summary>
    byte[] GetDel(string key);

    /// <summary>Removes keys and returns the number removed. Memory is reclaimed in the background.</summary>
    long Unlink(params string[] keys);
}
