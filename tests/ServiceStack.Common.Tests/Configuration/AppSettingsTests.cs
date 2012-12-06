﻿using System.Collections.Generic;
using System.Configuration;
using NUnit.Framework;
using ServiceStack.Configuration;

namespace ServiceStack.Common.Tests
{	
    public class AppSettingsTest
    {
        private class FakeAppSettings : ISettings
        {
            public string Get(string name)
            {
                switch (name)
                {
                    case "NullableKey":
                        return null;
                    case "EmptyKey":
                        return string.Empty;
                    case "RealKey":
                        return "This is a real value";
                    case "ListKey":
                        return "A,B,C,D,E";
                    case "IntKey":
                        return "42";
                    case "BadIntegerKey":
                        return "This is not an integer";
                    case "DictionaryKey":
                        return "A:1,B:2,C:3,D:4,E:5";
                    case "BadDictionaryKey":
                        return "A1,B:";
                    default:
                        return null;
                }
            }
        }

		[Test]
		public void GetNullable_String_Returns_Null()
		{
		    var appSettings = new AppSettingsBase(new FakeAppSettings());
		    var value = appSettings.GetNullableString("NullableKey");

            Assert.That(value, Is.Null);
		}

        [Test]
        public void GetString_Returns_Value()
        {
            var appSettings = new AppSettingsBase(new FakeAppSettings());
            var value = appSettings.GetString("RealKey");

            Assert.That(value, Is.EqualTo("This is a real value"));
        }

        [Test]
        public void Get_Returns_Default_Value_On_Null_Key()
        {
            var appSettings = new AppSettingsBase(new FakeAppSettings());
            var value = appSettings.Get("NullableKey", "default");

            Assert.That(value, Is.EqualTo("default"));
        }

        [Test]
        public void Get_Casts_To_Specified_Type()
        {
            var appSettings = new AppSettingsBase(new FakeAppSettings());
            var value = appSettings.Get<int>("IntKey", 1);

            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void Get_Throws_Exception_On_Bad_Value()
        {
            var appSettings = new AppSettingsBase(new FakeAppSettings());

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
        public void GetString_Throws_Exception_On_Nonexistent_Key()
        {
            var appSettings = new AppSettingsBase(new FakeAppSettings());
            try
            {
                appSettings.GetString("GarbageKey");
                Assert.Fail("GetString did not throw a ConfigurationErrorsException");
            }
            catch (ConfigurationErrorsException ex)
            {
                Assert.That(ex.Message.Contains("GarbageKey"));
            }
        }


        [Test]
        public void GetList_Parses_List_From_Setting()
        {
            var appSettings = new AppSettingsBase(new FakeAppSettings());
            var value = appSettings.GetList("ListKey");

            Assert.That(value, Has.Count.EqualTo(5));
            Assert.That(value, Is.EqualTo(new List<string> {"A", "B", "C", "D", "E"}));
        }

        [Test]
        public void GetList_Throws_Exception_On_Null_Key()
        {
            var appSettings = new AppSettingsBase(new FakeAppSettings());
            try
            {
                appSettings.GetList("GarbageKey");
                Assert.Fail("GetList did not throw a ConfigurationErrorsException");
            }
            catch (ConfigurationErrorsException ex)
            {
                Assert.That(ex.Message.Contains("GarbageKey"));
            }
        }

        [Test]
        public void GetDictionary_Parses_Dictionary_From_Setting()
        {
            var appSettings = new AppSettingsBase(new FakeAppSettings());

            var value = appSettings.GetDictionary("DictionaryKey");

            Assert.That(value, Has.Count.EqualTo(5));
            Assert.That(value.Keys, Is.EqualTo(new List<string> { "A", "B", "C", "D", "E" }));
            Assert.That(value.Values, Is.EqualTo(new List<string> { "1", "2", "3", "4", "5" }));
        }

        [Test]
        public void GetDictionary_Throws_Exception_On_Null_Key()
        {
            var appSettings = new AppSettingsBase(new FakeAppSettings());

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
            var appSettings = new AppSettingsBase(new FakeAppSettings());

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
	}
}
