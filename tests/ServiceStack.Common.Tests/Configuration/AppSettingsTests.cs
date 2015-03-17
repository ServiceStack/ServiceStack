using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class EnvironmentAppSettingsTests
    {
        [Test]
        public void Can_get_environment_variable()
        {
            var env = new EnvironmentVariableSettings();
            var path = env.Get("PATH");
            Assert.That(path, Is.Not.Null);
            path.Print();

            var unknown = env.Get("UNKNOWN");
            Assert.That(unknown, Is.Null);

            var envVars = env.GetAllKeys();
            Assert.That(envVars.Count, Is.GreaterThan(0));

            envVars.PrintDump();
        }
    }

    public class MultiAppSettingsTest : AppSettingsTest
    {
        public override AppSettingsBase GetAppSettings()
        {
            return new MultiAppSettings(
                new DictionarySettings(GetConfigDictionary()),
                new AppSettings());
        }

        public override Dictionary<string, string> GetConfigDictionary()
        {
            var configMap = base.GetConfigDictionary();
            configMap.Remove("NullableKey");
            return configMap;
        }
    }

    public class AppConfigAppSettingsTest : AppSettingsTest
    {
        public override AppSettingsBase GetAppSettings()
        {
            return new AppSettings();
        }

        public override Dictionary<string, string> GetConfigDictionary()
        {
            var configMap = base.GetConfigDictionary();
            configMap.Remove("NullableKey");
            return configMap;
        }
    }

    public class OrmLiteAppSettingsTest : AppSettingsTest
    {
        private OrmLiteAppSettings settings;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            settings = new OrmLiteAppSettings(
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            settings.InitSchema();
        }

        public override AppSettingsBase GetAppSettings()
        {
            var testConfig = (DictionarySettings)base.GetAppSettings();

            using (var db = settings.DbFactory.Open())
            {
                db.DeleteAll<ConfigSetting>();

                foreach (var config in testConfig.GetAll())
                {
                    settings.Set(config.Key, config.Value);
                }
            }

            return settings;
        }

        [Test]
        public void Can_access_ConfigSettings_directly()
        {
            GetAppSettings();
            using (var db = settings.DbFactory.Open())
            {
                var value = db.Scalar<string>(
                    "SELECT Value FROM ConfigSetting WHERE Id = @id", new { id = "RealKey"});

                Assert.That(value, Is.EqualTo("This is a real value"));
            }            
        }

        [Test]
        public void Can_preload_AppSettings()
        {
            GetAppSettings();
            using (var db = settings.DbFactory.Open())
            {
                var allSettings = db.Dictionary<string,string>(
                    db.From<ConfigSetting>().Select(x => new { x.Id, x.Value}));

                var cachedSettings = new DictionarySettings(allSettings);

                Assert.That(cachedSettings.Get("RealKey"), Is.EqualTo("This is a real value"));
            }
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
            var appSettings = (OrmLiteAppSettings)GetAppSettings();

            var i = 0;

            var key = "key";
            var result = appSettings.GetOrCreate(key, () => key + ++i);
            Assert.That(result, Is.EqualTo("key1"));

            result = appSettings.GetOrCreate(key, () => key + ++i);
            Assert.That(result, Is.EqualTo("key1"));
        }
    }

    public class DictionarySettingsTest : AppSettingsTest
    {
        [Test]
        public void GetRequiredString_Throws_Exception_On_Nonexistent_Key()
        {
            var appSettings = GetAppSettings();
            try
            {
                appSettings.GetRequiredString("GarbageKey");
                Assert.Fail("GetString did not throw a ConfigurationErrorsException");
            }
            catch (ConfigurationErrorsException ex)
            {
                Assert.That(ex.Message.Contains("GarbageKey"));
            }
        }

        [Test]
        public void Does_work_with_ParseKeyValueText()
        {
            var textFile = @"
EmptyKey  
RealKey This is a real value
ListKey A,B,C,D,E
IntKey 42
DictionaryKey A:1,B:2,C:3,D:4,E:5
ObjectKey {SomeSetting:Test,SomeOtherSetting:12,FinalSetting:Final}";

            var settings = textFile.ParseKeyValueText();
            var appSettings = new DictionarySettings(settings);

            Assert.That(appSettings.Get("EmptyKey"), Is.EqualTo(""));
            Assert.That(appSettings.Get("RealKey"), Is.EqualTo("This is a real value"));

            Assert.That(appSettings.Get("IntKey", defaultValue:1), Is.EqualTo(42));

            var list = appSettings.GetList("ListKey");
            Assert.That(list, Has.Count.EqualTo(5));
            Assert.That(list, Is.EqualTo(new List<string> { "A", "B", "C", "D", "E" }));

            var map = appSettings.GetDictionary("DictionaryKey");

            Assert.That(map, Has.Count.EqualTo(5));
            Assert.That(map.Keys, Is.EqualTo(new List<string> { "A", "B", "C", "D", "E" }));
            Assert.That(map.Values, Is.EqualTo(new List<string> { "1", "2", "3", "4", "5" }));

            var value = appSettings.Get("ObjectKey", new SimpleAppSettings());
            Assert.That(value, Is.Not.Null);
            Assert.That(value.FinalSetting, Is.EqualTo("Final"));
            Assert.That(value.SomeOtherSetting, Is.EqualTo(12));
            Assert.That(value.SomeSetting, Is.EqualTo("Test"));
        }
    }

    public abstract class AppSettingsTest
    {
        public virtual AppSettingsBase GetAppSettings()
        {
            return new DictionarySettings(GetConfigDictionary())
            {
                ParsingStrategy = null,   
            };
        }

        public virtual Dictionary<string, string> GetConfigDictionary()
        {
            return new Dictionary<string, string>
            {
                {"NullableKey", null},
                {"EmptyKey", string.Empty},
                {"RealKey", "This is a real value"},
                {"ListKey", "A,B,C,D,E"},
                {"IntKey", "42"},
                {"BadIntegerKey", "This is not an integer"},
                {"DictionaryKey", "A:1,B:2,C:3,D:4,E:5"},
                {"BadDictionaryKey", "A1,B:"},
                {"ObjectNoLineFeed", "{SomeSetting:Test,SomeOtherSetting:12,FinalSetting:Final}"},
                {"ObjectWithLineFeed", "{SomeSetting:Test,\r\nSomeOtherSetting:12,\r\nFinalSetting:Final}"},
            };
        }

        [Test]
        public void GetNullable_String_Returns_Null()
        {
            var appSettings = GetAppSettings();
            var value = appSettings.GetNullableString("NullableKey");

            Assert.That(value, Is.Null);
        }

        [Test]
        public void GetString_Returns_Value()
        {
            var appSettings = GetAppSettings();
            var value = appSettings.GetString("RealKey");

            Assert.That(value, Is.EqualTo("This is a real value"));
        }

        [Test]
        public void Get_Returns_Default_Value_On_Null_Key()
        {
            var appSettings = GetAppSettings();
            var value = appSettings.Get("NullableKey", "default");

            Assert.That(value, Is.EqualTo("default"));
        }

        [Test]
        public void Get_Casts_To_Specified_Type()
        {
            var appSettings = GetAppSettings();
            var value = appSettings.Get<int>("IntKey", 1);

            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void Get_Throws_Exception_On_Bad_Value()
        {
            var appSettings = GetAppSettings();

            try
            {
                appSettings.Get<int>("BadIntegerKey", 1);
                Assert.Fail("Get did not throw a ConfigurationErrorsException");
            }
            catch (ConfigurationErrorsException ex)
            {
                Assert.That(ex.Message.Contains("BadIntegerKey"));
            }
        }

        [Test]
        public void GetList_Parses_List_From_Setting()
        {
            var appSettings = GetAppSettings();
            var value = appSettings.GetList("ListKey");

            Assert.That(value, Has.Count.EqualTo(5));
            Assert.That(value, Is.EqualTo(new List<string> { "A", "B", "C", "D", "E" }));
        }

        [Test]
        public void GetDictionary_Parses_Dictionary_From_Setting()
        {
            var appSettings = GetAppSettings();
            var value = appSettings.GetDictionary("DictionaryKey");

            Assert.That(value, Has.Count.EqualTo(5));
            Assert.That(value.Keys, Is.EqualTo(new List<string> { "A", "B", "C", "D", "E" }));
            Assert.That(value.Values, Is.EqualTo(new List<string> { "1", "2", "3", "4", "5" }));
        }

        [Test]
        public void GetDictionary_Throws_Exception_On_Null_Key()
        {
            var appSettings = GetAppSettings();

            try
            {
                appSettings.GetDictionary("GarbageKey");
                Assert.Fail("GetDictionary did not throw a ConfigurationErrorsException");
            }
            catch (ConfigurationErrorsException ex)
            {
                Assert.That(ex.Message.Contains("GarbageKey"));
            }
        }

        [Test]
        public void GetDictionary_Throws_Exception_On_Bad_Value()
        {
            var appSettings = GetAppSettings();

            try
            {
                appSettings.GetDictionary("BadDictionaryKey");
                Assert.Fail("GetDictionary did not throw a ConfigurationErrorsException");
            }
            catch (ConfigurationErrorsException ex)
            {
                Assert.That(ex.Message.Contains("BadDictionaryKey"));
            }
        }
        
        [Test]
        public void Get_Returns_ObjectNoLineFeed()
        {
            var appSettings = GetAppSettings();
            appSettings.ParsingStrategy = AppSettingsStrategy.CollapseNewLines;
            var value = appSettings.Get("ObjectNoLineFeed", new SimpleAppSettings());
            Assert.That(value, Is.Not.Null);
            Assert.That(value.FinalSetting, Is.EqualTo("Final"));
            Assert.That(value.SomeOtherSetting, Is.EqualTo(12));
            Assert.That(value.SomeSetting, Is.EqualTo("Test"));

            value = appSettings.Get<SimpleAppSettings>("ObjectNoLineFeed");
            Assert.That(value, Is.Not.Null);
            Assert.That(value.FinalSetting, Is.EqualTo("Final"));
            Assert.That(value.SomeOtherSetting, Is.EqualTo(12));
            Assert.That(value.SomeSetting, Is.EqualTo("Test"));
        }

        [Test]
        public void Get_Returns_ObjectWithLineFeed()
        {
            var appSettings = GetAppSettings();
            appSettings.ParsingStrategy = AppSettingsStrategy.CollapseNewLines;
            var value = appSettings.Get("ObjectWithLineFeed", new SimpleAppSettings());
            Assert.That(value, Is.Not.Null);
            Assert.That(value.FinalSetting, Is.EqualTo("Final"));
            Assert.That(value.SomeOtherSetting, Is.EqualTo(12));
            Assert.That(value.SomeSetting, Is.EqualTo("Test"));

            value = appSettings.Get<SimpleAppSettings>("ObjectWithLineFeed");
            Assert.That(value, Is.Not.Null);
            Assert.That(value.FinalSetting, Is.EqualTo("Final"));
            Assert.That(value.SomeOtherSetting, Is.EqualTo(12));
            Assert.That(value.SomeSetting, Is.EqualTo("Test"));
        }

        [Test]
        public void Can_write_to_AppSettings()
        {
            var appSettings = GetAppSettings();
            var value = appSettings.Get("IntKey", 0);
            Assert.That(value, Is.EqualTo(42));

            appSettings.Set("IntKey", 99);
            value = appSettings.Get("IntKey", 0);
            Assert.That(value, Is.EqualTo(99));
        }

        public class SimpleAppSettings
        {
            public string SomeSetting { get; set; }
            public int SomeOtherSetting { get; set; }
            public string FinalSetting { get; set; }
        }

        [Test]
        public void Can_get_all_keys()
        {
            var appSettings = GetAppSettings();
            var allKeys = appSettings.GetAllKeys();
            allKeys.Remove("servicestack:license");

            Assert.That(allKeys, Is.EquivalentTo(GetConfigDictionary().Keys));
        }

        [Test]
        public void Can_search_all_keys()
        {
            var appSettings = GetAppSettings();
            var badKeys = appSettings.GetAllKeys().Where(x => x.Matches("Bad*"));

            Assert.That(badKeys, Is.EquivalentTo(new[] { "BadIntegerKey", "BadDictionaryKey" }));
        }
    }
}
