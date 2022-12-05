using System;

namespace ServiceStack.Messaging;

public interface IMessageByteSerializer
{
    byte[] ToBytes(IMessage message);

    byte[] ToBytes<T>(IMessage<T> message);

    IMessage ToMessage(byte[] bytes, Type ofType);

    Message<T> ToMessage<T>(byte[] bytes);
}
