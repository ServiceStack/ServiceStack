using ServiceStack;

namespace MyApp.ServiceModel;

[Tag("issues")]
public class GetWeatherForecast : IGet, IReturn<Forecast[]>
{
    public required DateOnly? Date { get; set; }
}

public record Forecast(DateOnly Date, int TemperatureC, string? Summary) : IGet, IReturn<Forecast[]>
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

[Tag("issues")]
public record Problem : IReturn<ResponseBase<Dictionary<string, List<HelloResponse>>>>
{
    public int Id;
}

public class ResponseBase<T>
{
    [ApiMember(ExcludeInSchema = true)]
    public ResponseStatus ResponseStatus { get; set; }
    [ApiMember(Description = "This will be returned when there is a single result available. (e.g. get single object by id)")]
    public T Result { get; set; }
    [ApiMember(Description = "This will be returned when there is a multiple results available (e.g. search or listing requests).")]
    public List<T> Results { get; set; }
    [ApiMember(Description = "This will be returned when there is a multiple results available (e.g. search or listing requests).")]
    public int? Total { get; set; }
    [ApiMember(Description = "This will be return the amount of skipped rows when paginating")]
    public int? Skip { get; set; }
}

[Tag("issues")]
public class DigitalPrescriptionDMDRequest : IReturn<ResponseBase<DigitalPrescriptionDMDResponse>> { public string Term { get; set; } }
public class DigitalPrescriptionDMDResponse
{
    public string Name { get; set; }
    public int ProductId { get; set; }
}


[Tag("issues")]
[Route("/getDiscountCodesBillingItem", Verbs = "POST")]
public class GetDiscountCodeBillingItem : IReturn<ResponseBase<BillingItem>>
{
    public BillingItem BillingItem { get; set; }

    public Guid DiscountCodeId { get; set; }
}

public class BillingItem
{
    public string Name { get; set; }
} 

