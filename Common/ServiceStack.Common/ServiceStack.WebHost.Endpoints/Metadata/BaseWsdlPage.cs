using System;
using System.Web;
using System.Web.UI.WebControls;

namespace ServiceStack.WebHost.Endpoints.Metadata
{
    public abstract class BaseWsdlPage : System.Web.UI.Page
    {
        public static void DataBind(params Repeater[] repeaters)
        {
            foreach (var repeater in repeaters)
            {
                repeater.DataBind();
            }
        }

        public string GetBaseUri(HttpRequest request)
        {
            var appPath = request.Url.AbsolutePath;
            var endpointsPath = appPath.Substring(0, appPath.LastIndexOf('/'));
            endpointsPath = endpointsPath.Substring(0, endpointsPath.LastIndexOf('/') + 1);
            return request.Url.GetLeftPart(UriPartial.Authority) + endpointsPath;
        }
    }
}