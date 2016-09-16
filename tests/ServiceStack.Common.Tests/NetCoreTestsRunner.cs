using NUnitLite;
using NUnit.Common;
using System.Reflection;
using ServiceStack;
using ServiceStack.Text;
using System;
using System.Globalization;
using System.Threading;

namespace NUnitLite.Tests
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

    	    CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            JsConfig.InitStatics();
            //JsonServiceClient client = new JsonServiceClient();
            var writer = new ExtendedTextWrapper(Console.Out);
            return new AutoRun(((IReflectableType)typeof(NetCoreTestsRunner)).GetTypeInfo().Assembly).Execute(args, writer, Console.In);
        }
    }
}