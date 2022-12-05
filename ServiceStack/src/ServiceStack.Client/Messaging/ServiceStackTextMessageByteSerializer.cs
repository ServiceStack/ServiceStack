using System;

namespace ServiceStack.Messaging;

public class ServiceStackTextMessageByteSerializer : IMessageByteSerializer
{
    public byte[] ToBytes(IMessage message) => message.ToBytes();

    public byte[] ToBytes<T>(IMessage<T> message) => message.ToBytes();

    public IMessage ToMessage(byte[] bytes, Type ofType) => bytes.ToMessage(ofType);

    public Message<T> ToMessage<T>(byte[] bytes) => bytes.ToMessage<T>();
}
