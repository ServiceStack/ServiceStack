using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Dapper;

namespace ServiceStack.OrmLite.SqlServerTests;

[TestFixture]
public class RowVersionTests : OrmLiteTestBase
{
	public class ByteChildTbl
	{
		[PrimaryKey]
		public Guid Id { get; set; }

		[References(typeof(ByteTbl))]
		public Guid ParentId { get; set; }

		[RowVersion]
		public byte[] RowVersion { get; set; }

			
	}

	public class ByteTbl
	{
		[PrimaryKey]
		public Guid Id { get; set; }

		public string Text { get; set; }

		[RowVersion]
		public byte[] RowVersion { get; set; }

		[Reference]
		public List<ByteChildTbl> Children { get; set; } = [];
	}

	public class UlongChildTbl
	{
		[PrimaryKey]
		public Guid Id { get; set; }

		[References(typeof(UlongTbl))]
		public Guid ParentId { get; set; }

		[RowVersion]
		public ulong RowVersion { get; set; }
	}

	public class UlongTbl
	{
		[PrimaryKey]
		public Guid Id { get; set; }

		public string Text { get; set; }

		// Use of ulong makes embedded Dapper functionality unavailable
		[RowVersion]
		public ulong RowVersion { get; set; }

		[Reference]
		public List<UlongChildTbl> Children { get; set; } = [];
	}

	[Test]
	public void Read_and_write_to_tables_with_rowversions()
	{
		using (var db = OpenDbConnection())
		{
			// Show that we can drop and create tables with rowversions of both .NET types and both get created as ROWVERSION in MSSQL
			db.DropTable<ByteChildTbl>();
			db.DropTable<ByteTbl>();
			db.DropTable<UlongChildTbl>();
			db.DropTable<UlongTbl>();
			db.CreateTable<ByteTbl>();
			db.CreateTable<ByteChildTbl>();
			db.CreateTable<UlongTbl>();
			db.CreateTable<UlongChildTbl>();

			{
				// Confirm off new Ormlite CRUD functionality with byte[] rowversion

				var byteId = Guid.NewGuid();
				db.Insert(new ByteTbl() { Id = byteId });

				var getByteRecord = db.SingleById<ByteTbl>(byteId);
				getByteRecord.Text += " Updated";
				db.Update(getByteRecord); //success!

				getByteRecord.Text += "Attempting to update stale record";

				//Can't update stale record
				Assert.Throws<OptimisticConcurrencyException>(() => db.Update(getByteRecord));

				//Can update latest version
				var updatedRow = db.SingleById<ByteTbl>(byteId); // fresh version
				updatedRow.Text += "Update Success!";
				db.Update(updatedRow);

				updatedRow = db.SingleById<ByteTbl>(byteId);
				db.Delete(updatedRow); // can delete fresh version
			}

			{
				// confirm original Ormlite CRUD functionality based on ulong rowversion

				var ulongId = Guid.NewGuid();
				db.Insert(new UlongTbl { Id = ulongId });

				var getUlongRecord = db.SingleById<UlongTbl>(ulongId);
				getUlongRecord.Text += " Updated";
				db.Update(getUlongRecord); //success!

				getUlongRecord.Text += "Attempting to update stale record";

				//Can't update stale record
				Assert.Throws<OptimisticConcurrencyException>(() => db.Update(getUlongRecord));

				// Can update latest version
				var updatedUlongRow = db.SingleById<UlongTbl>(ulongId); // fresh version
				updatedUlongRow.Text += "Update Success!";
				db.Update(updatedUlongRow);

				updatedUlongRow = db.SingleById<UlongTbl>(ulongId);
				db.Delete(updatedUlongRow); // can delete fresh version
			}

			{
				// Confirm that original ulong rowversion unfortunately fails with Dapper custom sql queries

				var ulongId = Guid.NewGuid();
				db.Insert(new UlongTbl { Id = ulongId });

				// As a further example, using Dapper but without rowversion column WILL work (but rowversion will NOT be set of course)
				var thisDapperQryWorks = db.Query<UlongTbl>("select Id, Text from [UlongTbl]").ToList();
				Assert.True(thisDapperQryWorks.Count == 1);

				// But any time RowVersion as ulong is include... no joy.. the map from db rowversion to ulong in dapper fails
				Assert.Throws<System.Data.DataException>(() => db.Query<UlongTbl>("select  Id, Text, Rowversion from [UlongTbl]"));
			}

			{
				// Now test out use of new byte[] rowversion with Dapper custom sql queries

				var byteId = Guid.NewGuid();
				db.Insert(new ByteTbl { Id = byteId });

				// As a further example, using Dapper but without rowversion column WILL work, BUT the rowversion WONT BE SET (of course)
				var thisDapperQryWorks = db.Query<ByteTbl>("select Id, Text from [ByteTbl]").ToList();
				Assert.True(thisDapperQryWorks.Count == 1);
				Assert.True(thisDapperQryWorks.First().RowVersion == default(byte[]));

				// But any time RowVersion as ulong is include... no joy.. the map from db rowversion to ulong in dapper fails
				var thisDapperQryWithRowVersionAlsoWorks = db.Query<ByteTbl>("select  Id, Text, Rowversion from [ByteTbl]").ToList();
				Assert.True(thisDapperQryWithRowVersionAlsoWorks.Count == 1);
				Assert.True(thisDapperQryWithRowVersionAlsoWorks.First().RowVersion != default(byte[]));
			}

			{
				// test original ulong version with child operations
				var ulongId1 = Guid.NewGuid();
				db.Save(new UlongTbl
				{
					Id = ulongId1,
					Children = new List<UlongChildTbl>()
					{
						new UlongChildTbl { Id = Guid.NewGuid() },
						new UlongChildTbl { Id = Guid.NewGuid() }
					}
				}, references: true);
				var ulongObj1 = db.LoadSelect<UlongTbl>(x => x.Id == ulongId1, x => x.Children).First();
				Assert.AreNotEqual(default(byte[]), ulongObj1.RowVersion);
				Assert.AreNotEqual(default(byte[]), ulongObj1.Children[0].RowVersion);
				Assert.AreNotEqual(default(byte[]), ulongObj1.Children[1].RowVersion);

				var ulongId2 = Guid.NewGuid();
				db.Save(new UlongTbl
				{
					Id = ulongId2,
					Children = new List<UlongChildTbl>()
					{
						new UlongChildTbl {Id = Guid.NewGuid()},
						new UlongChildTbl {Id = Guid.NewGuid()}
					}
				}, references: true);
				var ulongObj2 = db.LoadSelect<UlongTbl>(x => x.Id == ulongId2, x => x.Children).First();
				Assert.AreNotEqual(default(byte[]), ulongObj2.RowVersion);
				Assert.AreNotEqual(default(byte[]), ulongObj2.Children[0].RowVersion);
				Assert.AreNotEqual(default(byte[]), ulongObj2.Children[1].RowVersion);

				// COnfirm multip select logic works
				var q = db.From<UlongTbl>()
						.Join<UlongTbl, UlongChildTbl>()
					;
				var results = db.SelectMulti<UlongTbl, UlongChildTbl>(q);
				foreach (var tuple in results)
				{
					UlongTbl parent = tuple.Item1;
					UlongChildTbl child = tuple.Item2;
					Assert.AreNotEqual(default(byte[]), parent.RowVersion);
					Assert.AreNotEqual(default(byte[]), child.RowVersion);
				}
			}

			{
				// test byte[] version with child operations
				var byteid1 = Guid.NewGuid();
				db.Save(new ByteTbl
				{
					Id = byteid1,
					Children = new List<ByteChildTbl>()
					{
						new ByteChildTbl() { Id = Guid.NewGuid(), ParentId = byteid1},
						new ByteChildTbl { Id = Guid.NewGuid(), ParentId = byteid1 }
					}
				}, references: true);
				var byteObj1 = db.LoadSelect<ByteTbl>(x => x.Id == byteid1, x => x.Children).First();
				Assert.AreNotEqual(default(byte[]), byteObj1.RowVersion);
				Assert.AreNotEqual(default(byte[]), byteObj1.Children[0].RowVersion);
				Assert.AreNotEqual(default(byte[]), byteObj1.Children[1].RowVersion);

				var byteid2 = Guid.NewGuid();
				db.Save(new ByteTbl
				{
					Id = byteid2,
					Children = new List<ByteChildTbl>()
					{
						new ByteChildTbl {Id = Guid.NewGuid(), ParentId = byteid2},
						new ByteChildTbl {Id = Guid.NewGuid(), ParentId = byteid2}
					}
				}, references: true);
				var byteObj2 = db.LoadSelect<ByteTbl>(x => x.Id == byteid2, x => x.Children).First();
				Assert.AreNotEqual(default(byte[]), byteObj2.RowVersion);
				Assert.AreNotEqual(default(byte[]), byteObj2.Children[0].RowVersion);
				Assert.AreNotEqual(default(byte[]), byteObj2.Children[1].RowVersion);

				// COnfirm multip select logic works
				var q = db.From<ByteTbl>()
						.Join<ByteTbl, ByteChildTbl>()
					;
				var results = db.SelectMulti<ByteTbl, ByteChildTbl>(q);
				foreach (var tuple in results)
				{
					ByteTbl parent = tuple.Item1;
					ByteChildTbl child = tuple.Item2;
					Assert.AreNotEqual(default(byte[]), parent.RowVersion);
					Assert.AreNotEqual(default(byte[]), child.RowVersion);
				}
			}
			
			{
				// test the multi select using dapper
				var byteid2 = Guid.NewGuid();
				db.Save(new ByteTbl
				{
					Id = byteid2,
					Children = new List<ByteChildTbl>()
					{
						new ByteChildTbl {Id = Guid.NewGuid(), ParentId = byteid2},
						new ByteChildTbl {Id = Guid.NewGuid(), ParentId = byteid2}
					}
				}, references: true);

				var q = db.From<ByteTbl>()
					.Join<ByteTbl, ByteChildTbl>()
					.Select("*");

				using var multi = db.QueryMultiple(q.ToSelectStatement());
				var results = multi.Read<ByteTbl, ByteChildTbl,
					Tuple<ByteTbl, ByteChildTbl>>(Tuple.Create).ToList();

				foreach (var tuple in results)
				{
					ByteTbl parent = tuple.Item1;
					ByteChildTbl child = tuple.Item2;
					Assert.AreNotEqual(default(byte[]), parent.RowVersion);
					Assert.AreNotEqual(default(byte[]), child.RowVersion);
				}
				Assert.True(results.Count > 0);
			}
			//Console.WriteLine("hit a key to end test");
			//Console.ReadLine();
		}
	}
}