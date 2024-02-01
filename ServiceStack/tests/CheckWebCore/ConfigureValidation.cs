using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;

namespace CheckWebCore
{
    public class DynamicIsAuthenticated : IReturn<DynamicIsAuthenticated>
    {
        public string Name { get; set; }
    }
    public class DynamicIsAdmin : IReturn<DynamicIsAdmin>
    {
        public string Name { get; set; }
    }
    public class DynamicHasRole : IReturn<DynamicHasRole>
    {
        public string Name { get; set; }
    }
    public class DynamicHasPermissions : IReturn<DynamicHasPermissions>
    {
        public string Name { get; set; }
    }

    public class DynamicValidationServices : Service
    {
        public object Any(DynamicIsAuthenticated request) => request;
        public object Any(DynamicIsAdmin request) => request;
        public object Any(DynamicHasRole request) => request;
        public object Any(DynamicHasPermissions request) => request;
    }

    public class ConfigureValidation : IConfigureServices, IConfigureAppHost
    {
        public void Configure(IServiceCollection services)
        {
            services.AddSingleton<IValidationSource>(c => new MemoryValidationSource());
        }

        public void Configure(IAppHost appHost)
        {
            var validationSource = appHost.Resolve<IValidationSource>();
            validationSource.InitSchema();

            validationSource.SaveValidationRulesAsync([
                new() { Type = nameof(DynamicIsAuthenticated), Validator = nameof(ValidateScripts.IsAuthenticated) },
                new() { Type = nameof(DynamicIsAdmin), Validator = nameof(ValidateScripts.IsAdmin) },
                new() { Type = nameof(DynamicHasRole), Validator = "HasRole('TheRole')" },
                new() { Type = nameof(DynamicHasPermissions), Validator = "HasPermissions(['Perm1','Perm2'])" }
            ]);
        }
    }
}