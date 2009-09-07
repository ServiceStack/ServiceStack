namespace ServiceStack.OrmLite.Tests.Models
{
	public class ModelWithIdAndName
	{
		public ModelWithIdAndName()
		{
		}

		public ModelWithIdAndName(int id)
		{
			Id = id;
			Name = "Name" + id;
		}

		public int Id { get; set; }

		public string Name { get; set; }

	}
}