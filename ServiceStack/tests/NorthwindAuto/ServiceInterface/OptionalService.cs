using ServiceStack;

namespace MyApp.ServiceInterface;

public record class OptionalClass(int Id);
public enum OptionalEnum { Value1 }

public class OptionalTest : IReturn<OptionalTest>
{
    public int Int { get; set; }
    public int? NInt { get; set; }
    [ValidateNotNull]
    public int? NRequiredInt { get; set; }
    public string String { get; set; }
    public string? NString { get; set; }
    [ValidateNotEmpty]
    public string? NRequiredString { get; set; }
    
    public OptionalClass OptionalClass { get; set; }
    public OptionalClass? NOptionalClass { get; set; }
    [ValidateNotNull]
    public OptionalClass? NRequiredOptionalClass { get; set; }
    
    public OptionalEnum OptionalEnum { get; set; }
    public OptionalEnum? NOptionalEnum { get; set; }
    [ValidateNotNull]
    public OptionalEnum? NRequiredOptionalEnum { get; set; }
}

public class OptionalService : Service
{
    public object Any(OptionalTest request) => request;
}
