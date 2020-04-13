using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ServiceStack.WebHost.IntegrationTests
{
	public partial class Default : PageBase
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			var ssTest = base.SessionBag["ss-test"];
			
			SessionBag["test"] = "foo";
		}
	}
}