using ServiceStack;

namespace Check.ServiceInterface
{
    [Route("/anontype")]
    public class AnonType { }

    public class AnonTypeService : Service
    {
        public object Any(AnonType request)
        {
            return new { PrimaryKeyId = 1 };
        }
    }
}