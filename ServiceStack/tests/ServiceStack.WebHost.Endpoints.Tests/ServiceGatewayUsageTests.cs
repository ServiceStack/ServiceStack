using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    class UsageNone { }
    class UsageReturn : IReturn<UsageReturn> { }
    class UsageVoid : IReturnVoid { }

    /// <summary>
    /// Ensure no ambiguous compile errors with normal API Usage
    /// </summary>
    public class ServiceGatewayUsageTests
    {
        void ConcreteSyncApiUsage(JsonServiceClient client)
        {
            UsageNone none = client.Send<UsageNone>(new UsageNone());
            UsageReturn @return = client.Send(new UsageReturn());
            client.Send(new UsageVoid());
            List<UsageReturn> @returnAll = client.SendAll(new[] { new UsageReturn() });
            client.Publish(new UsageNone());
            client.Publish(new UsageReturn());
            client.Publish(new UsageVoid());
            client.PublishAll(new[] { new UsageNone() });
            client.PublishAll(new [] { new UsageReturn() });
            client.PublishAll(new [] { new UsageVoid() });
        }

        async Task ConcreteAsyncApiUsage(JsonServiceClient client)
        {
            UsageNone none = await client.SendAsync<UsageNone>(new UsageNone());
            UsageNone noneToken = await client.SendAsync<UsageNone>(new UsageNone(), CancellationToken.None);
            UsageReturn @return = await client.SendAsync(new UsageReturn());
            UsageReturn returnToken = await client.SendAsync(new UsageReturn(), CancellationToken.None);
            await client.SendAsync(new UsageVoid());
            await client.SendAsync(new UsageVoid(), CancellationToken.None);
            List<UsageReturn> returnAll = await client.SendAllAsync(new[] { new UsageReturn() });
            List<UsageReturn> returnAllToken = await client.SendAllAsync(new[] { new UsageReturn() }, CancellationToken.None);
            await client.PublishAsync(new UsageNone());
            await client.PublishAsync(new UsageNone(), CancellationToken.None);
            await client.PublishAsync(new UsageReturn());
            await client.PublishAsync(new UsageReturn(), CancellationToken.None);
            await client.PublishAsync(new UsageVoid());
            await client.PublishAsync(new UsageVoid(), CancellationToken.None);
            await client.PublishAllAsync(new[] { new UsageNone() });
            await client.PublishAllAsync(new[] { new UsageNone() }, CancellationToken.None);
            await client.PublishAllAsync(new[] { new UsageReturn() });
            await client.PublishAllAsync(new[] { new UsageReturn() }, CancellationToken.None);
            await client.PublishAllAsync(new[] { new UsageVoid() });
            await client.PublishAllAsync(new[] { new UsageVoid() }, CancellationToken.None);
        }

        void IServiceClientSyncApiUsage(IServiceClient client)
        {
            UsageNone none = client.Send<UsageNone>(new UsageNone());
            UsageReturn @return = client.Send(new UsageReturn());
            client.Send(new UsageVoid());
            List<UsageReturn> @returnAll = client.SendAll(new[] { new UsageReturn() });
            client.Publish(new UsageNone());
            client.Publish(new UsageReturn());
            client.Publish(new UsageVoid());
            client.PublishAll(new[] { new UsageNone() });
            client.PublishAll(new[] { new UsageReturn() });
            client.PublishAll(new[] { new UsageVoid() });
        }

        async Task IServiceClientAsyncApiUsage(IServiceClient client)
        {
            UsageNone none = await client.SendAsync<UsageNone>(new UsageNone());
            UsageNone noneToken = await client.SendAsync<UsageNone>(new UsageNone(), CancellationToken.None);
            UsageReturn @return = await client.SendAsync(new UsageReturn());
            UsageReturn returnToken = await client.SendAsync(new UsageReturn(), CancellationToken.None);
            await client.SendAsync(new UsageVoid());
            await client.SendAsync(new UsageVoid(), CancellationToken.None);
            List<UsageReturn> returnAll = await client.SendAllAsync(new[] { new UsageReturn() });
            List<UsageReturn> returnAllToken = await client.SendAllAsync(new[] { new UsageReturn() }, CancellationToken.None);
            await client.PublishAsync(new UsageNone());
            await client.PublishAsync(new UsageNone(), CancellationToken.None);
            await client.PublishAsync(new UsageReturn());
            await client.PublishAsync(new UsageReturn(), CancellationToken.None);
            await client.PublishAsync(new UsageVoid());
            await client.PublishAsync(new UsageVoid(), CancellationToken.None);
            await client.PublishAllAsync(new[] { new UsageNone() });
            await client.PublishAllAsync(new[] { new UsageNone() }, CancellationToken.None);
            await client.PublishAllAsync(new[] { new UsageReturn() });
            await client.PublishAllAsync(new[] { new UsageReturn() }, CancellationToken.None);
            await client.PublishAllAsync(new[] { new UsageVoid() });
            await client.PublishAllAsync(new[] { new UsageVoid() }, CancellationToken.None);
        }

        void IServiceGatewaySyncApiUsage(IServiceGateway client)
        {
            UsageNone none = client.Send<UsageNone>(new UsageNone());
            UsageReturn @return = client.Send(new UsageReturn());
            client.Send(new UsageVoid());
            List<UsageReturn> @returnAll = client.SendAll(new[] { new UsageReturn() });
            client.Publish(new UsageNone());
            client.Publish(new UsageReturn());
            client.Publish(new UsageVoid());
            client.PublishAll(new[] { new UsageNone() });
            client.PublishAll(new[] { new UsageReturn() });
            client.PublishAll(new[] { new UsageVoid() });
        }

        async Task IServiceGatewayAsyncApiUsage(IServiceGateway client)
        {
            UsageNone none = await client.SendAsync<UsageNone>(new UsageNone());
            UsageNone noneToken = await client.SendAsync<UsageNone>(new UsageNone(), CancellationToken.None);
            UsageReturn @return = await client.SendAsync(new UsageReturn());
            UsageReturn returnToken = await client.SendAsync(new UsageReturn(), CancellationToken.None);
            await client.SendAsync(new UsageVoid());
            await client.SendAsync(new UsageVoid(), CancellationToken.None);
            List<UsageReturn> returnAll = await client.SendAllAsync(new[] { new UsageReturn() });
            List<UsageReturn> returnAllToken = await client.SendAllAsync(new[] { new UsageReturn() }, CancellationToken.None);
            await client.PublishAsync(new UsageNone());
            await client.PublishAsync(new UsageNone(), CancellationToken.None);
            await client.PublishAsync(new UsageReturn());
            await client.PublishAsync(new UsageReturn(), CancellationToken.None);
            await client.PublishAsync(new UsageVoid());
            await client.PublishAsync(new UsageVoid(), CancellationToken.None);
            await client.PublishAllAsync(new[] { new UsageNone() });
            await client.PublishAllAsync(new[] { new UsageNone() }, CancellationToken.None);
            await client.PublishAllAsync(new[] { new UsageReturn() });
            await client.PublishAllAsync(new[] { new UsageReturn() }, CancellationToken.None);
            await client.PublishAllAsync(new[] { new UsageVoid() });
            await client.PublishAllAsync(new[] { new UsageVoid() }, CancellationToken.None);
        }

        async Task IServiceGatewayAsyncApiUsage(IServiceGatewayAsync client)
        {
            UsageNone none = await client.SendAsync<UsageNone>(new UsageNone());
            UsageNone noneToken = await client.SendAsync<UsageNone>(new UsageNone(), CancellationToken.None);
            UsageReturn @return = await client.SendAsync<UsageReturn>(new UsageReturn());
            UsageReturn returnToken = await client.SendAsync<UsageReturn>(new UsageReturn(), CancellationToken.None);
            await client.SendAsync<UsageReturn>(new UsageVoid());
            await client.SendAsync<UsageReturn>(new UsageVoid(), CancellationToken.None);
            List<UsageReturn> returnAll = await client.SendAllAsync<UsageReturn>(new[] { new UsageReturn() });
            List<UsageReturn> returnAllToken = await client.SendAllAsync<UsageReturn>(new[] { new UsageReturn() }, CancellationToken.None);
            await client.PublishAsync(new UsageNone());
            await client.PublishAsync(new UsageNone(), CancellationToken.None);
            await client.PublishAsync(new UsageReturn());
            await client.PublishAsync(new UsageReturn(), CancellationToken.None);
            await client.PublishAsync(new UsageVoid());
            await client.PublishAsync(new UsageVoid(), CancellationToken.None);
            await client.PublishAllAsync(new[] { new UsageNone() });
            await client.PublishAllAsync(new[] { new UsageNone() }, CancellationToken.None);
            await client.PublishAllAsync(new[] { new UsageReturn() });
            await client.PublishAllAsync(new[] { new UsageReturn() }, CancellationToken.None);
            await client.PublishAllAsync(new[] { new UsageVoid() });
            await client.PublishAllAsync(new[] { new UsageVoid() }, CancellationToken.None);
        }
    }
}