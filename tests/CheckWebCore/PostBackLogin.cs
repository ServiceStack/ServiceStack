
using ServiceStack;

namespace CheckWebCore
{
    [Route("/validation/postback/login")]
    public class PostBackLogin
    {
    }

    [Route("/validation/postback/login-nojs")]
    public class PostBackLoginNoJs
    {
    }

    public class PostBackServices : Service
    {
        public object Any(PostBackLogin request) => request;
        public object Any(PostBackLoginNoJs request) => request;
    }
}