using ServiceStack.Messaging.Tests.Objects.Exceptions;

namespace ServiceStack.Messaging.Tests.Services.Messaging
{
    public class ExceptionService : IService
    {
        public string Execute(IServiceHost serviceHost, ITextMessage message)
        {
            throw new MessagingException("ExceptionService.MessagingException");
        }
    }
}