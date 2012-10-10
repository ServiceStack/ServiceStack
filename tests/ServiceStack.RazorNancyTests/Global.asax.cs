using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Nancy;

namespace ServiceStack.RazorNancyTests
{
	public class TestModel
	{
		public string Name { get; set; }
	}

	public class Module : NancyModule
	{
		public Module()
		{
			Get["/greet/{name}"] = x => "Hello " + x.name;

			Get["/simple/view"] = param => View["SimpleView.cshtml"];

			Get["/simple/model"] = param => {
				var model = new TestModel { Name = "Demis" };
				return View["ViewThatUsesLayoutAndModel.cshtml", model];
			};

			Get["/simple"] = param => View["ViewThatUsesLayout.cshtml"];
		}
	}

	public class Global : System.Web.HttpApplication
	{

		void Application_Start(object sender, EventArgs e)
		{
			// Code that runs on application startup

		}

		void Application_End(object sender, EventArgs e)
		{
			//  Code that runs on application shutdown

		}

		void Application_Error(object sender, EventArgs e)
		{
			// Code that runs when an unhandled error occurs

		}
	}
}
