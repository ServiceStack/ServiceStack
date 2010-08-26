using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedisWebServices.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace RedisWebServices.Host
{
	public partial class _Default : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			Response.Redirect(AppHostBase.Instance.Container.Resolve<AppConfig>().DefaultRedirectPath);
		}
	}
}