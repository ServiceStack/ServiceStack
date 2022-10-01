using MyApp.ServiceModel;
using ServiceStack;

namespace MyApp.ServiceInterface;

public class AllTypesService : Service
{
    public object Any(HelloAllTypes request) => request;
}

