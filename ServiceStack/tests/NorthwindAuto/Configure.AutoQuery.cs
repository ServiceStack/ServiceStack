using Microsoft.AspNetCore.Hosting;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Data;

// In Configure.AppHost
[assembly: HostingStartup(typeof(MyApp.ConfigureAutoQuery))]

namespace MyApp
{
    public class ConfigureAutoQuery : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder) => builder
            .ConfigureServices(services => {
                // Enable Audit History
                services.AddSingleton<ICrudEvents>(c =>
                    new OrmLiteCrudEvents(c.Resolve<IDbConnectionFactory>()));
            })
            .ConfigureAppHost(appHost => {
                // For TodosService
                appHost.Plugins.Add(new AutoQueryDataFeature());

                // For NorthwindAuto + Bookings
                appHost.Plugins.Add(new AutoQueryFeature {
                    MaxLimit = 100,
                    GenerateCrudServices = new GenerateCrudServices {
                        AutoRegister = true,
                        ServiceFilter = (op, req) =>
                        {
                            op.Request.AddAttributeIfNotExists(new TagAttribute("Northwind"));
                            if (op.Request.Name.IndexOf("User", StringComparison.Ordinal) >= 0)
                                op.Request.AddAttributeIfNotExists(new ValidateIsAdminAttribute());
                        },
                        /* Example adding attributes to generated Type
                        TypeFilter = (type, req) =>
                        {
                            switch (type.Name)
                            {
                                case "Order":
                                    type.Properties.Where(x => x.Name.EndsWith("Date")).Each(p => 
                                        p.AddAttribute(new IntlDateTime(DateStyle.Medium)));
                                    type.Properties.First(x => x.Name == "Freight")
                                        .AddAttribute(new IntlNumber { Currency = NumberCurrency.USD });
                                    break;
                                case "OrderDetail":
                                    type.Properties.First(x => x.Name == "UnitPrice")
                                        .AddAttribute(new IntlNumber { Currency = NumberCurrency.USD });
                                    type.Properties.First(x => x.Name == "Discount")
                                        .AddAttribute(new IntlNumber(NumberStyle.Percent));
                                    break;
                            }
                        },
                        */
                        IncludeService = op => !op.ReferencesAny(nameof(Booking)),
                    },
                });

                // Can use to configure both code-first + generated types
                var dateFormat = new IntlDateTime(DateStyle.Medium).ToFormat();
                var currency = new IntlNumber { Currency = NumberCurrency.USD }.ToFormat();
                var percent = new IntlNumber(NumberStyle.Percent).ToFormat();
                appHost.ConfigureTypes(type =>
                {
                    switch (type.Name)
                    {
                        case "Order":
                            type.Properties.Where(x => x.Name.EndsWith("Date")).Each(x => x.Format = dateFormat);
                            type.Properties.First(x => x.Name == "Freight").Format = currency;
                            break;
                        case "OrderDetail":
                            type.Properties.First(x => x.Name == "UnitPrice").Format = currency;
                            type.Properties.First(x => x.Name == "Discount").Format = percent;
                            break;
                    }
                });

                appHost.Resolve<ICrudEvents>().InitSchema();
            });
    }
}