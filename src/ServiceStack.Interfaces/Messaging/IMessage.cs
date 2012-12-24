using System;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Messaging
{
    public interface IMessage
        : IHasId<Guid>
    {
        DateTime CreatedDate { get; }

        long Priority { get; set; }

        int RetryAttempts { get; set; }

        Guid? ReplyId { get; set; }

        string ReplyTo { get; set; }

        int Options { get; set; }

        MessageError Error { get; set; }

        object Body { get; set; }
    }

    public interface IMessage<T>
        : IMessage
    {
        T GetBody();
    }
}