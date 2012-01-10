using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
	/// <summary>
	/// Enable the Registration feature and configure the RegistrationService.
	/// </summary>
	public class RegistrationFeature
	{
		public static void Init(IAppHost appHost)
		{
			RegistrationService.Init(appHost);
		}
	}
}