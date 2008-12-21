using ServiceStack.Messaging.UseCases.Services.Basic;

namespace ServiceStack.Messaging.UseCases.Services.Messaging
{
    public class GreetTextService : IService
    {
        public string Execute(IServiceHost serviceHost, ITextMessage message)
        {
            return SimpleService.Greet(message.Text);
        }
    }
}