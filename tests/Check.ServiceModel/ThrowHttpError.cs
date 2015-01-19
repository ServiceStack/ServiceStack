using ServiceStack;

namespace Check.ServiceModel
{
    [Route("/throwhttperror/{Status}")]
    public class ThrowHttpError
    {
        public int Status { get; set; }
        public string Message { get; set; }
    }

    [Route("/throw404")]
    [Route("/throw404/{Message}")]
    public class Throw404
    {
        public string Message { get; set; }
    }
}