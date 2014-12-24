using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;

namespace Check.ServiceInterface
{
    [Route("/namedconnection")]
    public class NamedConnection
    {
        public string EmailAddresses { get; set; }
    }

    public class NamedConnectionService : Service
    {
        public object Any(NamedConnection request)
        {
            using (var db = DbFactory.OpenDbConnection("SqlServer"))
            {
                using (var cmd = db.SqlProc(
                    "GetUserIdsFromEmailAddresses", new {request.EmailAddresses}))
                {
                    var userIds = cmd.ConvertToList<int>();
                    return userIds;
                }
            }
        }
    }
}