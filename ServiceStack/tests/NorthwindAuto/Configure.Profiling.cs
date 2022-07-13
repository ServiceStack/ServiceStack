using MyApp.ServiceInterface;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Messaging;

[assembly: HostingStartup(typeof(MyApp.ConfigureProfiling))]

namespace MyApp;

public class ConfigureProfiling : IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        builder.ConfigureAppHost(
            host => {
                host.Plugins.AddIfDebug(new RequestLogsFeature {
                    EnableResponseTracking = true,
                    RequestLogFilter = (req, entry) => {
                        entry.Meta = new() {
                            ["RemoteIp"] = req.RemoteIp,
                            ["Referrer"] = req.UrlReferrer?.ToString(),
                            ["Language"] = req.GetHeader(HttpHeaders.AcceptLanguage),
                        };
                    },
                });
                
                host.Plugins.AddIfDebug(new ProfilingFeature {
                    TagLabel = "Tenant",
                    TagResolver = req => req.PathInfo.ToMd5Hash().Substring(0,5),
                    IncludeStackTrace = true,
                    DiagnosticEntryFilter = (entry, evt) => {
                        if (evt is RequestDiagnosticEvent requestEvent)
                        {
                            var req = requestEvent.Request;
                            entry.Meta = new() {
                                ["RemoteIp"] = req.RemoteIp,
                                ["Referrer"] = req.UrlReferrer?.ToString(),
                                ["Language"] = req.GetHeader(HttpHeaders.AcceptLanguage),
                            };
                        }
                    },
                });
                host.Plugins.Add(new ServerEventsFeature());
                
                host.Container.Register<IMessageService>(c => new BackgroundMqService());
                var mqServer = host.Container.Resolve<IMessageService>();

                mqServer.RegisterHandler<ProfileGen>(host.ExecuteMessage);
                mqServer.RegisterHandler<CreateMqBooking>(host.ExecuteMessage);
            },
            afterAppHostInit: host => {
                host.ServiceController.Execute(new ProfileGen());
                
                host.Resolve<IMessageService>().Start();
                // host.ExecuteMessage(Message.Create(new ProfileGen()));
                var messageProducer = host.GetMessageProducer();
                host.PublishMessage(messageProducer, new ProfileGen());
                
                var client = new JsonApiClient("https://chinook.locode.dev");
                var api = client.Api(new QueryAlbums { Take = 5 });
                
            });
    }
}
