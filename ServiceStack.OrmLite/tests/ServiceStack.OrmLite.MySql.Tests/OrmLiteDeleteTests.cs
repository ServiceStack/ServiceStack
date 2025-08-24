using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.MySql.Tests;

[TestFixture]
public class OrmLiteDeleteTests
	: OrmLiteTestBase
{
	[SetUp]
	public void SetUp()
	{
	}

	[Test]
	public void Can_Delete_from_ModelWithFieldsOfDifferentTypes_table()
	{
		using var db = OpenDbConnection();
		db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

		var rowIds = new List<int>(new[] { 1, 2, 3 });
		rowIds.ForEach(x => db.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

		var rows = db.Select<ModelWithFieldsOfDifferentTypes>();
		var row2 = rows.First(x => x.Id == 2);

		db.Delete(row2);

		rows = db.SelectByIds<ModelWithFieldsOfDifferentTypes>(rowIds);
		var dbRowIds = rows.ConvertAll(x => x.Id);

		Assert.That(dbRowIds, Is.EquivalentTo(new[] { 1, 3 }));
	}

	[Test]
	public void Can_DeleteById_from_ModelWithFieldsOfDifferentTypes_table()
	{
		using var db = OpenDbConnection();
		db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

		var rowIds = new List<int>(new[] { 1, 2, 3 });
		rowIds.ForEach(x => db.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

		db.DeleteById<ModelWithFieldsOfDifferentTypes>(2);

		var rows = db.SelectByIds<ModelWithFieldsOfDifferentTypes>(rowIds);
		var dbRowIds = rows.ConvertAll(x => x.Id);

		Assert.That(dbRowIds, Is.EquivalentTo(new[] { 1, 3 }));
	}

	[Test]
	public void Can_DeleteByIds_from_ModelWithFieldsOfDifferentTypes_table()
	{
		using var db = OpenDbConnection();
		db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

		var rowIds = new List<int>(new[] { 1, 2, 3 });
		rowIds.ForEach(x => db.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

		db.DeleteByIds<ModelWithFieldsOfDifferentTypes>(new[] { 1, 3 });

		var rows = db.SelectByIds<ModelWithFieldsOfDifferentTypes>(rowIds);
		var dbRowIds = rows.ConvertAll(x => x.Id);

		Assert.That(dbRowIds, Is.EquivalentTo(new[] { 2 }));
	}

}