using MyApp.ServiceModel;
using ServiceStack;

namespace MyApp.ServiceInterface;

public class DummyServices : Service
{
    // public object Any(Dummy request) => request;
    public object Any(EchoComplexTypes request) => request;
    public object Any(EchoCollections request) => request;
}