using Funq;
using ServiceStack;

namespace CheckCoreApi;

public class AppHost() : AppHostBase(nameof(CheckCoreApi), typeof(MyServices).Assembly)
{
    public override void Configure(Container container)
    {
    }
}