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

    public class OrnLiteAppSettings : AppSettingsBase, IRequiresSchema 
    {
        private OrmLiteSettings DbSettings
        {
            get { return (OrmLiteSettings)base.settings; }
        }

        public IDbConnectionFactory DbFactory
        {
            get { return DbSettings.DbFactory; }
        }

        public OrnLiteAppSettings(IDbConnectionFactory dbFactory)
            : base(new OrmLiteSettings(dbFactory)) {}

        class OrmLiteSettings : ISettings
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
        }

        public override string GetString(string name)
        {
            return base.GetNullableString(name);
        }

        public void Set<T>(string key, T value)
        {
            DbSettings.Set(key, value);
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