
using ServiceStack;

namespace CheckWebCore
{
    [Route("/validation/postback/login")]
    public class PostBackLogin
    {
    }

    public class PostBackServices : Service
    {
        public object Any(PostBackLogin request) => request;
    }
}