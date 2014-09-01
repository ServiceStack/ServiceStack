using System.Collections.Generic;

namespace ServiceStack.Configuration
{
	public interface IAppSettings
	{
        bool Exists(string key);
        
        string GetString(string name);

		IList<string> GetList(string key);

		IDictionary<string, string> GetDictionary(string key);

		T Get<T>(string name, T defaultValue);
	}
}