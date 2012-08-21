using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Text;

namespace RazorRockstars.Console
{
	[TestFixture]
	public class RazorRockstarsTests
	{
        AppHost appHost;
       
        [TestFixtureSetUp]
	    public void TestFixtureSetUp()
	    {
	        appHost = new AppHost();
	        appHost.Init();
            appHost.Start("http://*:1337/");
	    }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

	    [Test]
	    public void RunFor10Mins()
	    {
	        Thread.Sleep(TimeSpan.FromMinutes(10));
	    }

		public static string AcceptContentType = "*/*";
		public void Assert200(string url, params string[] containsItems)
		{
			var text = url.GetStringFromUrl(AcceptContentType, r => {
				if (r.StatusCode != HttpStatusCode.OK)
					Assert.Fail(url + " did not return 200 OK");
			});
			foreach (var item in containsItems)
			{
				if (!text.Contains(item))
				{
					Assert.Fail(item + " was not found in " + url);
				}
			}
		}

		public void Assert200UrlContentType(string url, string contentType)
		{
			var text = url.GetStringFromUrl(AcceptContentType, r => {
				if (r.StatusCode != HttpStatusCode.OK)
					Assert.Fail(url + " did not return 200 OK: " + r.StatusCode);
				if (!r.ContentType.StartsWith(contentType))
					Assert.Fail(url + " did not return contentType " + contentType);
			});
		}

		public static List<string> Hosts = new List<string>{ "http://localhost:1337" };

		static string ViewRockstars = "<!--view:Rockstars.cshtml-->";
		static string ViewRockstars2 = "<!--view:Rockstars2.cshtml-->";
		static string ViewRockstarsMark = "<!--view:RockstarsMark.md-->";
		static string ViewNoModelNoController = "<!--view:NoModelNoController.cshtml-->";
		static string ViewTypedModelNoController = "<!--view:TypedModelNoController.cshtml-->";

		static string Template_Layout = "<!--template:_Layout.cshtml-->";
		static string TemplateSimpleLayout = "<!--template:SimpleLayout.cshtml-->";
		static string TemplateHtmlReport = "<!--template:HtmlReport.cshtml-->";

		[Test]
		public void Can_get_page_with_default_view_and_template()
		{
			Hosts.ForEach(x =>
			  Assert200(x.AppendPath("rockstars"), ViewRockstars, TemplateHtmlReport));
		}

		[Test]
		public void Can_get_page_with_alt_view_and_default_template()
		{
			Hosts.ForEach(x =>
				Assert200(x.AppendPath("rockstars?View=Rockstars2"), ViewRockstars2, TemplateHtmlReport));
		}
		
		[Test]
		public void Can_get_page_with_alt_viewengine_view_and_default_template()
		{
			Hosts.ForEach(x =>
				Assert200(x.AppendPath("rockstars?View=RockstarsMark"), ViewRockstarsMark, TemplateHtmlReport));
		}

		[Test]
		public void Can_get_page_with_default_view_and_alt_template()
		{
			Hosts.ForEach(x =>
				Assert200(x.AppendPath("rockstars?Template=SimpleLayout"), ViewRockstars, TemplateSimpleLayout));
		}
	}
}

