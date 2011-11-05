using System.Collections.Generic;

namespace ServiceStack.ServiceInterface.OAuth
{
	public interface IOAuthTokens
	{
		string Provider { get; set; }
		string OAuthToken { get; set; }
		string AccessToken { get; set; }
		string RequestToken { get; set; }
		string RequestTokenSecret { get; set; }
		Dictionary<string, string> Items { get; set; }
	}

	public class OAuthTokens : IOAuthTokens
	{
		public OAuthTokens()
		{
			this.Items = new Dictionary<string, string>();
		}

		public string Provider { get; set; }
		public string OAuthToken { get; set; }
		public string AccessToken { get; set; }
		public string RequestToken { get; set; }
		public string RequestTokenSecret { get; set; }
		public Dictionary<string, string> Items { get; set; }
	}
}