using System;
using System.Collections.Generic;
using ServiceStack.Configuration;

namespace ServiceStack.WebHost.Endpoints.Results
{
	public class HtmlResult : IHasOptions
	{
		public HtmlResult()
		{
			this.HttpHeaders = new Dictionary<string, string> { { "ContentType", "text/html" } };
		}

		public string Html { get; set; }

		public Dictionary<string, string> HttpHeaders { get; set; }

		public IDictionary<string, string> Options
		{
			get { return this.HttpHeaders; }
		}
	}
}