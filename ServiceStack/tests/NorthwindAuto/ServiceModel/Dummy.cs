using MyApp.ServiceInterface;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

public class Dummy
{
    public GetNavItems GetNavItems { get; set; }
    public GetNavItemsResponse GetNavItemsResponse { get; set; }
    public EmptyResponse EmptyResponse { get; set; }
    public IdResponse IdResponse { get; set; }
    public StringResponse StringResponse { get; set; }
    public StringsResponse StringsResponse { get; set; }
    public ConvertSessionToToken ConvertSessionToToken { get; set; }
    public ConvertSessionToTokenResponse ConvertSessionToTokenResponse { get; set; }
    public CancelRequest CancelRequest { get; set; }
    public CancelRequestResponse CancelRequestResponse { get; set; }
    public UpdateEventSubscriber UpdateEventSubscriber { get; set; }
    public UpdateEventSubscriberResponse UpdateEventSubscriberResponse { get; set; }
}

[Route("/echo/collections")]
public class EchoCollections : IReturn<EchoCollections>
{
    public List<string> StringList { get; set; }
    public string[] StringArray { get; set; }
    public Dictionary<string, string> StringMap { get; set; }
    public Dictionary<int, string> IntStringMap { get; set; }
}

[Route("/echo/complex")]
public class EchoComplexTypes : IReturn<EchoComplexTypes>
{
    public SubType SubType { get; set; }
    public List<SubType> SubTypes { get; set; }
    public Dictionary<string, SubType> SubTypeMap { get; set; }
    public Dictionary<string, string> StringMap { get; set; }
    public Dictionary<int, string> IntStringMap { get; set; }
}

public class Data1
{
    public int Value { get; set; }
    public int? OptionalValue { get; set; }
    public string Text { get; set; }
    public string? OptionalText { get; set; }
    public List<string> Texts { get; set; }
    public List<string>? OptionalTexts { get; set; }
}

public class Data2
{
    [Required]
    public int Value { get; set; }
    [Required] // No effect
    public int? OptionalValue { get; set; }
    [Required]
    public string Text { get; set; }
    [Required] // Generates non-nullable
    public string? OptionalText { get; set; }
    [Required]
    public List<string> Texts { get; set; }
    [Required]
    public List<string>? OptionalTexts { get; set; }
}

public class Data3
{
    public int Value { get; set; }
    public int? OptionalValue { get; set; }
    [ServiceStack.DataAnnotations.Required]
    public string Text { get; set; }
    [System.ComponentModel.DataAnnotations.Required] // Not supported
    public string Text2 { get; set; }
    [ServiceStack.DataAnnotations.Required]
    public string? NText { get; set; }
    [System.ComponentModel.DataAnnotations.Required] // Not supported
    public string? NText2 { get; set; }
}

public class EchoData
{
    public Data1 Data1 { get; set; }
    public Data2 Data2 { get; set; }
    public Data3 Data3 { get; set; }
}

/*
public class Tupe<T>
{
    public T Item { get; set; }
}
public class Tupe<T1,T2>
{
    public T1 Item1 { get; set; }
    public T2 Item2 { get; set; }
}
public class Tupe<T1,T2,T3>
{
    public T1 Item1 { get; set; }
    public T2 Item2 { get; set; }
    public T3 Item3 { get; set; }
}

public class Tupes
{
    public Tupe<int> TupeInt { get; set; }
    public Tupe<int,string> TupeIntString { get; set; }
    public Tupe<int,string,Todo> TupeIntStringTodo { get; set; }
}
*/