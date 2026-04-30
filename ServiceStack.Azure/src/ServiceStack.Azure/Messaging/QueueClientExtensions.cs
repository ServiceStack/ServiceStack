using System;

namespace ServiceStack.Azure.Messaging;

public static class QueueClientExtensions
{
    public static string FromMessageBody(this byte[] messageBody) => messageBody.FromUtf8Bytes().FromMessageBody();

    public static string FromMessageBody(this string strMessage)
    {
        // WindowsAzure.ServiceBus (net472) is not wire-compatible with Azure.Messaging.ServiceBus (netcore).
        // Messages from the Windows client have a 64-char header and 2-char footer that must be stripped.
        // This has been left here for any legacy messages that have yet to be processed using Azure.Messaging.ServiceBus.
        // See https://github.com/Azure/azure-service-bus-dotnet/issues/239
        if (strMessage.StartsWith("@\u0006string", StringComparison.Ordinal))
        {
            strMessage = strMessage.Substring(64, strMessage.Length - 66);
        }

        return strMessage;
    }

    internal static string? SafeQueueName(this string queueName) =>
        queueName?.Replace(":", ".").Replace("[]", "Array");
}
