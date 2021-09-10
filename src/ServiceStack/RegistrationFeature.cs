using ServiceStack.Auth;
using ServiceStack.FluentValidation;

namespace ServiceStack
{
    /// <summary>
    /// Enable the Registration feature and configure the RegistrationService.
    /// </summary>
    public class RegistrationFeature : IPlugin, Model.IHasStringId
    {
        public string Id { get; set; } = Plugins.Register;
        public string AtRestPath { get; set; }
        
        public ValidateFn ValidateFn 
        {
            get => RegisterService.ValidateFn; 
            set => RegisterService.ValidateFn = value;
        }

        public bool AllowUpdates
        {
            get => RegisterService.AllowUpdates; 
            set => RegisterService.AllowUpdates = value;
        }

        public RegistrationFeature()
        {
            this.AtRestPath = "/register";
        }

        public void Register(IAppHost appHost)
        {
            appHost.RegisterService<RegisterService>(AtRestPath);

            if (!appHost.GetContainer().Exists<IValidator<Register>>())
            {
                appHost.RegisterAs<RegistrationValidator, IValidator<Register>>();
            }
        }
    }
    
    public class GenericRegistrationFeature<T,TReq> : IPlugin, Model.IHasStringId
        where TReq : Register
    {
        // Use same Plugin ID as they should not be used together
        public string Id { get; set; } = Plugins.Register;
        public string AtRestPath { get; set; }
        
        public ValidateFn ValidateFn 
        {
            get => GenericRegisterService<TReq>.ValidateFn; 
            set => GenericRegisterService<TReq>.ValidateFn = value;
        }

        public bool AllowUpdates
        {
            get => GenericRegisterService<TReq>.AllowUpdates; 
            set => GenericRegisterService<TReq>.AllowUpdates = value;
        }

        public GenericRegistrationFeature()
        {
            this.AtRestPath = "/register";
        }

        public void Register(IAppHost appHost)
        {
            appHost.RegisterService<T>(AtRestPath);

            if (!appHost.GetContainer().Exists<IValidator<TReq>>())
            {
                appHost.RegisterAs<GenericRegistrationValidator<TReq>, IValidator<TReq>>();
            }
        }
    }
}