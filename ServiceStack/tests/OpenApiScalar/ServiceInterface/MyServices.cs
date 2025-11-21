using OpenApiScalar.ServiceModel;
using ServiceStack;

namespace OpenApiScalar.ServiceInterface;

public class MyServices : Service
{
    public object Any(Hello request) => new HelloResponse
    {
        Result = $"Hello, {request.Name ?? "World"}!"
    };
}

