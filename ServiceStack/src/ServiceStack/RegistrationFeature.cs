using System.Collections.Generic;
using ServiceStack.Auth;
using ServiceStack.FluentValidation;
using ServiceStack.Html;

namespace ServiceStack;

/// <summary>
/// Enable the Registration feature and configure the RegistrationService.
/// </summary>
public class RegistrationFeature : IPlugin, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.Register;
    public string AtRestPath { get; set; }

    /// <summary>
    /// UI Layout for User Registration
    /// </summary>
    public List<InputInfo> FormLayout { get; set; } = new()
    {
        Input.For<Register>(x => x.DisplayName, x => x.Help = "Your first and last name"),
        Input.For<Register>(x => x.Email, x => x.Type = Input.Types.Email),
        Input.For<Register>(x => x.Password, x => x.Type = Input.Types.Password),
        Input.For<Register>(x => x.ConfirmPassword, x => x.Type = Input.Types.Password),
    };
        
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
        appHost.ConfigureOperation<Register>(op => op.FormLayout = FormLayout);

        if (!appHost.GetContainer().Exists<IValidator<Register>>())
        {
            appHost.RegisterAs<RegistrationValidator, IValidator<Register>>();
        }
    }
}