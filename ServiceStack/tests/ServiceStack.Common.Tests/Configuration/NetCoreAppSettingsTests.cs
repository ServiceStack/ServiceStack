#if !NETFRAMEWORK
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Configuration;
using Microsoft.Extensions.Configuration;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class NetCoreAppSettingsTests
    {
        public class KeyWithSubkey
        {
            public string Subkey { get; set; }
        }

        public static Dictionary<string, string> Settings = new Dictionary<string, string>
        {
            {"A:A1", "A_A1_Value"},
            {"A:A2:Subkey", "A_A2_Subkey_Value"},
            {"B", "B_Value"},
            {"C:List1:0", "C_List1_Value1"},
            {"C:List1:1", "C_List1_Value2"},
            {"D:Dict1:A", "D_Dict1_ValueA"},
            {"D:Dict1:B", "D_Dict1_ValueB"}
        };

        public IAppSettings GetAppSettings()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(Settings);
            var config = configurationBuilder.Build();
            return new NetCoreAppSettings(config);
        }
        
        [Test]
        [TestCaseSource("Settings")]
        public void Can_GetString_use_NestedKey(KeyValuePair<string,string> keyValue)
        {
            var appSettings = GetAppSettings();
            var child2Value = appSettings.GetString(keyValue.Key);
            Assert.That(child2Value, Is.EqualTo(keyValue.Value));
        }

        [Test]
        [TestCaseSource("Settings")]
        public void Can_Get_use_NestedKey(KeyValuePair<string,string> keyValue)
        {
            var appSettings = GetAppSettings();
            var child2Value = appSettings.Get<string>(keyValue.Key);
            Assert.That(child2Value, Is.EqualTo(keyValue.Value));
        }

        [Test]
        [TestCaseSource("Settings")]
        public void Can_GetType_String_with_Default_use_NestedKey(KeyValuePair<string,string> keyValue)
        {
            var appSettings = GetAppSettings();
            var value = appSettings.Get<string>(keyValue.Key, "default");
            Assert.That(value, Is.EqualTo(keyValue.Value));
        }

        [Test]
        public void Can_GetType_Object_use_NestedKey()
        {
            var appSettings = GetAppSettings();
            var value = appSettings.Get<KeyWithSubkey>("A:A2");
            Assert.That(value.Subkey, Is.EqualTo("A_A2_Subkey_Value"));
        }

        [Test]
        public void Can_GetType_Object_with_Default_use_NestedKey()
        {
            var appSettings = GetAppSettings();
            var value = appSettings.Get<KeyWithSubkey>("A:A2", new KeyWithSubkey{ Subkey = "default" });
            Assert.That(value.Subkey, Is.EqualTo("A_A2_Subkey_Value"));
        }

        [Test]
        public void Can_GetAll_see_NestedKeys()
        {
            var appSettings = (GetAppSettings() as NetCoreAppSettings).Configuration;
            var allKeyValues = appSettings.AsEnumerable();
            foreach (var key in Settings.Keys)
            {
                Assert.That(allKeyValues.Any(x => x.Key == key));
            }
        }

        [Test]
        public void Can_GetAllKeys_see_NestedKeys()
        {
            var appSettings = (GetAppSettings() as NetCoreAppSettings).Configuration;
            var allKeyValues = appSettings.AsEnumerable();
            foreach (var key in Settings.Keys)
            {
                Assert.That(allKeyValues.Any(x => x.Key == key));
            }
        }

        [Test]
        [TestCaseSource("Settings")]
        public void Can_Exists_using_NestedKey(KeyValuePair<string,string> keyValue)
        {
            var appSettings = GetAppSettings();
            var keyExists = appSettings.Exists(keyValue.Key);
            Assert.That(keyExists, Is.True);
        }

        [Test]
        public void Can_GetList_using_NestedKey()
        {
            var appSettings = GetAppSettings();
            var listValues = appSettings.GetList("C:List1");
            Assert.That(listValues, Is.Not.Null.And.Not.Empty.And.Count.EqualTo(2));
        }

        [Test]
        public void Can_GetDictionary_using_NestedKey() 
        {
            var appSettings = GetAppSettings();
            var dictValues = appSettings.GetDictionary("D:Dict1");
            Assert.That(dictValues, Is.Not.Null.And.Not.Empty.And.Count.EqualTo(2));
        }

        [Test]
        public void Can_GetKeyValuePairs_using_NestedKey()
        {
            var appSettings = GetAppSettings();
            var dictValues = appSettings.GetKeyValuePairs("D:Dict1");
            Assert.That(dictValues, Is.Not.Null.And.Not.Empty.And.Count.EqualTo(2));
        }
    }
}
#endif
