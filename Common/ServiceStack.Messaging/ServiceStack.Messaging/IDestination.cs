using System;

namespace ServiceStack.Messaging
{
    public interface IDestination
    {
        DestinationType DestinationType { get; }

        string Uri { get; }
    }
}
