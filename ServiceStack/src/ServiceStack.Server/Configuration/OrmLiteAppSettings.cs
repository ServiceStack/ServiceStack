using System;
using System.Collections.Generic;
using System.Data;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.Configuration;

public class ConfigSetting
{
    public string Id { get; set; }
    public string Value { get; set; }
}

public class OrmLiteAppSettings(IDbConnectionFactory dbFactory)
    : AppSettingsBase(new OrmLiteSettings(dbFactory)), IRequiresSchema
{
    static void ConfigureDb(IDbConnection db) => db.WithTag(nameof(OrmLiteAppSettings));
    private OrmLiteSettings DbSettings => (OrmLiteSettings)base.settings;

    public IDbConnectionFactory DbFactory => DbSettings.DbFactory;

    class OrmLiteSettings(IDbConnectionFactory dbFactory) : ISettingsWriter
    {
        public IDbConnectionFactory DbFactory { get; } = dbFactory;

        public string Get(string key)
        {
            using var db = DbFactory.Open(ConfigureDb);
            var config = db.SingleById<ConfigSetting>(key);
            return config?.Value;
        }

        public List<string> GetAllKeys()
        {
            using var db = DbFactory.Open(ConfigureDb);
            return db.Column<string>(db.From<ConfigSetting>().Select(x => x.Id));
        }

        public Dictionary<string, string> GetAll()
        {
            using var db = DbFactory.Open(ConfigureDb);
            return db.Dictionary<string, string>(
                db.From<ConfigSetting>().Select(x => new { x.Id, x.Value }));
        }

        public void Set<T>(string key, T value)
        {
            var textValue = value is string
                ? (string)(object)value
                : value.ToJsv();

            using var db = DbFactory.Open(ConfigureDb);
            db.Save(new ConfigSetting { Id = key, Value = textValue });
        }

        public bool Exists(string key)
        {
            using var db = DbFactory.Open(ConfigureDb);
            return db.Count<ConfigSetting>(q => q.Id == key) > 0;
        }

        public void Delete(string key)
        {
            using var db = DbFactory.Open(ConfigureDb);
            db.DeleteById<ConfigSetting>(key);
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

    public override string GetString(string name) => base.GetNullableString(name);

    public override void Set<T>(string key, T value) => DbSettings.Set(key, value);

    public override Dictionary<string, string> GetAll() => DbSettings.GetAll();

    public void Delete(string key) => DbSettings.Delete(key);


    public void InitSchema()
    {
        using var db = DbSettings.DbFactory.Open(ConfigureDb);
        db.CreateTableIfNotExists<ConfigSetting>();
    }
}