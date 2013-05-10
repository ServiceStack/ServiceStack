using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.ServiceInterface.Testing;
using NUnit.Framework;

namespace ServiceStack.ServiceHost.Tests
{
    [TestFixture]
    public class BasicAppHostTests
    {
        [Test]
        public void Can_dispose_without_init()
        {
            BasicAppHost appHost = new BasicAppHost();
            appHost.Dispose();
        }
    }
}
