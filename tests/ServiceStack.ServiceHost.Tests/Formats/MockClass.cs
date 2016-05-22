using System;
using System.IO;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Html;

namespace CSharpEval
{
    [TestFixture]
    public class _Expr
     : ServiceStack.ServiceHost.Tests.Formats.TemplateTests.CustomMarkdownViewBase
    {
        public MvcHtmlString EvalExpr_0()
        {
            return null;
        }

        //[Test]
        public void Compare_access()
        {
            var filePath = "~/AppData/TestsResults/Customer.htm".MapProjectPath();
            const int Times = 10000;

            var start = DateTime.Now;
            var count = 0;
            for (var i = 0; i < Times; i++)
            {
                var result = File.ReadAllText(filePath);
                if (result != null) count++;
            }
            var timeTaken = DateTime.Now - start;
            Console.WriteLine("File.ReadAllText: Times {0}: {1}ms", Times, timeTaken.TotalMilliseconds);

            start = DateTime.Now;
            count = 0;
            //var fi = new FileInfo(filePath);
            for (var i = 0; i < Times; i++)
            {
                var result = File.GetLastWriteTime(filePath);
                if (result != default(DateTime)) count++;
            }
            timeTaken = DateTime.Now - start;
            Console.WriteLine("FileInfo.LastWriteTime: Times {0}: {1}ms", Times, timeTaken.TotalMilliseconds);
        }

        [Test]
        public void A()
        {
            var str = "https://github.com/ServiceStack/ServiceStack.Redis/wiki/RedisPubSub";
            var pos = str.IndexOf("/wiki");
            Console.WriteLine(str.Substring(pos));
        }
    }
}
