// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Data;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests
{
    [Ignore("Run manually")]
    [TestFixtureOrmLite]
    public class FreeLicenseUsageTests : LicenseUsageTests
    {
        public FreeLicenseUsageTests(DialectContext context) : base(context) {}

        [SetUp]
        public void SetUp()
        {
            OrmLiteConfig.ClearCache();
            LicenseUtils.RemoveLicense();
            JsConfig.Reset();
        }

        [TearDown]
        public void TearDown()
        {
#if NETCORE
            Licensing.RegisterLicense(Environment.GetEnvironmentVariable("SERVICESTACK_LICENSE"));
#else            
            Licensing.RegisterLicense(new AppSettings().GetString("servicestack:license"));
#endif
        }

        [Ignore(""), Test]
        public void Allows_creation_of_10_tables()
        {
            Create10Tables();
            Create10Tables();
        }

        [Ignore(""), Test]
        public void Throws_on_creation_of_11_tables()
        {
            Create10Tables();
            Create10Tables();

            Assert.Throws<LicenseException>(() => 
                db.DropAndCreateTable<T11>());
        }
    }

    [Ignore("Run manually")]
    [TestFixtureOrmLite]
    public class RegisteredLicenseUsageTests : LicenseUsageTests
    {
        public RegisteredLicenseUsageTests(DialectContext context) : base(context) {}

        [Test]
        public void Allows_creation_of_11_tables()
        {
#if NETCORE
            Licensing.RegisterLicense(Environment.GetEnvironmentVariable("SERVICESTACK_LICENSE"));
#else            
            Licensing.RegisterLicense(new AppSettings().GetString("servicestack:license"));
#endif
            Create10Tables();
            Create10Tables();

            db.DropAndCreateTable<T11>();
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

    public abstract class LicenseUsageTests : OrmLiteProvidersTestBase
    {
        protected LicenseUsageTests(DialectContext context) : base(context) {}

        protected IDbConnection db;

        [OneTimeSetUp]
        public new void TestFixtureSetUp()
        {
            db = base.OpenDbConnection();
        }

        [OneTimeTearDown]
        public new void TestFixtureTearDown()
        {
            db.Dispose();
        }

        protected void Create10Tables()
        {
            db.DropAndCreateTable<T01>();
            db.DropAndCreateTable<T02>();
            db.DropAndCreateTable<T03>();
            db.DropAndCreateTable<T04>();
            db.DropAndCreateTable<T05>();
            db.DropAndCreateTable<T06>();
            db.DropAndCreateTable<T07>();
            db.DropAndCreateTable<T08>();
            db.DropAndCreateTable<T09>();
            db.DropAndCreateTable<T10>();
        }
    }
}