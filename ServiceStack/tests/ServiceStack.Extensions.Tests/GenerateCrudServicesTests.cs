#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.Html;
using ServiceStack.OrmLite;
using ServiceStack.Text;

#if NET8_0_OR_GREATER

namespace ServiceStack.Extensions.Tests;

public class GenerateCrudServicesTests
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : DbContext(options)
    {
    }
    
    class AppHost() : AppHostBase(nameof(IdentityJwtAuthProviderTests), typeof(AutoQueryService).Assembly)
    {
        public override void Configure()
        {
            
        }
    }
    
    private AppHostBase? appHost = null;
    Task? startTask = null;

    public GenerateCrudServicesTests()
    {
        var contentRootPath = "~/../../../../NorthwindAuto".MapServerPath();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = contentRootPath,
            WebRootPath = contentRootPath,
        });
        var services = builder.Services;
        var config = builder.Configuration;

        var dbPath = contentRootPath.CombineWith("northwind.sqlite");

        var connectionString = $"DataSource={dbPath};Cache=Shared";
        var dbFactory = new OrmLiteConnectionFactory(connectionString, SqliteDialect.Provider);
        services.AddSingleton<IDbConnectionFactory>(dbFactory);
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString /*, b => b.MigrationsAssembly(nameof(MyApp))*/));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        ServiceStackHost.InitOptions.ScriptContext.ScriptMethods.AddRange([
            new MyValidators(),
        ]);

        // services.AddPlugin(new AuthFeature(IdentityAuth.For<ApplicationUser>(options =>
        // {
        //     options.SessionFactory = () => new CustomUserSession();
        //     options.CredentialsAuth();
        //     options.JwtAuth(x => { x.ExtendRefreshTokenExpiryAfterUsage = TimeSpan.FromDays(90); });
        // })));

        var autoQuery = new AutoQueryFeature
        {
            MaxLimit = 100,
            GenerateCrudServices = new GenerateCrudServices
            {
                DbFactory = dbFactory,
                AutoRegister = true,
                ServiceFilter = (op, req) =>
                {
                    op.Request.AddAttributeIfNotExists(new TagAttribute("Northwind"));
                    if (op.Request.Name.IndexOf("User", StringComparison.Ordinal) >= 0)
                        op.Request.AddAttributeIfNotExists(new ValidateIsAdminAttribute());
                },
                TypeFilter = (type, req) =>
                {
                    if (type.IsCrudCreateOrUpdate())
                    {
                        type.Properties?.Where(p => p.Name is "Notes" or "Description")
                            .Each(p => p.AddAttribute(new InputAttribute { Type = Input.Types.Textarea }));
                    }

                    if (type.IsCrudCreateOrUpdate("Employee"))
                    {
                        type.Property("PhotoPath")
                            .AddAttribute(new InputAttribute { Type = Input.Types.File })
                            .AddAttribute(new UploadToAttribute("employees"));
                    }

                    /* Example adding attributes to generated Type
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
                    */
                },
            },
        }; 
        services.AddPlugin(autoQuery);

        services.AddServiceStack(typeof(MyServices).Assembly, c =>
        {
            c.AddSwagger(o =>
            {
                //o.AddJwtBearer();
                //o.AddBasicAuth();
            });
        });

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        appHost = new AppHost();
        app.UseServiceStack(appHost, options => { options.MapEndpoints(); });

        startTask = app.StartAsync(TestsConfig.ListeningOn);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown() => appHost?.Dispose();

    [Test]
    public void Can_AutoGen_Northwind_services()
    {
        appHost!.Metadata.GetOperationDtos().Select(x => x.Name).PrintDump();
    }
}

#endif