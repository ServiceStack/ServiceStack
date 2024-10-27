
using MyApp.ServiceModel;
using ServiceStack;

namespace MyApp.ServiceInterface;

public class IssuesService : Service
{
    public object Any(Problem req)
    {
        return new Dictionary<string, List<HelloResponse>>() {
            {"one", [new() { Result = "hello" }] }
        };
    }
}