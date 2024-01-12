using System.Threading.Tasks;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ServiceStack.Azure.Messaging;

public static class QueueClientExtensions
{

#if NETCORE
    static readonly PropertyInfo InnerReceiverProperty =
        typeof(Microsoft.Azure.ServiceBus.QueueClient).GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).First(x => x.Name == "InnerReceiver");

    public static async Task<Microsoft.Azure.ServiceBus.Message> ReceiveAsync(this Microsoft.Azure.ServiceBus.QueueClient sbClient, TimeSpan? timeout)
    {
        var receiver = (Microsoft.Azure.ServiceBus.Core.MessageReceiver)InnerReceiverProperty.GetValue(sbClient)!;

        var msg = timeout.HasValue
            ? await receiver.ReceiveAsync(timeout.Value)
            : await receiver.ReceiveAsync();

        return msg;
    }
#endif

    public static string FromMessageBody(this byte[] messageBody) => FromMessageBody(messageBody.FromUtf8Bytes());

    public static string FromMessageBody(this Stream messageBody)
    {
        using (messageBody)
        {
            return messageBody.ReadToEnd().FromMessageBody();
        }                
    }

    public static string FromMessageBody(this string strMessage)
    {
        //Windows Azure Client is not wire-compatible with .NET Core client
        //we check if the message comes from Windows client and cut off 
        //64 header chars and 2 footer chars
        //see https://github.com/Azure/azure-service-bus-dotnet/issues/239  
        if (strMessage.StartsWith("@\u0006string", StringComparison.Ordinal))
        {
            strMessage = strMessage.Substring(64, strMessage.Length - 66);
        }

        return strMessage;
    }

    internal static string? SafeQueueName(this string queueName) =>
        queueName?.Replace(":", ".").Replace("[]", "Array");
}