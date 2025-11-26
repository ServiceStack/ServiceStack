using OpenApi3Swashbuckle.ServiceModel;
using ServiceStack;

namespace OpenApi3Swashbuckle.ServiceInterface;

public class MyServices : Service
{
    public object Any(Hello request) => new HelloResponse
    {
        Result = $"Hello, {request.Name ?? "World"}!"
    };
    
    public object Any(HelloSecure request) => new HelloResponse
    {
        Result = $"Hello, {request.Name ?? "World"}!"
    };

    public object Any(HelloApiKey request) => new HelloResponse
    {
        Result = $"Hello, {request.Name ?? "World"}!"
    };
}

