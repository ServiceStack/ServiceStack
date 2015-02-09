using System;
using System.Linq;

using System.Configuration;
using System.Reflection;

namespace ServiceStack.Razor.BuildTask
{
    // Thanks, Daniel Hilgarth (https://stackoverflow.com/users/572644/daniel-hilgarth)
    // https://stackoverflow.com/questions/6150644/change-default-app-config-at-runtime/6151688#6151688
    public abstract class AppConfigScope : IDisposable
    {
        public static AppConfigScope Change(string path)
        {
            return new ChangeAppConfigScope(path);
        }

        public abstract void Dispose();

        private class ChangeAppConfigScope : AppConfigScope
        {
            private readonly string oldConfig =
                AppDomain.CurrentDomain.GetData("APP_CONFIG_FILE").ToString();

            private bool disposedValue;

            public ChangeAppConfigScope(string path)
            {
                AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", path);
                ResetConfigMechanism();
            }

            public override void Dispose()
            {
                if (!disposedValue)
                {
                    AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", oldConfig);
                    ResetConfigMechanism();


                    disposedValue = true;
                }
                GC.SuppressFinalize(this);
            }

            private static void ResetConfigMechanism()
            {
                typeof(ConfigurationManager)
                    .GetField("s_initState", BindingFlags.NonPublic |
                                             BindingFlags.Static)
                    .SetValue(null, 0);

                typeof(ConfigurationManager)
                    .GetField("s_configSystem", BindingFlags.NonPublic |
                                                BindingFlags.Static)
                    .SetValue(null, null);

                typeof(ConfigurationManager)
                    .Assembly.GetTypes()
                    .First(x => x.FullName == "System.Configuration.ClientConfigPaths")
                    .GetField("s_current", BindingFlags.NonPublic |
                                           BindingFlags.Static)
                    .SetValue(null, null);
            }
        }
    }
}
