using System;
using ServiceStack.FluentValidation;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
	/// <summary>
	/// Enable the Registration feature and configure the RegistrationService.
	/// </summary>
	public class RegistrationFeature : IPlugin
	{
		public void Register(IAppHost appHost)
		{
			appHost.RegisterService<RegistrationService>();
			appHost.RegisterAs<RegistrationValidator, IValidator<Registration>>();
		}
	}
}