using ServiceStack.DataAnnotations;

namespace ServiceStack.Common.Tests.Models
{
    [CompositeIndex(true, "Composite1", "Composite2")]
    public class ModelWithCompositeIndexFields
    {
        public string Id { get; set; }

        [Index]
        public string Name { get; set; }

        public string AlbumId { get; set; }

        [Index(true)]
        public string UniqueName { get; set; }

        public string Composite1 { get; set; }

        public string Composite2 { get; set; }
    }
}