using System.Security.Authentication;

namespace ServiceStack.Client
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