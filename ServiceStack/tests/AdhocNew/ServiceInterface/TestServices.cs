using MyApp.ServiceModel;

namespace MyApp.ServiceInterface;

public class SearchRootSummary : QueryDb<RootSummary>
{
    
}
public class RootSummary {}
public class QueryDbRootService(IAutoQueryDb autoQuery) : Service
{
    public QueryResponse<RootSummary> Get(SearchRootSummary request)
    {
        using var db = autoQuery.GetDb<RootSummary>(Request);
        return new();
    }
    
    public object Any(TestNullable request) => request.ConvertTo<TestNullableResponse>();
}