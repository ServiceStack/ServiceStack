using ServiceStack.Auth;
using ServiceStack.FluentValidation;

namespace ServiceStack
{
    /// <summary>
    /// Enable the Registration feature and configure the RegistrationService.
    /// </summary>
    public class RegistrationFeature : IPlugin
    {
        public string AtRestPath { get; set; }

        public RegistrationFeature()
        {
            this.AtRestPath = "/register";
        }

        public void Register(IAppHost appHost)
        {
            appHost.RegisterService<RegisterService>(AtRestPath);
            appHost.RegisterAs<RegistrationValidator, IValidator<Register>>();
        }
    }
}