using System.Collections.Generic;

namespace ServiceStack.Configuration;

public interface ISettings
{
    /// <summary>
    /// Provides a common interface for Settings providers such as
    /// ConfigurationManager or Azure's RoleEnvironment. The only
    /// requirement is that if the implementation cannot find the
    /// specified key, the return value must be null
    /// </summary>
    /// <param name="key">The key for the setting</param>
    /// <returns>The string value of the specified key, or null if the key
    /// was invalid</returns>
    string Get(string key);

    /// <summary>
    /// Return all keys in this configuration source.
    /// </summary>
    /// <returns></returns>
    List<string> GetAllKeys();
}

public interface ISettingsWriter : ISettings
{
    void Set<T>(string key, T value);
}