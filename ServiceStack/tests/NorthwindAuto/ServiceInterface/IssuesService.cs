
using MyApp.ServiceModel;
using ServiceStack;

namespace MyApp.ServiceInterface;

public class IssuesService : Service
{
    public object Any(GetWeatherForecast request) => Array.Empty<Forecast>();
    
    public object Any(Problem req)
    {
        return new ResponseBase<Dictionary<string, List<HelloResponse>>>
        {
            Result = new()
            {
                {"one", [new() { Result = "hello" }] }
            }
        };
    }

    public object Any(DigitalPrescriptionDMDRequest request) => new ResponseBase<DigitalPrescriptionDMDResponse>();
    
    public object Any(GetDiscountCodeBillingItem request) => new ResponseBase<BillingItem>();
}