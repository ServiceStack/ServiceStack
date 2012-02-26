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
		private readonly string atRestPath;

		public RegistrationFeature()
		{
			this.atRestPath = "/register";
		}

		public RegistrationFeature(string atRestPath)
		{
			this.atRestPath = atRestPath;
		}

		public void Register(IAppHost appHost)
		{
			appHost.RegisterService<RegistrationService>(atRestPath);
			appHost.RegisterAs<RegistrationValidator, IValidator<Registration>>();
		}
	}
}