namespace ServiceStack.Common.Tests.Models
{
    public class ModelWithOnlyStringFields
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string AlbumId { get; set; }

        public string AlbumName { get; set; }

        public static ModelWithOnlyStringFields Create(string id)
        {
            return new ModelWithOnlyStringFields
            {
                Id = id,
                Name = "Name",
                AlbumId = "AlbumId",
                AlbumName = "AlbumName",
            };
        }
    }
}