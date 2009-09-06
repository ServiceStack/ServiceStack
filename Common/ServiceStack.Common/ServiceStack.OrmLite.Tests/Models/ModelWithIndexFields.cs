using ServiceStack.DataAccess;

namespace ServiceStack.OrmLite.Tests.Models
{
	public class ModelWithIndexFields
	{
		public string Id { get; set; }

		[Index]
		public string Name { get; set; }

		public string AlbumId { get; set; }

		[Index(true)]
		public string UniqueName { get; set; }
	}
}