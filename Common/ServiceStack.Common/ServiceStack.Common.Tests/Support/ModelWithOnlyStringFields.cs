using ServiceStack.DataAccess;

namespace ServiceStack.Common.Tests.Support
{
	public class ModelWithOnlyStringFields
	{
		public string Id { get; set; }

		[Index]
		public string Name { get; set; }

		public string AlbumId { get; set; }

		[Index]
		public string AlbumName { get; set; }

		public static ModelWithOnlyStringFields Create(string id)
		{
			return new ModelWithOnlyStringFields {
				Id = id,
				Name = "Name" + id,
				AlbumId = "AlbumId" + id,
				AlbumName = "AlbumName" + id,
			};
		}
	}
}