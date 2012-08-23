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
    }
}