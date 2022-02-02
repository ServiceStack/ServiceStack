// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if !NETCORE
using System.Data;
#endif
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Text;
using System;

namespace ServiceStack.Redis.Tests
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
            Licensing.RegisterLicense(Environment.GetEnvironmentVariable("SERVICESTACK_LICENSE"));
        }

        [Test]
        public void Allows_access_of_21_types()
        {
            Access20Types();
            Access20Types();
        }

        [Test]
        public void Throws_on_access_of_21_types()
        {
            using (var client = new RedisClient(TestConfig.SingleHost))
            {
                Access20Types();
                Access20Types();

                Assert.Throws<LicenseException>(() =>
                    client.As<T21>());
            }
        }

        [Test, Ignore("Takes too long - but works!")]
        public void Allows_access_of_6000_operations()
        {
            using (var client = new RedisClient(TestConfig.SingleHost))
            {
                6000.Times(() => client.Get("any key"));
            }
        }

        [Test, Ignore("Takes too long - but works!")]
        public void Throws_on_access_of_6100_operations()
        {
            using (var client = new RedisClient(TestConfig.SingleHost))
            {
                Assert.Throws<LicenseException>(() =>
                    6100.Times(() => client.Get("any key")));
            }
        }
    }

    [TestFixture]
    public class RegisteredLicenseUsageTests : LicenseUsageTests
    {
        [Test]
        public void Allows_access_of_21_types()
        {
#if NETCORE
            Environment.GetEnvironmentVariable("SERVICESTACK_LICENSE");
#else
            Licensing.RegisterLicense(new AppSettings().GetString("servicestack:license"));
#endif

            using (var client = new RedisClient(TestConfig.SingleHost))
            {
                Access20Types();
                Access20Types();

                client.As<T21>();
            }
        }

        [Test, Ignore("Takes too long - but works!")]
        public void Allows_access_of_6100_operations()
        {
#if NETCORE
	        Environment.GetEnvironmentVariable("SERVICESTACK_LICENSE");
#else
            Licensing.RegisterLicense(new AppSettings().GetString("servicestack:license"));
#endif

            using (var client = new RedisClient(TestConfig.SingleHost))
            {
                6100.Times(() => client.Get("any key"));
            }
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
        protected void Access20Types()
        {
            using (var client = new RedisClient(TestConfig.SingleHost))
            {
                client.As<T01>();
                client.As<T02>();
                client.As<T03>();
                client.As<T04>();
                client.As<T05>();
                client.As<T06>();
                client.As<T07>();
                client.As<T08>();
                client.As<T09>();
                client.As<T10>();
                client.As<T11>();
                client.As<T12>();
                client.As<T13>();
                client.As<T14>();
                client.As<T15>();
                client.As<T16>();
                client.As<T17>();
                client.As<T18>();
                client.As<T19>();
                client.As<T20>();
            }
        }
    }
}