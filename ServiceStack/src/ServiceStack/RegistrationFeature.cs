using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Auth;
using ServiceStack.FluentValidation;
using ServiceStack.Html;
using ServiceStack.Validation;

namespace ServiceStack;

/// <summary>
/// Enable the Registration feature and configure the RegistrationService.
/// </summary>
public class RegistrationFeature : IPlugin, IConfigureServices, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.Register;
    public string AtRestPath { get; set; } = "/register";

    /// <summary>
    /// UI Layout for User Registration
    /// </summary>
    public List<InputInfo> FormLayout { get; set; } =
    [
        Input.For<Register>(x => x.DisplayName, x => x.Help = "Your first and last name"),
        Input.For<Register>(x => x.Email, x => x.Type = Input.Types.Email),
        Input.For<Register>(x => x.Password, x => x.Type = Input.Types.Password),
        Input.For<Register>(x => x.ConfirmPassword, x => x.Type = Input.Types.Password)
    ];
        
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

    public void Configure(IServiceCollection services)
    {
        if (!services.Exists<IValidator<Register>>())
        {
            services.RegisterValidator(c => new RegistrationValidator());
        }
    }

    public void Register(IAppHost appHost)
    {
        appHost.RegisterService<RegisterService>(AtRestPath);
        appHost.ConfigureOperation<Register>(op => op.FormLayout = FormLayout);
    }
}
