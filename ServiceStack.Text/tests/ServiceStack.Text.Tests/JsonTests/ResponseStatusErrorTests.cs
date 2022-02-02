#if !NETCORE
using System.Globalization;
using System.IO;
using System.Threading;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class ResponseStatusErrorTests
    {
        [Test, Explicit]
        public void Does_parse_ErrorResponse()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("it-IT");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("it-IT");
            

            var json = File.ReadAllText("~/_error.json".MapAbsolutePath());

            var dto = json.FromJson<ErrorResponse>();

            dto.PrintDump();
        }
    }
}
#endif