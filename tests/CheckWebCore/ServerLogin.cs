
using ServiceStack;

namespace CheckWebCore
{
    [Route("/validation/server/login")]
    public class ServerLogin {}
    [Route("/validation/server/register")]
    public class ServerRegister {}

    [Route("/validation/server/login-nojs")]
    public class ServerLoginNoJs {}
    [Route("/validation/server/register-nojs")]
    public class ServerRegisterNoJs {}

    public class ServerServices : Service
    {
        public object Any(ServerLogin request) => request;
        public object Any(ServerRegister request) => request;
        
        public object Any(ServerLoginNoJs request) => request;
        public object Any(ServerRegisterNoJs request) => request;
    }
}