using Chinook.ServiceModel;
using MyApp.ServiceInterface;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.IO;
using ServiceStack.Jobs;
using ServiceStack.Messaging;
using ServiceStack.Web;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

[assembly: HostingStartup(typeof(MyApp.ConfigureProfiling))]

namespace MyApp;

public class ConfigureProfiling : IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        builder
            .ConfigureServices((context, services) =>
            {
                if (context.HostingEnvironment.IsDevelopment())
                {
                    services.AddOpenTelemetry()
                        .ConfigureResource(resource => resource
                            .AddService("northwind-auto", serviceVersion: typeof(ConfigureProfiling).Assembly.GetName().Version?.ToString())
                            .AddAttributes(new Dictionary<string, object> {
                                ["deployment.environment.name"] = context.HostingEnvironment.EnvironmentName,
                            }))
                        .WithTracing(tracing => tracing
                            .AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation()
                            .AddSource(ServiceStack.Telemetry.OperationDiagnostics.Name,
                                MessagingDiagnostics.Name, JobsDiagnostics.Name)
                            .AddOtlpExporter())
                        .WithMetrics(metrics => metrics
                            .AddAspNetCoreInstrumentation()
                            .AddMeter(ServiceStack.Telemetry.OperationDiagnostics.Name,
                                MessagingDiagnostics.Name, JobsDiagnostics.Name)
                            .AddOtlpExporter());
                    var vfs = new FileSystemVirtualFiles(context.HostingEnvironment.ContentRootPath);
                    services.AddHostedService<RequestLogsHostedService>();
                    services.AddPlugin(new PostmanFeature());
                    services.AddPlugin(new RequestLogsFeature
                    {
#if PGSQL || MSSQL || MYSQL                        
                        RequestLogger = new DbRequestLogger {
                            NamedConnection = "northwind"
                        },
#else
                        RequestLogger = new SqliteRequestLogger(),
#endif                        
                        // DisableAnalytics = true,
                        // DisableUserAnalytics = true,
                        /*
                        RequestLogger = new CsvRequestLogger(vfs,
                            "requestlogs/{year}-{month}/{year}-{month}-{day}.csv",
                            "requestlogs/{year}-{month}/{year}-{month}-{day}-errors.csv",
                            TimeSpan.FromSeconds(1)
                        ),
                        */

                        EnableResponseTracking = true,
                        EnableRequestBodyTracking = true,
                        EnableErrorTracking = true
                        // RequestLogFilter = (req, entry) => {
                        //     entry.Meta = new() {
                        //         ["RemoteIp"] = req.RemoteIp,
                        //         ["Referrer"] = req.UrlReferrer?.ToString(),
                        //         ["Language"] = req.GetHeader(HttpHeaders.AcceptLanguage),
                        //     };
                        // },
                    });

                    services.AddPlugin(new ProfilingFeature
                    {
                        ExternalTraceUrlTemplate = context.Configuration["OTEL_TRACE_URL_TEMPLATE"],
                        // TagLabel = "Tenant",
                        // TagResolver = req => req.PathInfo.ToMd5Hash().Substring(0, 5),
                        IncludeStackTrace = true,
                        DiagnosticEntryFilter = (entry, evt) =>
                        {
                            if (evt is RequestDiagnosticEvent requestEvent)
                            {
                                var req = requestEvent.Request;
                                entry.Meta = new()
                                {
                                    ["RemoteIp"] = req.RemoteIp,
                                    ["Referrer"] = req.UrlReferrer?.ToString(),
                                    ["Language"] = req.GetHeader(HttpHeaders.AcceptLanguage),
                                };
                            }
                        },
                    });
                }

                services.AddPlugin(new ServerEventsFeature());

                services.AddSingleton<IMessageService, BackgroundMqService>();
            })
            .ConfigureAppHost(
                afterAppHostInit: host =>
                {
                    var mqServer = host.Container.Resolve<IMessageService>();

                    mqServer.RegisterHandler<ProfileGen>(host.ExecuteMessage);
                    mqServer.RegisterHandler<CreateMqBooking>(host.ExecuteMessage);

                    host.Resolve<IMessageService>().Start();
                });
    }
}


public class RequestLogsHostedService(ILogger<RequestLogsHostedService> log, IRequestLogger requestLogger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (requestLogger is IRequireAnalytics dbRequestLogger)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await dbRequestLogger.TickAsync(log, stoppingToken);
            }
        }
    }
}
