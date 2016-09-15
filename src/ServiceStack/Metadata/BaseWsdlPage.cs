#if !NETSTANDARD1_6
using System.Web.UI.WebControls;

namespace ServiceStack.Metadata
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
#endif
