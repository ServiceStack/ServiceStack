#if NETCORE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.Extensions.Configuration;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack
{
    public class NetCoreAppSettings : IAppSettings
    {
        public IConfiguration Configuration { get; }
        public NetCoreAppSettings(IConfiguration configuration) => Configuration = configuration;

        static object GetValue(IConfigurationSection section)
        {
            if (section == null)
                return null;
            if (section.Value != null)
                return section.Value;
            
            var children = section.GetChildren();
            var first = children.FirstOrDefault();
            if (first == null)
                return null;
            
            if (first.Key == "0")
            {
                var to = children.Select(GetValue).ToList();
                if (to.Count > 0)
                    return to;
            }
            else
            {
                var to = new Dictionary<string, object>();
                foreach (var child in children)
                {
                    to[child.Key] = GetValue(child);
                }
                if (to.Count > 0)
                    return to;
            }
            
            return null;
        }

        private static T Bind<T>(IConfigurationSection config)
        {
            try
            {
                if (config.Value != null)
                    return config.Value.ConvertTo<T>();

                if (typeof(T).HasInterface(typeof(IEnumerable))
                    && !typeof(T).HasInterface(typeof(IDictionary)))
                {
                    var values = config.GetChildren().Map(GetValue);
                    return values.ConvertTo<T>();
                }
            }
            catch (Exception ex)
            {
                var message = $"The {config.Key} setting had an invalid format. " +
                              $"The value \"{config.Value}\" could not be cast to type {typeof(T).FullName}";
                throw new ConfigurationErrorsException(message, ex);
            }

            try
            {
                var to = typeof(T).CreateInstance();
                config.Bind(to);
                return (T)to;
            }
            catch (InvalidOperationException ex)
            {
                throw new ConfigurationErrorsException(ex.Message, ex);
            }
        }
        
        private IConfigurationSection GetRequiredSection(string key)
        {
            var child = GetSection(key);
            if (!child.Exists())
                throw new ConfigurationErrorsException(ErrorMessages.AppSettingNotFoundFmt.LocalizeFmt(key));

            return child;
        }

        private IConfigurationSection GetSection(string key)
        {
            var child = Configuration.GetChildren().FirstOrDefault(x => x.Key == key);
            if (!child.Exists())
                child = Configuration.GetSection(key);
            return child;
        }

        public Dictionary<string, string> GetAll()
        {
            var to = new Dictionary<string, string>();
            foreach (var kvp in Configuration.GetChildren())
            {
                to[kvp.Key] = kvp.Value;
            }
            return to;
        }

        public List<string> GetAllKeys() => Configuration.GetChildren()
            .Map(x => x.Key);

        public bool Exists(string key) => Configuration.GetChildren().Any(x => x.Key == key)
            || Configuration.GetSection(key).Exists();

        public void Set<T>(string key, T value) => Configuration[key] = value is string 
            ? value.ToString()
            : TypeSerializer.SerializeToString(value);

        public string GetString(string name) => Configuration[name];

        public IList<string> GetList(string key)
        {
            var section = GetRequiredSection(key);
            var members = section.GetChildren();
            var to = members.Map(x => x.Value);
            return to;
        }

        public IDictionary<string, string> GetDictionary(string key)
        {
            var to = Bind<Dictionary<string,string>>(GetRequiredSection(key));
            return to;
        }

        public List<KeyValuePair<string, string>> GetKeyValuePairs(string key)
        {
            var section = GetRequiredSection(key);
            return Bind<Dictionary<string,string>>(section).ToList();
        }

        public T Get<T>(string name)
        {
            return Get<T>(name, default);
        }

        public T Get<T>(string name, T defaultValue)
        {
            var child = GetSection(name);
            if (!child.Exists())
                return defaultValue;

            var to = Bind<T>(child);
            return to;
        }
    }
}
#endif
