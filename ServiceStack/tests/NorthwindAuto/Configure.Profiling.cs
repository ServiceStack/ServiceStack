using Chinook.ServiceModel;
using MyApp.ServiceInterface;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.IO;
using ServiceStack.Jobs;
using ServiceStack.Messaging;
using ServiceStack.Web;

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
                    var vfs = new FileSystemVirtualFiles(context.HostingEnvironment.ContentRootPath);
                    services.AddHostedService<RequestLogsHostedService>();
                    services.AddPlugin(new PostmanFeature());
                    services.AddPlugin(new RequestLogsFeature
                    {
                        RequestLogger = new SqliteRequestLogger(),
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
                        TagLabel = "Tenant",
                        TagResolver = req => req.PathInfo.ToMd5Hash().Substring(0, 5),
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
        if (requestLogger is SqliteRequestLogger dbRequestLogger)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                dbRequestLogger.Tick(log);
            }
        }
    }
}