using ServiceStack;

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