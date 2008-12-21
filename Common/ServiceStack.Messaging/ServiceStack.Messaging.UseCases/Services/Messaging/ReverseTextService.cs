using ServiceStack.Messaging.UseCases.Services.Basic;

namespace ServiceStack.Messaging.UseCases.Services.Messaging
{
    public class ReverseTextService : IService
    {
        #region IService Members

        public string Execute(IServiceHost serviceHost, ITextMessage message)
        {
            return SimpleService.Reverse(message.Text);
        }

        #endregion
    }
}