using ServiceStack;

namespace MyApp.ServiceInterface;

public class SendVerbResponse
{
    public int Id { get; set; }
    public string PathInfo { get; set; }
    public string RequestMethod { get; set; }
}

public class SendDefault : IReturn<SendVerbResponse>
{
    public int Id { get; set; }
}

[Route("/sendrestget/{Id}", "GET")]
public class SendRestGet : IReturn<SendVerbResponse>, IGet
{
    public int Id { get; set; }
}

public class SendGet : IReturn<SendVerbResponse>, IGet
{
    public int Id { get; set; }
}

public class SendPost : IReturn<SendVerbResponse>, IPost
{
    public int Id { get; set; }
}

public class SendPut : IReturn<SendVerbResponse>, IPut
{
    public int Id { get; set; }
}

public class SendReturnVoid : IReturnVoid
{
    public int Id { get; set; }
}

public class SendVerbService : Service
{
    public object Any(SendDefault request)
    {
        return CreateResponse(request.Id);
    }

    public object Get(SendRestGet request)
    {
        return CreateResponse(request.Id);
    }

    public object Any(SendGet request)
    {
        return CreateResponse(request.Id);
    }

    public object Any(SendPost request)
    {
        return CreateResponse(request.Id);
    }

    public object Any(SendPut request)
    {
        return CreateResponse(request.Id);
    }

    private object CreateResponse(int requestId)
    {
        return new SendVerbResponse
        {
            Id = requestId,
            PathInfo = base.Request.PathInfo,
            RequestMethod = base.Request.Verb
        };
    }

    public void Any(SendReturnVoid request) { }
}