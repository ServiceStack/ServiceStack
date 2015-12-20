using ServiceStack;

namespace Check.ServiceModel
{
    [Route("/throwhttperror/{Status}")]
    public class ThrowHttpError
    {
        public int Status { get; set; }
        public string Message { get; set; }
    }

    public class ThrowHttpErrorResponse { }

    [Route("/throw404")]
    [Route("/throw404/{Message}")]
    public class Throw404
    {
        public string Message { get; set; }
    }

    [Route("/return404")]
    public class Return404 { }

    [Route("/return404result")]
    public class Return404Result { }

    [Route("/throw/{Type}")]
    public class ThrowType : IReturn<ThrowTypeResponse>
    {
        public string Type { get; set; }
        public string Message { get; set; }
    }

    public class ThrowTypeResponse
    {
        public ResponseStatus ResponseStatus { get; set; }
    }
}