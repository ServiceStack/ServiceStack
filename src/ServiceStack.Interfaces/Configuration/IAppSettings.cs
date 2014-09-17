using System.Collections.Generic;

namespace ServiceStack.Configuration
{
    public interface IAppSettings
    {
        List<string> GetAllKeys(); 

        bool Exists(string key);

        void Set<T>(string key, T value);

        string GetString(string name);

        IList<string> GetList(string key);

        IDictionary<string, string> GetDictionary(string key);

        T Get<T>(string name, T defaultValue);
    }
}