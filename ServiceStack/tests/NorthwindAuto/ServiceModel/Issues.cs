using ServiceStack;

namespace MyApp.ServiceModel;

public class ResponseBase<T>
{
    public T Result { get; set; }
}

public record Problem : IReturn<ResponseBase<Dictionary<string, List<HelloResponse>>>>
{
    public int Id { get; set; }
}
