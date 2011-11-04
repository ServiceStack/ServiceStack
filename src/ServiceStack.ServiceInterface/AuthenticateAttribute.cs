using System;

namespace ServiceStack.ServiceInterface
{
	public class AuthenticateAttribute : Attribute
	{
		public string Provider { get; set; }

		public AuthenticateAttribute() {}

		public AuthenticateAttribute(string provider)
		{
			Provider = provider;
		}
	}
}