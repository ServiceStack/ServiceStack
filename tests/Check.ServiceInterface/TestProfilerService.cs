using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace Check.ServiceInterface
{
    public class DummyTable
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [Route("/testexecproc")]
    public class TestExecProc {}

    public class TestProfilerService : Service
    {
        public IDbConnectionFactory ConnectionFactory { get; set; }

        public object Any(TestExecProc request)
         {
             using (var sqlServer = ConnectionFactory.OpenDbConnection("SqlServer"))
             {
                 var results = sqlServer.SqlList<DummyTable>("EXEC DummyTable @Times", new { Times = 10 });
                 return results;
             }
         }
    }
}