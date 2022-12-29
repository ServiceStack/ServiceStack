using System.Collections.Generic;
using Amazon;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.Configuration;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class DynamoDbAppSettingsTests : AppSettingsTest
    {
        private DynamoDbAppSettings settings;

        public IPocoDynamo Db => settings.Db;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            settings = new DynamoDbAppSettings(DynamoTestBase.CreatePocoDynamo());
            settings.InitSchema();
        }

        public override AppSettingsBase GetAppSettings()
        {
            var testConfig = (DictionarySettings)base.GetAppSettings();
            settings.Clear();

            foreach (var config in testConfig.GetAll())
            {
                settings.Set(config.Key, config.Value);
            }

            return settings;
        }

        [Test]
        public void Can_access_ConfigSettings_directly()
        {
            GetAppSettings();

            var value = Db.GetItem<ConfigSetting>("RealKey").Value;

            Assert.That(value, Is.EqualTo("This is a real value"));
        }

        [Test]
        public void Can_preload_AppSettings()
        {
            GetAppSettings();

            var allSettings = new Dictionary<string, string>();
            Db.ScanAll<ConfigSetting>().Each(x => allSettings[x.Id] = x.Value);

            var cachedSettings = new DictionarySettings(allSettings);

            Assert.That(cachedSettings.Get("RealKey"), Is.EqualTo("This is a real value"));
        }

        [Test]
        public void GetString_returns_null_On_Nonexistent_Key()
        {
            var appSettings = GetAppSettings();
            var value = appSettings.GetString("GarbageKey");
            Assert.IsNull(value);
        }

        [Test]
        public void GetList_returns_emtpy_list_On_Null_Key()
        {
            var appSettings = GetAppSettings();

            var result = appSettings.GetList("GarbageKey");

            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void Does_GetOrCreate_New_Value()
        {
            var appSettings = (DynamoDbAppSettings)GetAppSettings();

            var i = 0;

            var key = "key";
            var result = appSettings.GetOrCreate(key, () => key + ++i);
            Assert.That(result, Is.EqualTo("key1"));

            result = appSettings.GetOrCreate(key, () => key + ++i);
            Assert.That(result, Is.EqualTo("key1"));
        }
    }
}