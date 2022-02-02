using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Setup
{
    public class Init
    {
        [Test]
        public void Run_RDBMS_Setup_on_all_databases()
        {
            var dbFactory = TestConfig.InitDbFactory();
            TestConfig.InitDbScripts(dbFactory);
        }
    }
}