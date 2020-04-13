using System.Web;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [Route("/session/test")]
    public class SessionTest
    {
        public string Result { get; set; }
    }
    
    public class SessionTestServices : ServiceStack.Service
    {
        public object Any(SessionTest request)
        {
            SessionBag["ss-test"] = "bar";
            
            var sessions = HttpContext.Current.Session;
            var aspNetReq = base.Request.OriginalRequest as HttpRequestBase;
            var test = aspNetReq.RequestContext.HttpContext.Items["test"];
            return new SessionTest {
                Result = test as string
            };
        }
    }
}