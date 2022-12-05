#nullable enable
using System;
using ServiceStack.Messaging;

namespace ServiceStack;

public class MessageSerializer
{
    public static IMessageSerializer Instance { get; set; } = new ServiceStackMessageSerializer();
}

public class ServiceStackMessageSerializer : IMessageSerializer
{
    public byte[] ToBytes(IMessage message) => message.ToBytes();

    public byte[] ToBytes<T>(IMessage<T> message) => message.ToBytes();

    public IMessage ToMessage(byte[] bytes, Type ofType) => bytes.ToMessage(ofType);

    public Message<T> ToMessage<T>(byte[] bytes) => bytes.ToMessage<T>();
}