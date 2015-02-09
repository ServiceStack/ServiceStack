using ServiceStack;

namespace RazorRockstars.Console.Files
{
    [Route("/ComplexModel")]
    public class ComplexModel
    {
        public int[] Ids { get; set; }
        public string[] Names { get; set; }
    }

    public class ComplexModelService : Service
    {
        public object Any(ComplexModel request)
        {
            return request;
        }
    }
}