using System.Net;
using NUnit.Framework;
using ServiceStack;

namespace NetCoreTests;

public class ServerEventTests
{
    public ServerEventTests()
    {
        HttpUtils.HttpClientHandlerFactory = () => new HttpClientHandler {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            UseDefaultCredentials = true,
            AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.Deflate | DecompressionMethods.GZip,
        };
        JsonApiClient.GlobalHttpMessageHandlerFactory = HttpUtils.HttpClientHandlerFactory;
    }
    
    [Test]
    public async Task Can_subscribe_to_channels()
    {
        var client = new ServerEventsClient("https://localhost:5001", ["myChannel"])
        {
            OnConnect = subscription =>
            {
                string subscriptionId = subscription.Id;
                string[] channels = subscription.Channels;

                //Post(new SubscriptionRequest(subscriptionId, channels));
                Console.WriteLine($"OnConnect({subscriptionId}): [{string.Join(", ", channels)}]");
            },
            OnMessage = message =>
            {
                string channel = message.Channel;
                string data = message.Data;

                Console.WriteLine($"OnMessage({channel}): {data}");
            }
        };
        await client.Connect();
        Console.WriteLine("After Connect()");

        try
        {
            await client.SubscribeToChannelsAsync("myChannel");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
