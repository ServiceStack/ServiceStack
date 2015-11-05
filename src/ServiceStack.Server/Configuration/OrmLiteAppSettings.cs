using System;
using System.Collections.Generic;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.Configuration
{
    public class ConfigSetting
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }

    public class OrmLiteAppSettings : AppSettingsBase, IRequiresSchema
    {
        private OrmLiteSettings DbSettings
        {
            get { return (OrmLiteSettings)base.settings; }
        }

        public IDbConnectionFactory DbFactory
        {
            get { return DbSettings.DbFactory; }
        }

        public OrmLiteAppSettings(IDbConnectionFactory dbFactory)
            : base(new OrmLiteSettings(dbFactory)) {}

        class OrmLiteSettings : ISettingsWriter
        {
            public IDbConnectionFactory DbFactory { get; private set; }

            public OrmLiteSettings(IDbConnectionFactory dbFactory)
            {
                DbFactory = dbFactory;
            }

            public string Get(string key)
            {
                using (var db = DbFactory.Open())
                {
                    var config = db.SingleById<ConfigSetting>(key);
                    return config != null ? config.Value : null;
                }
            }

            public List<string> GetAllKeys()
            {
                using (var db = DbFactory.Open())
                {
                    return db.Column<string>(db.From<ConfigSetting>().Select(x => x.Id));
                }
            }

            public Dictionary<string, string> GetAll()
            {
                using (var db = DbFactory.Open())
                {
                    return db.Dictionary<string, string>(
                        db.From<ConfigSetting>().Select(x => new { x.Id, x.Value }));
                }
            }

            public void Set<T>(string key, T value)
            {
                var textValue = value is string
                    ? (string)(object)value
                    : value.ToJsv();

                using (var db = DbFactory.Open())
                {
                    db.Save(new ConfigSetting { Id = key, Value = textValue });
                }
            }

            public bool Exists(string key)
            {
                using (var db = DbFactory.Open())
                {
                    return db.Count<ConfigSetting>(q => q.Id == key) > 0;
                }
            }
        }

        public T GetOrCreate<T>(string key, Func<T> createFn)
        {
            if (!DbSettings.Exists(key))
            {
                var value = createFn();
                DbSettings.Set(key, value);
                return value;
            }

            return base.Get(key, default(T));
        }

        public override string GetString(string name)
        {
            return base.GetNullableString(name);
        }

        public void Set<T>(string key, T value)
        {
            DbSettings.Set(key, value);
        }

        public override Dictionary<string, string> GetAll()
        {
            return DbSettings.GetAll();
        }

        public void InitSchema()
        {
            using (var db = DbSettings.DbFactory.Open())
            {
                db.CreateTableIfNotExists<ConfigSetting>();
            }
        }
    }
}