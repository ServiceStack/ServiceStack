using NUnit.Framework;

namespace ServiceStack.OrmLite.MySql.Tests
{
	[TestFixture]
	public class OrmLiteConnectionTests 
		: OrmLiteTestBase
	{
		// [Test]
		public void Can_create_connection_to_blank_database()
		{
			var connString = @"C:\Projects\PoToPe\trunk\website\src\Mflow.Intranet\Mflow.Intranet\App_Data\Exports\2009-10\MonthlySnapshot.mdf";
			using (var db = connString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
			}
		}

		[Test]
		public void Can_create_connection()
		{
			using (var db = OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
			}
		}

	}
}