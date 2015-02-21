using ServiceStack;

namespace Check.ServiceInterface
{
    public class TestMiniverView {}

    public class TestMinifierServices : Service
    {
         public object Any(TestMiniverView request)
         {
             return request;
         }
    }
}