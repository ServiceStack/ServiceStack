using MyApp.ServiceInterface;
using MyApp.ServiceModel.Types;
using ServiceStack.FluentValidation.Validators;
using ServiceStack.Script;

[assembly: HostingStartup(typeof(MyApp.ConfigureContacts))]

namespace MyApp;

/// <summary>
/// Custom Validators for usage in Declarative Validation 
/// </summary>
public class MyValidators : ScriptMethods
{
    public IPropertyValidator ValidColor() => new PredicateValidator((i,x,ctx) => x?.ToString().IsValidColor() == true);
}

public class ConfigureContacts : IHostingStartup 
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureAppHost(appHost => {
            AutoMapping.RegisterConverter((Data.Contact from) => from.ConvertTo<Contact>(skipConverters: true));
            appHost.ScriptContext.ScriptMethods.Add(new MyValidators());
        });
}
