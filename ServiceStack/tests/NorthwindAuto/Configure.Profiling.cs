using MyApp.ServiceInterface;
using ServiceStack;
using ServiceStack.Messaging;

[assembly: HostingStartup(typeof(MyApp.ConfigureProfiling))]

namespace MyApp;

public class ConfigureProfiling : IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        builder.ConfigureAppHost(
            host => {
                host.Plugins.Add(new ProfilingFeature());
                host.Plugins.Add(new ServerEventsFeature());
                
                host.Container.Register<IMessageService>(c => new BackgroundMqService());
                var mqServer = host.Container.Resolve<IMessageService>();

                mqServer.RegisterHandler<ProfileGen>(host.ExecuteMessage);
            },
            afterAppHostInit: host => {
                host.ServiceController.Execute(new ProfileGen());
                
                host.Resolve<IMessageService>().Start();
                host.ExecuteMessage(Message.Create(new ProfileGen()));
            });
    }
}
