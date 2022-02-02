using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Configuration;

namespace ServiceStack.Aws.DynamoDbTests.Shared
{
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