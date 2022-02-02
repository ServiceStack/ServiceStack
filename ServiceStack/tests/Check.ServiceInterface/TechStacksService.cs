using Check.ServiceModel;
using ServiceStack;

namespace Check.ServiceInterface
{
    public class TechStacksService : Service
    {
        public object Any(GetTechnology request) => new GetTechnologyResponse();
    }
}