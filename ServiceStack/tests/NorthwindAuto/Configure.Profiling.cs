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
                host.Plugins.AddIfDebug(new ProfilingFeature {
                    TagLabel = "Tenant",
                    TagResolver = req => req.PathInfo.ToMd5Hash().Substring(0,5),
                    IncludeStackTrace = true,
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
            });
    }
}
