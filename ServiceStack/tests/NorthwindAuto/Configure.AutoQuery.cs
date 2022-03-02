using Microsoft.AspNetCore.Hosting;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Data;
using TalentBlazor.ServiceModel;

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
                        IncludeService = op => !op.ReferencesAny(nameof(Booking),
                            nameof(Player),nameof(GameItem),nameof(Profile),nameof(Level),nameof(PlayerGameItem),
                            nameof(Contact),nameof(Job),nameof(JobApplication),nameof(JobApplicationEvent),nameof(JobApplicationAttachment),
                            nameof(PhoneScreen),nameof(Interview)),
                    },
                });

                // Can use to configure both code-first + generated types
                var dateFormat = new IntlDateTime(DateStyle.Medium).ToFormat();
                var currency = new IntlNumber { Currency = NumberCurrency.USD }.ToFormat();
                var percent = new IntlNumber(NumberStyle.Percent).ToFormat();
                var icons = new Dictionary<string, ImageInfo>
                {
                    [nameof(ApiKey)] = Svg.CreateImage(Svg.Body.Key),
                    [nameof(AppUser)] = Svg.CreateImage(Svg.Body.User),
                    [nameof(CrudEvent)] = Svg.CreateImage(Svg.Body.History),
                    [nameof(UserAuthDetails)] = Svg.CreateImage(Svg.Body.UserDetails),
                    [nameof(UserAuthRole)] = Svg.CreateImage(Svg.Body.UserShield),
                    ["Category"] = Svg.CreateImage("<path fill='currentColor' d='M20 5h-9.586L8.707 3.293A.997.997 0 0 0 8 3H4c-1.103 0-2 .897-2 2v14c0 1.103.897 2 2 2h16c1.103 0 2-.897 2-2V7c0-1.103-.897-2-2-2z'/>"),
                    ["Customer"] = Svg.CreateImage("<path fill='currentColor' d='M19 2H5a2 2 0 0 0-2 2v14c0 1.1.9 2 2 2h4l2.29 2.29c.39.39 1.02.39 1.41 0L15 20h4c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm-7 3.3c1.49 0 2.7 1.21 2.7 2.7s-1.21 2.7-2.7 2.7S9.3 9.49 9.3 8s1.21-2.7 2.7-2.7zM18 16H6v-.9c0-2 4-3.1 6-3.1s6 1.1 6 3.1v.9z'/>"),
                    ["Employee"] = Svg.CreateImage("<path fill='currentColor' d='M19.745 4a2.25 2.25 0 0 1 2.25 2.25v11.505a2.25 2.25 0 0 1-2.25 2.25H4.25A2.25 2.25 0 0 1 2 17.755V6.25A2.25 2.25 0 0 1 4.25 4h15.495Zm0 1.5H4.25a.75.75 0 0 0-.75.75v11.505c0 .414.336.75.75.75l2.749-.001L7 15.75a1.75 1.75 0 0 1 1.606-1.744L8.75 14h6.495a1.75 1.75 0 0 1 1.744 1.607l.006.143l-.001 2.754h2.751a.75.75 0 0 0 .75-.75V6.25a.75.75 0 0 0-.75-.75ZM12 7a3 3 0 1 1 0 6a3 3 0 0 1 0-6Z'/>"),
                    ["EmployeeTerritory"] = Svg.CreateImage("<path fill='currentColor' d='M1 11v10h5v-6h4v6h5V11L8 6z'/><path fill='currentColor' d='M10 3v1.97l7 5V11h2v2h-2v2h2v2h-2v4h6V3H10zm9 6h-2V7h2v2z'/>"),
                    ["Order"] = Svg.CreateImage("<path fill='currentColor' d='M9 20c0 1.1-.9 2-2 2s-1.99-.9-1.99-2S5.9 18 7 18s2 .9 2 2zm8-2c-1.1 0-1.99.9-1.99 2s.89 2 1.99 2s2-.9 2-2s-.9-2-2-2zm.396-5a2 2 0 0 0 1.952-1.566L21 5H7V4a2 2 0 0 0-2-2H3v2h2v11a2 2 0 0 0 2 2h12a2 2 0 0 0-2-2H7v-2h10.396z'/>"),
                    ["OrderDetail"] = Svg.CreateImage("<path fill='currentColor' d='M20 3H4c-1.103 0-2 .897-2 2v14c0 1.103.897 2 2 2h16c1.103 0 2-.897 2-2V5c0-1.103-.897-2-2-2zM4 19V5h16l.002 14H4z'/><path fill='currentColor' d='M6 7h12v2H6zm0 4h12v2H6zm0 4h6v2H6z'/>"),
                    ["Product"] = Svg.CreateImage("<path fill='currentColor' d='m17.078 22.004l-1.758-4.13l-2.007 4.753l-7.52-3.29l.175 3.906l9.437 4.374l10.91-5.365l-.15-4.99l-9.087 4.742zM29.454 6.62L18.52 3.382l-3.005 2.67l-3.09-2.358L1.544 8.2l3.796 3.047l-3.43 5.303l10.88 4.756l2.53-5.998l2.256 5.308l11.393-5.942l-3.105-4.71l3.592-3.345zm-14.177 7.96l-9.06-3.83l9.276-4.102L25.1 9.903l-9.823 4.676z'/>",viewBox:"0 0 32 32"),
                    ["Region"] = Svg.CreateImage("<path fill='currentColor' d='M12 2c3.31 0 6 2.66 6 5.95C18 12.41 12 19 12 19S6 12.41 6 7.95C6 4.66 8.69 2 12 2m0 4a2 2 0 0 0-2 2a2 2 0 0 0 2 2a2 2 0 0 0 2-2a2 2 0 0 0-2-2m8 13c0 2.21-3.58 4-8 4s-8-1.79-8-4c0-1.29 1.22-2.44 3.11-3.17l.64.91C6.67 17.19 6 17.81 6 18.5c0 1.38 2.69 2.5 6 2.5s6-1.12 6-2.5c0-.69-.67-1.31-1.75-1.76l.64-.91C18.78 16.56 20 17.71 20 19Z'/>"),
                    ["Shipper"] = Svg.CreateImage("<path fill='currentColor' d='M4 10.4V4a1 1 0 0 1 1-1h5V1h4v2h5a1 1 0 0 1 1 1v6.4l1.086.326a1 1 0 0 1 .682 1.2l-1.516 6.068A4.992 4.992 0 0 1 16 16a4.992 4.992 0 0 1-4 2a4.992 4.992 0 0 1-4-2a4.992 4.992 0 0 1-4.252 1.994l-1.516-6.068a1 1 0 0 1 .682-1.2L4 10.4zm2-.6L12 8l2.754.826l1.809.543L18 9.8V5H6v4.8zM4 20a5.978 5.978 0 0 0 4-1.528A5.978 5.978 0 0 0 12 20a5.978 5.978 0 0 0 4-1.528A5.978 5.978 0 0 0 20 20h2v2h-2a7.963 7.963 0 0 1-4-1.07A7.963 7.963 0 0 1 12 22a7.963 7.963 0 0 1-4-1.07A7.963 7.963 0 0 1 4 22H2v-2h2z'/>"),
                    ["Supplier"] = Svg.CreateImage("<path fill='currentColor' d='M20 8h-3V4H3c-1.1 0-2 .9-2 2v11h2c0 1.66 1.34 3 3 3s3-1.34 3-3h6c0 1.66 1.34 3 3 3s3-1.34 3-3h2v-5l-3-4zM6 18.5c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5s1.5.67 1.5 1.5s-.67 1.5-1.5 1.5zm13.5-9l1.96 2.5H17V9.5h2.5zm-1.5 9c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5s1.5.67 1.5 1.5s-.67 1.5-1.5 1.5z'/>"),
                    ["Territory"] = Svg.CreateImage("<path fill='currentColor' d='M12 3L2 12h3v8h14v-8h3L12 3m0 4.7c2.1 0 3.8 1.7 3.8 3.8c0 3-3.8 6.5-3.8 6.5s-3.8-3.5-3.8-6.5c0-2.1 1.7-3.8 3.8-3.8m0 2.3a1.5 1.5 0 0 0-1.5 1.5A1.5 1.5 0 0 0 12 13a1.5 1.5 0 0 0 1.5-1.5A1.5 1.5 0 0 0 12 10Z'/>"),
                };
                appHost.ConfigureTypes(type =>
                {
                    if (icons.TryGetValue(type.Name, out var icon))
                        type.Icon = icon;

                    switch (type.Name)
                    {
                        case "Order":
                            type.EachProperty(x => x.Name.EndsWith("Date"), x => x.Format = dateFormat);
                            type.Property("Freight").Format = currency;
                            type.Property("ShipVia").Ref = new() { Model = "Shipper", RefId = "Id", RefLabel = "CompanyName" };
                            break;
                        case "OrderDetail":
                            type.Property("UnitPrice").Format = currency;
                            type.Property("Discount").Format = percent;
                            break;
                        case "EmployeeTerritory":
                            type.Property("TerritoryId").Ref = new() { Model = "Territory", RefId = "Id", RefLabel = "TerritoryDescription" };
                            break;
                    }
                });

                appHost.Resolve<ICrudEvents>().InitSchema();
            });
    }
}