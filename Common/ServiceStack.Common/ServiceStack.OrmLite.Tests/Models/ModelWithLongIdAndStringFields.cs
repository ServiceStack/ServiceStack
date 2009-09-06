namespace ServiceStack.OrmLite.Tests.Models
{
	public class ModelWithLongIdAndStringFields
	{
		public long Id { get; set; }

		public string Name { get; set; }

		public string AlbumId { get; set; }

		public string AlbumName { get; set; }
	}
}