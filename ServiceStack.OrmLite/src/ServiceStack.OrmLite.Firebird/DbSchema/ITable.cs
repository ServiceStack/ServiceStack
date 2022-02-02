
namespace ServiceStack.OrmLite.Firebird.DbSchema
{
	public interface ITable
	{
		string Name { get; set; }

		string Owner { get; set; }
	}
}

