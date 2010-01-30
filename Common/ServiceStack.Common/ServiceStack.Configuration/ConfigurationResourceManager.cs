using System.Collections.Generic;
using ServiceStack.Common.Text;
using ServiceStack.Common.Utils;

namespace ServiceStack.Configuration
{
	public class ConfigurationResourceManager : IResourceManager
	{
		public string GetString(string name)
		{
			return ConfigUtils.GetNullableAppSetting(name);
		}

		public IList<string> GetList(string key)
		{
			return ConfigUtils.GetListFromAppSetting(key);
		}

		public IDictionary<string, string> GetDictionary(string key)
		{
			return ConfigUtils.GetDictionaryFromAppSetting(key);
		}

		public T Get<T>(string name, T defaultValue)
		{
			var stringValue = ConfigUtils.GetNullableAppSetting(name);
			return TypeSerializer.DeserializeFromString<T>(stringValue);
		}
	}
}