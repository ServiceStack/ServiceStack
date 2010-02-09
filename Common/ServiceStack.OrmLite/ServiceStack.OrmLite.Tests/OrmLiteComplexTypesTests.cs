using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class OrmLiteComplexTypesTests
		: OrmLiteTestBase
	{

		[Ignore("Endless recursion, need to fix")]
		[Test]
		public void Can_insert_into_ModelWithComplexTypes_table()
		{
			using (var dbConn = ConnectionString.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithComplexTypes>(true);

				var row = ModelWithComplexTypes.Create(1);

				dbCmd.Insert(row);
			}
		}

		[Ignore("Endless recursion, need to fix")]
		[Test]
		public void Can_insert_and_select_from_ModelWithComplexTypes_table()
		{
			using (var dbConn = ConnectionString.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithComplexTypes>(true);

				var row = ModelWithComplexTypes.Create(1);

				dbCmd.Insert(row);

				var rows = dbCmd.Select<ModelWithComplexTypes>();

				Assert.That(rows, Has.Count(1));

				ModelWithComplexTypes.AssertIsEqual(rows[0], row);
			}
		}

		[Test]
		public void Can_insert_and_select_from_OrderLineData()
		{
			using (var dbConn = ConnectionString.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.CreateTable<SampleOrderLine>(true);

				var orderIds = new[] { 1, 2, 3, 4, 5 }.ToList();

				orderIds.ForEach(x => dbCmd.Insert(
					SampleOrderLine.Create(Guid.NewGuid(), x, 1)));

				var rows = dbCmd.Select<SampleOrderLine>();
				Assert.That(rows, Has.Count(orderIds.Count));

			}

		}


	}

}