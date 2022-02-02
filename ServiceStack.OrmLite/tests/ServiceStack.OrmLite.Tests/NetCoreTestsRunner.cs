//NUnitLite isn't recognized in VS2017 - shouldn't need NUnitLite with NUnit 3.5+ https://github.com/nunit/dotnet-test-nunit
#if NUNITLITE
using NUnitLite;
using NUnit.Common;
using System.Reflection;
using ServiceStack;
using ServiceStack.Text;
using System;
using System.Globalization;
using System.Threading;

namespace ServiceStack.OrmLite.Tests
{
    public class NetCoreTestsRunner
    {
        /// <summary>
        /// The main program executes the tests. Output may be routed to
        /// various locations, depending on the arguments passed.
        /// </summary>
        /// <remarks>Run with --help for a full list of arguments supported</remarks>
        /// <param name="args"></param>
        public static int Main(string[] args)
        {
            var licenseKey = Environment.GetEnvironmentVariable("SERVICESTACK_LICENSE");
            if (licenseKey.IsNullOrEmpty())
                throw new ArgumentNullException("SERVICESTACK_LICENSE", "Add Environment variable for SERVICESTACK_LICENSE");

            Licensing.RegisterLicense(licenseKey);
            //"ActivatedLicenseFeatures: ".Print(LicenseUtils.ActivatedLicenseFeatures());

            var sqlServerBuildDb = Environment.GetEnvironmentVariable("SQL_SERVER_BUILD_DB");

            if (!String.IsNullOrEmpty(sqlServerBuildDb))
            {
                TestConfig.SqlServerBuildDb = sqlServerBuildDb;
                TestConfig.DefaultConnection = TestConfig.SqlServerBuildDb;
            }

            var postgreSqlDb = Environment.GetEnvironmentVariable("POSTGRESQL_DB");

            if (!String.IsNullOrEmpty(postgreSqlDb))
            {
                TestConfig.PostgreSqlDb = postgreSqlDb;
            }

            var dialect = Environment.GetEnvironmentVariable("ORMLITE_DIALECT");

            if (!String.IsNullOrEmpty(dialect))
            {
                Dialect defaultDialect;

                if (Enum.TryParse(dialect, out defaultDialect))
                    TestConfig.DefaultDialect = defaultDialect;

            }

            Console.WriteLine($"Dialect: {TestConfig.DefaultDialect}");

    	    CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            JsConfig.InitStatics();

            //JsonServiceClient client = new JsonServiceClient();
            var writer = new ExtendedTextWrapper(Console.Out);
            return new AutoRun(((IReflectableType)typeof(NetCoreTestsRunner)).GetTypeInfo().Assembly).Execute(args, writer, Console.In);
        }
    }
}
#endif
