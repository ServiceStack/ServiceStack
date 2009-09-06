using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class OrmLiteDeleteTests
		: OrmLiteTestBase
	{

		[Test]
		public void Can_Delete_from_ModelWithFieldsOfDifferentTypes_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithFieldsOfDifferentTypes>(false);

				var rowIds = new List<int>(new[] { 1, 2, 3 });
				rowIds.ForEach(x => dbCmd.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

				var rows = dbCmd.Select<ModelWithFieldsOfDifferentTypes>();
				var row2 = rows.First(x => x.Id == 2);

				dbCmd.Delete(row2);

				rows = dbCmd.GetByIds<ModelWithFieldsOfDifferentTypes>(rowIds);
				var dbRowIds = rows.ConvertAll(x => x.Id);

				Assert.That(dbRowIds, Is.EquivalentTo(new[] { 1, 3 }));
			}
		}

		[Test]
		public void Can_DeleteById_from_ModelWithFieldsOfDifferentTypes_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithFieldsOfDifferentTypes>(false);

				var rowIds = new List<int>(new[] { 1, 2, 3 });
				rowIds.ForEach(x => dbCmd.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

				dbCmd.DeleteById<ModelWithFieldsOfDifferentTypes>(2);

				var rows = dbCmd.GetByIds<ModelWithFieldsOfDifferentTypes>(rowIds);
				var dbRowIds = rows.ConvertAll(x => x.Id);

				Assert.That(dbRowIds, Is.EquivalentTo(new[] { 1, 3 }));
			}
		}

	}
}