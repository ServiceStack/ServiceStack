using ServiceStack.ServiceHost;

namespace ServiceStack.Razor.Tests
{
    public class CheckBoxService : ServiceInterface.Service
    {
        public CheckBoxData Get(GetCheckBox request)
        {
            return new CheckBoxData
                       {
                           BooleanValue = true
                       };
        }

        public CheckBoxData Post(CheckBoxData request)
        {
            return Get(new GetCheckBox());
        }
    }

    [Route("/checkbox", "GET")]
    public class GetCheckBox
    {
        
    }

    [Route("/checkbox", "POST")]
    public class CheckBoxData
    {
        public bool BooleanValue { get; set; }
    }
}