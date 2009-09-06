namespace ServiceStack.OrmLite.Tests.Models
{
	public class ModelWithIdOnly
	{
		public ModelWithIdOnly()
		{
		}

		public ModelWithIdOnly(int id)
		{
			Id = id;
		}

		public int Id { get; set; }

	}
}