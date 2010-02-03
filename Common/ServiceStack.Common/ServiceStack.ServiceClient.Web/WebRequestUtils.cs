using System.Security.Authentication;

namespace ServiceStack.ServiceClient.Web
{
	internal static class WebRequestUtils
	{
		internal static AuthenticationException CreateCustomException(string uri, AuthenticationException ex)
		{
			if (uri.StartsWith("https"))
			{
				return new AuthenticationException(
					string.Format("Invalid remote SSL certificate, overide with: \nServicePointManager.ServerCertificateValidationCallback += ((sender, certificate, chain, sslPolicyErrors) => isValidPolicy);"),
					ex);
			}
			return null;
		}
	}
}