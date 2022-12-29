
namespace ServiceStack.OrmLite.Oracle.DbSchema
{
	public interface ITable
	{
		string Name { get; set; }

		string Owner { get; set; }
	}
}

