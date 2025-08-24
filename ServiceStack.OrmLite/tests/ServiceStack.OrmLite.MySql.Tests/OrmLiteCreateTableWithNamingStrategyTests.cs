using System;
using System.Diagnostics;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.MySql.Tests
{

	[TestFixture]
	public class OrmLiteCreateTableWithNamingStrategyTests 
		: OrmLiteTestBase
	{

		[Test]
		public void Can_create_TableWithNamingStrategy_table_prefix()
		{
            using (new TemporaryNamingStrategy(new PrefixNamingStrategy { TablePrefix = "tab_", ColumnPrefix = "col_" }))
			using (var db = OpenDbConnection())
			{
				db.CreateTable<ModelWithOnlyStringFields>(true);
			}
		}

		[Test]
		public void Can_create_TableWithNamingStrategy_table_lowered()
		{
            using (new TemporaryNamingStrategy(new LowercaseNamingStrategy()))
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);
            }
		}


		[Test]
		public void Can_create_TableWithNamingStrategy_table_nameUnderscoreCoumpound()
		{
            using (new TemporaryNamingStrategy(new UnderscoreSeparatedCompoundNamingStrategy()))
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);
            }
		}

		[Test]
		public void Can_get_data_from_TableWithNamingStrategy_with_GetById()
		{
            using (new TemporaryNamingStrategy(new PrefixNamingStrategy { TablePrefix = "tab_", ColumnPrefix = "col_" }))
            using (var db = OpenDbConnection())
			{
				db.CreateTable<ModelWithOnlyStringFields>(true);
				ModelWithOnlyStringFields m = new ModelWithOnlyStringFields() { Id= "999", AlbumId = "112", AlbumName="ElectroShip", Name = "MyNameIsBatman"};

				db.Save<ModelWithOnlyStringFields>(m);
                var modelFromDb = db.SingleById<ModelWithOnlyStringFields>("999");

				Assert.AreEqual(m.Name, modelFromDb.Name);
			}
		}


		[Test]
		public void Can_get_data_from_TableWithNamingStrategy_with_query_by_example()
		{
            using (new TemporaryNamingStrategy(new PrefixNamingStrategy { TablePrefix = "tab_", ColumnPrefix = "col_" }))
            using (var db = OpenDbConnection())
			{
				db.CreateTable<ModelWithOnlyStringFields>(true);
				ModelWithOnlyStringFields m = new ModelWithOnlyStringFields() { Id = "998", AlbumId = "112", AlbumName = "ElectroShip", Name = "QueryByExample" };

				db.Save<ModelWithOnlyStringFields>(m);
				var modelFromDb = db.Where<ModelWithOnlyStringFields>(new { Name = "QueryByExample" })[0];

				Assert.AreEqual(m.Name, modelFromDb.Name);
			}
		}
		
		
		[Test]
		public void Can_get_data_from_TableWithNamingStrategy_AfterChangingNamingStrategy()
		{			
            using (new TemporaryNamingStrategy(new PrefixNamingStrategy { TablePrefix = "tab_", ColumnPrefix = "col_" }))
            using (var db = OpenDbConnection())
			{
				db.CreateTable<ModelWithOnlyStringFields>(true);
				ModelWithOnlyStringFields m = new ModelWithOnlyStringFields() { Id = "998", AlbumId = "112", AlbumName = "ElectroShip", Name = "QueryByExample" };

				db.Save<ModelWithOnlyStringFields>(m);
				var modelFromDb = db.Where<ModelWithOnlyStringFields>(new { Name = "QueryByExample" })[0];

				Assert.AreEqual(m.Name, modelFromDb.Name);

                modelFromDb = db.SingleById<ModelWithOnlyStringFields>("998");
				Assert.AreEqual(m.Name, modelFromDb.Name);
				
			}
			
			using (var db = OpenDbConnection())
			{
				db.CreateTable<ModelWithOnlyStringFields>(true);
				ModelWithOnlyStringFields m = new ModelWithOnlyStringFields() { Id = "998", AlbumId = "112", AlbumName = "ElectroShip", Name = "QueryByExample" };

				db.Save<ModelWithOnlyStringFields>(m);
				var modelFromDb = db.Where<ModelWithOnlyStringFields>(new { Name = "QueryByExample" })[0];

				Assert.AreEqual(m.Name, modelFromDb.Name);

                modelFromDb = db.SingleById<ModelWithOnlyStringFields>("998");
				Assert.AreEqual(m.Name, modelFromDb.Name);	
			}
			
            using (new TemporaryNamingStrategy(new PrefixNamingStrategy { TablePrefix = "tab_", ColumnPrefix = "col_" }))
            using (var db = OpenDbConnection())
			{
				db.CreateTable<ModelWithOnlyStringFields>(true);
				ModelWithOnlyStringFields m = new ModelWithOnlyStringFields() { Id = "998", AlbumId = "112", AlbumName = "ElectroShip", Name = "QueryByExample" };

				db.Save<ModelWithOnlyStringFields>(m);
				var modelFromDb = db.Where<ModelWithOnlyStringFields>(new { Name = "QueryByExample" })[0];

				Assert.AreEqual(m.Name, modelFromDb.Name);

                modelFromDb = db.SingleById<ModelWithOnlyStringFields>("998");
				Assert.AreEqual(m.Name, modelFromDb.Name);
			}
		}
	}

    public class TemporaryNamingStrategy : IDisposable
    {
        private readonly IOrmLiteDialectProvider _dialectProvider;
        private readonly INamingStrategy _previous;
        private bool _disposed;

        public TemporaryNamingStrategy(INamingStrategy temporary)
        {
            _dialectProvider = OrmLiteConfig.DialectProvider;
            _previous = _dialectProvider.NamingStrategy;
            _dialectProvider.NamingStrategy = temporary;
        }

#if DEBUG
        ~TemporaryNamingStrategy()
        {
            Debug.Assert(_disposed, "TemporaryNamingStrategy was not disposed of - previous naming strategy was not restored");
        }
#endif

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _dialectProvider.NamingStrategy = _previous;

#if DEBUG
                GC.SuppressFinalize(this);
#endif
            }
        }
    }
    public class PrefixNamingStrategy : OrmLiteNamingStrategyBase
	{

		public string TablePrefix { get; set; }

		public string ColumnPrefix { get; set; }

		public override string GetTableName(string name)
		{
			return TablePrefix + name;
		}

		public override string GetColumnName(string name)
		{
			return ColumnPrefix + name;
		}

	}

	public class LowercaseNamingStrategy : OrmLiteNamingStrategyBase
	{

		public override string GetTableName(string name)
		{
			return name.ToLower();
		}

		public override string GetColumnName(string name)
		{
			return name.ToLower();
		}

	}

	public class UnderscoreSeparatedCompoundNamingStrategy : OrmLiteNamingStrategyBase
	{

		public override string GetTableName(string name)
		{
			return toUnderscoreSeparatedCompound(name);
		}

		public override string GetColumnName(string name)
		{
			return toUnderscoreSeparatedCompound(name);
		}


		string toUnderscoreSeparatedCompound(string name)
		{

			string r = char.ToLower(name[0]).ToString();

			for (int i = 1; i < name.Length; i++)
			{
				char c = name[i];
				if (char.IsUpper(name[i]))
				{
					r += "_";
					r += char.ToLower(name[i]);
				}
				else
				{
					r += name[i];
				}
			}
			return r;
		}

	}

}