using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace ServiceStack.Auth.Tests
{
    [Ignore("Debug Test")]
    [TestFixture]
    public class RazorAppHostTests
    {
        [Test]
        public void Run_for_10Mins()
        {
            using (var appHost = new AppHost())
            {
                appHost.Init();
                appHost.Start("http://localhost:11001/");

                Process.Start("http://localhost:11001/");

                Thread.Sleep(TimeSpan.FromMinutes(10));
            }
        }
    }
}