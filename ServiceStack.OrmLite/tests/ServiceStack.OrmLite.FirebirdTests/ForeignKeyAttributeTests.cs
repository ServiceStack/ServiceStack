using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.FirebirdTests
{
	[TestFixture]
	public class ForeignKeyAttributeTests : OrmLiteTestBase
	{
		[OneTimeSetUp]
		public void Setup()
		{
			using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<ReferencedType>(true);
			}
		}
		
		[Test]
		public void CanCreateSimpleForeignKey()
		{
			using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<TypeWithSimpleForeignKey>(true);
			}
		}
		
		[Test]
		public void CanCreateForeignWithOnDeleteCascade()
		{
			using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<TypeWithOnDeleteCascade>(true);
			}
		}
		
		[Test]
		public void CascadesOnDelete()
		{
			using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<TypeWithOnDeleteCascade>(true);
				
				db.Save(new ReferencedType { Id = 1 });
				db.Save(new TypeWithOnDeleteCascade { RefId = 1 });
				
				Assert.AreEqual(1, db.Select<ReferencedType>().Count);
				Assert.AreEqual(1, db.Select<TypeWithOnDeleteCascade>().Count);
				
				db.Delete<ReferencedType>(r => r.Id == 1);
				
				Assert.AreEqual(0, db.Select<ReferencedType>().Count);
				Assert.AreEqual(0, db.Select<TypeWithOnDeleteCascade>().Count);
			}
		}
		
		[Test]
		public void CanCreateForeignWithOnDeleteCascadeAndOnUpdateCascade()
		{
			using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<TypeWithOnDeleteAndUpdateCascade>(true);
			}
		}
		
		[Test]
		public void CanCreateForeignWithOnDeleteNoAction()
		{
			using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<TypeWithOnDeleteNoAction>(true);
			}
		}
		
		[Test]
		public void CanCreateForeignWithOnDeleteRestrict()
		{
			using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<TypeWithOnDeleteRestrict>(true);
			}
		}
		

		[Test]
		public void CanCreateForeignWithOnDeleteSetDefault()
		{
			using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<TypeWithOnDeleteSetDefault>(true);
			}
		}
		
		[Test]
		public void CanCreateForeignWithOnDeleteSetNull()
		{
			using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.CreateTable<TypeWithOnDeleteSetNull>(true);
			}
		}
		
		[OneTimeTearDown]
		public void TearDwon()
		{
			using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
			{
				db.DropTable<TypeWithOnDeleteAndUpdateCascade>();
				db.DropTable<TypeWithOnDeleteSetNull>();
				db.DropTable<TypeWithOnDeleteSetDefault>();
				db.DropTable<TypeWithOnDeleteRestrict>();
				db.DropTable<TypeWithOnDeleteNoAction>();
				db.DropTable<TypeWithOnDeleteCascade>();
				db.DropTable<TypeWithSimpleForeignKey>();
				db.DropTable<ReferencedType>();
			}
		}
	}
	
	public class ReferencedType
	{
		public int Id { get; set; }
	}
	

	[Alias("TWSKF")]
	public class TypeWithSimpleForeignKey
	{
		[AutoIncrement]
		public int Id { get; set; }
		[References(typeof(ReferencedType))]
		public int RefId { get; set; }
	}

	[Alias("TWODC")]
	public class TypeWithOnDeleteCascade
	{
		[AutoIncrement]
		public int Id { get; set; }
		
		[ForeignKey(typeof(ReferencedType), OnDelete = "CASCADE", ForeignKeyName="FK_DC")]
		public int? RefId { get; set; }
	}

	[Alias("TWODUC")]
	public class TypeWithOnDeleteAndUpdateCascade
	{
		[AutoIncrement]
		public int Id { get; set; }
		
		[ForeignKey(typeof(ReferencedType), OnDelete = "CASCADE", OnUpdate = "CASCADE", ForeignKeyName="FK_DC_UC")]
		public int? RefId { get; set; }
	}

	[Alias("TWODNA")]
	public class TypeWithOnDeleteNoAction
	{
		[AutoIncrement]
		public int Id { get; set; }
		
		[ForeignKey(typeof(ReferencedType), OnDelete = "NO ACTION", ForeignKeyName="FK_DNA")]
		public int? RefId { get; set; }
	}

	[Alias("TWODNR")]
	public class TypeWithOnDeleteRestrict
	{
		[AutoIncrement]
		public int Id { get; set; }
		
		[ForeignKey(typeof(ReferencedType), OnDelete = "RESTRICT", ForeignKeyName="FK_DR")]
		public int? RefId { get; set; }
	}

	[Alias("TWODDF")]
	public class TypeWithOnDeleteSetDefault
	{
		[AutoIncrement]
		public int Id { get; set; }
		
		[Default(typeof(int), "17")]
		[ForeignKey(typeof(ReferencedType), OnDelete = "SET DEFAULT", ForeignKeyName="FK_DDF")]
		public int RefId { get; set; }
	}

	[Alias("TWODSN")]
	public class TypeWithOnDeleteSetNull
	{
		[AutoIncrement]
		public int Id { get; set; }
		
		[ForeignKey(typeof(ReferencedType), OnDelete = "SET NULL", ForeignKeyName="FKSN")]
		public int? RefId { get; set; }
	}
}