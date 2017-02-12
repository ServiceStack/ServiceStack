using System.Collections.Generic;
using ServiceStack;

namespace Check.ServiceInterface
{
    public class BuiltInDtosService : Service
    {
        public object Any(GetEventSubscribers request)
        {
            return new List<Dictionary<string, string>>();
        }

        public object Any(UpdateEventSubscriber request)
        {
            return new UpdateEventSubscriberResponse();
        }
    }
}