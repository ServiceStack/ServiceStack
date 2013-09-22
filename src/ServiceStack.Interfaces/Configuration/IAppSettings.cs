using System.Collections.Generic;

namespace ServiceStack.Configuration
{
	public interface IAppSettings
	{
		string GetString(string name);

		IList<string> GetList(string key);

		IDictionary<string, string> GetDictionary(string key);

		T Get<T>(string name, T defaultValue);
	}
}