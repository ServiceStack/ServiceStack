// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using NUnit.Framework;
using ServiceStack.Configuration;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class FreeLicenseUsageTests : LicenseUsageTests
    {
        [SetUp]
        public void SetUp()
        {
            LicenseUtils.RemoveLicense();
            JsConfig.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            LicenseHelper.RegisterLicense();
        }

        [Test]
        public void Allows_serialization_of_20_types()
        {
            Serialize20();
            Serialize20();
        }

        [Test]
        public void Allows_deserialization_of_20_types()
        {
            Deserialize20();
            Deserialize20();
        }

        [Test]
        public void Allows_mixed_serialization_of_20_types()
        {
            SerializeTop10();
            DeserializeTop10();
            SerializeBottom10();
            DeserializeBottom10();
        }

        [Ignore(""), Test]
        public void Throws_on_serialization_of_21_types()
        {
            Serialize20();
            Serialize20();

            Assert.Throws<LicenseException>(() => new T21().ToJson());
        }

        [Ignore(""),Test]
        public void Throws_on_deserialization_of_21_types()
        {
            Deserialize20();
            Deserialize20();

            Assert.Throws<LicenseException>(() =>
                "{\"Id\":1}".FromJson<T21>());
        }

        [Ignore(""), Test]
        public void Throws_on_mixed_serialization_of_21_types()
        {
            SerializeTop10();
            DeserializeTop10();
            SerializeBottom10();
            DeserializeBottom10();

            Assert.Throws<LicenseException>(() => new T21().ToJson());
        }
    }

    [TestFixture]
    public class RegisteredLicenseUsageTests : LicenseUsageTests
    {
        [Test]
        public void Allows_serialization_of_21_types()
        {
            LicenseHelper.RegisterLicense();

            Serialize20();
            Serialize20();
            Deserialize20();
            Deserialize20();

            new T21().ToJson();
            "{\"Id\":1}".FromJson<T21>();
        }
    }

    public static class LicenseHelper
    {
        public static void RegisterLicense()
        {
            var envKey = System.Environment.GetEnvironmentVariable("SERVICESTACK_LICENSE");
            if (envKey != null)
            {
                Licensing.RegisterLicense(envKey);
            }

#if !NETCORE
            Licensing.RegisterLicense(new AppSettings().GetString("servicestack:license"));
#endif
        }
    }
    
    class T01 { public int Id { get; set; } }
    class T02 { public int Id { get; set; } }
    class T03 { public int Id { get; set; } }
    class T04 { public int Id { get; set; } }
    class T05 { public int Id { get; set; } }
    class T06 { public int Id { get; set; } }
    class T07 { public int Id { get; set; } }
    class T08 { public int Id { get; set; } }
    class T09 { public int Id { get; set; } }
    class T10 { public int Id { get; set; } }
    class T11 { public int Id { get; set; } }
    class T12 { public int Id { get; set; } }
    class T13 { public int Id { get; set; } }
    class T14 { public int Id { get; set; } }
    class T15 { public int Id { get; set; } }
    class T16 { public int Id { get; set; } }
    class T17 { public int Id { get; set; } }
    class T18 { public int Id { get; set; } }
    class T19 { public int Id { get; set; } }
    class T20 { public int Id { get; set; } }
    class T21 { public int Id { get; set; } }

    public class LicenseUsageTests
    {
        protected static void Serialize20()
        {
            SerializeTop10();
            SerializeBottom10();
        }

        protected static void SerializeBottom10()
        {
            new T11().ToJson();
            new T12().ToJson();
            new T13().ToJson();
            new T14().ToJson();
            new T15().ToJson();
            new T16().ToJson();
            new T17().ToJson();
            new T18().ToJson();
            new T19().ToJson();
            new T20().ToJson();
        }

        protected static void SerializeTop10()
        {
            new T01().ToJson();
            new T02().ToJson();
            new T03().ToJson();
            new T04().ToJson();
            new T05().ToJson();
            new T06().ToJson();
            new T07().ToJson();
            new T08().ToJson();
            new T09().ToJson();
            new T10().ToJson();
        }

        protected static void Deserialize20()
        {
            DeserializeTop10();
            DeserializeBottom10();
        }

        protected static void DeserializeBottom10()
        {
            "{\"Id\":1}".FromJson<T11>();
            "{\"Id\":1}".FromJson<T12>();
            "{\"Id\":1}".FromJson<T13>();
            "{\"Id\":1}".FromJson<T14>();
            "{\"Id\":1}".FromJson<T15>();
            "{\"Id\":1}".FromJson<T16>();
            "{\"Id\":1}".FromJson<T17>();
            "{\"Id\":1}".FromJson<T18>();
            "{\"Id\":1}".FromJson<T19>();
            "{\"Id\":1}".FromJson<T20>();
        }

        protected static void DeserializeTop10()
        {
            "{\"Id\":1}".FromJson<T01>();
            "{\"Id\":1}".FromJson<T02>();
            "{\"Id\":1}".FromJson<T03>();
            "{\"Id\":1}".FromJson<T04>();
            "{\"Id\":1}".FromJson<T05>();
            "{\"Id\":1}".FromJson<T06>();
            "{\"Id\":1}".FromJson<T07>();
            "{\"Id\":1}".FromJson<T08>();
            "{\"Id\":1}".FromJson<T09>();
            "{\"Id\":1}".FromJson<T10>();
        }        
    }
}
