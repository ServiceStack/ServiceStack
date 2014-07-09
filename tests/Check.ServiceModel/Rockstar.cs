using ServiceStack;

namespace Check.ServiceModel
{
    [Route("/rockstars")]
    public class QueryRockstars : QueryBase<Rockstar> {}

    public class Rockstar
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
    }
}
