#if NETSTANDARD2_0

using System;
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

        private static T Bind<T>(IConfigurationSection config)
        {
            try
            {
                if (config.Value != null)
                    return config.Value.ConvertTo<T>();
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
            var child = Configuration.GetChildren().FirstOrDefault(x => x.Key == key);
            if (child == null)
                throw new ConfigurationErrorsException(string.Format(ErrorMessages.AppsettingNotFound, key));

            return child;
        }

        public Dictionary<string, string> GetAll()
        {
            var to = new Dictionary<string, string>();
            foreach (var child in Configuration.GetChildren())
            {
                if (child.Value == null)
                    continue;

                var key = child.Key;
                to[key] = child.Value;
            }
            return to;
        }

        public List<string> GetAllKeys() => Configuration.GetChildren().Select(child => child.Key).ToList();

        public bool Exists(string key) => Configuration.GetChildren().Any(x => x.Key == key);

        public void Set<T>(string key, T value) => Configuration[key] = TypeSerializer.SerializeToString(value);

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

        public T Get<T>(string name)
        {
            return Bind<T>(Configuration.GetSection(name));
        }

        public T Get<T>(string name, T defaultValue)
        {
            var child = Configuration.GetChildren().FirstOrDefault(x => x.Key == name);
            if (child?.Value == null)
                return defaultValue;

            var to = Bind<T>(child);
            return to;
        }
    }
}
#endif
