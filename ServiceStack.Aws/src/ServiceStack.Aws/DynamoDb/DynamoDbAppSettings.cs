// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Auth;
using ServiceStack.Configuration;

namespace ServiceStack.Aws.DynamoDb
{
    public class ConfigSetting
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }

    public class DynamoDbAppSettings : AppSettingsBase, IRequiresSchema, IClearable
    {
        private DynamoDbSettings DbSettings => (DynamoDbSettings)base.settings;

        public IPocoDynamo Db => DbSettings.Db;

        private readonly DynamoMetadataType metadata;

        public DynamoDbAppSettings(IPocoDynamo db, bool initSchema = false)
            : base(new DynamoDbSettings(db))
        {
            Db.RegisterTable<ConfigSetting>();
            this.metadata = db.GetTableMetadata<ConfigSetting>();

            if (initSchema)
                InitSchema();
        }

        class DynamoDbSettings : ISettingsWriter
        {
            public readonly IPocoDynamo Db;

            public DynamoDbSettings(IPocoDynamo db)
            {
                this.Db = db;
            }

            public string Get(string key)
            {
                var config = Db.GetItem<ConfigSetting>(key);
                return config?.Value;
            }

            public List<string> GetAllKeys()
            {
                return Db.FromScan<ConfigSetting>().ExecColumn(x => x.Id).ToList();
            }

            public void Set<T>(string key, T value)
            {
                var textValue = value is string
                    ? (string)(object)value
                    : value.ToJsv();

                Db.PutItem(new ConfigSetting { Id = key, Value = textValue });
            }

            public bool Exists(string key)
            {
                return Get(key) != null;
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

        public override void Set<T>(string key, T value)
        {
            DbSettings.Set(key, value);
        }

        public void InitSchema()
        {
            Db.InitSchema();
        }

        public void Clear()
        {
            var q = Db.FromScan<ConfigSetting>().ExecColumn(x => x.Id);
            Db.DeleteItems<ConfigSetting>(q);
        }
    }
}