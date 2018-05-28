using ServiceStack;

namespace CheckWeb
{
    [Route("/defaultview/class")]
    public class DefaultViewAttr {}

    [DefaultView("TheView")]
    public class ViewTests : Service
    {
        public object Get(DefaultViewAttr request) => request;
    }
    
    [Route("/defaultview/action")]
    public class DefaultViewActionAttr {}

    public class ActionViewTests : Service
    {
        [DefaultView("TheView")]
        public object Get(DefaultViewActionAttr request) => request;
    }
    
}