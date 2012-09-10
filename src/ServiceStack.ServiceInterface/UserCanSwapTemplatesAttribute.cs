using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface
{
    public class UserCanSwapTemplatesAttribute : ResponseFilterAttribute
    {
        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            req.Items["View"] = req.GetParam("View");
            req.Items["Template"] = req.GetParam("Template");
        }
    }
}