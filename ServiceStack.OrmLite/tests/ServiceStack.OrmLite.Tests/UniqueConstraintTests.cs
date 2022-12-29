using System;
using System.Text;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests
{
    public class UniqueTest
    {
        [AutoIncrement]
        public int Id { get; set; }

        [Unique]
        public string Field { get; set; }
    }

    [UniqueConstraint(nameof(Field2), nameof(Field3))]
    public class UniqueTest2
    {
        [AutoIncrement]
        public int Id { get; set; }

        public string Field2 { get; set; }
        public string Field3 { get; set; }
    }

    [UniqueConstraint(nameof(Field4), nameof(Field5), nameof(Field6), Name = "UC_CUSTOM")]
    public class UniqueTest3
    {
        [AutoIncrement]
        public int Id { get; set; }

        public string Field4 { get; set; }
        public string Field5 { get; set; }
        public string Field6 { get; set; }
    }

    [TestFixtureOrmLite]
    public class UniqueConstraintTests : OrmLiteProvidersTestBase
    {
        public UniqueConstraintTests(DialectContext context) : base(context) {}

        [Test]
        public void Does_add_individual_Constraints()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<UniqueTest>();

                db.Insert(new UniqueTest { Field = "A" });
                db.Insert(new UniqueTest { Field = "B" });

                try
                {
                    db.Insert(new UniqueTest { Field = "B" });
                    Assert.Fail("Should throw");
                }
                catch (Exception ex)
                {
                    ex.Message.Print();
                    Assert.That(ex.Message.ToLower().Contains("unique") || ex.Message.ToLower().Contains("duplicate"));
                }
            }
        }

        [Test]
        public void Does_add_multiple_column_unique_constraint()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<UniqueTest2>();

                db.Insert(new UniqueTest2 { Field2 = "A", Field3 = "A" });
                db.Insert(new UniqueTest2 { Field2 = "A", Field3 = "B" });

                try
                {
                    db.Insert(new UniqueTest2 { Field2 = "A", Field3 = "B" });
                    Assert.Fail("Should throw");
                }
                catch (Exception ex)
                {
                    ex.Message.Print();
                    Assert.That(ex.Message.ToLower().Contains("unique") || ex.Message.ToLower().Contains("duplicate"));
                }
            }
        }

        [Test]
        public void Does_add_multiple_column_unique_constraint_with_custom_name()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<UniqueTest3>();

                var createSql = DialectProvider.ToCreateTableStatement(typeof(UniqueTest3));
                Assert.That(createSql.ToUpper(), Does.Contain("CONSTRAINT UC_CUSTOM UNIQUE"));

                db.Insert(new UniqueTest3 { Field4 = "A", Field5 = "A", Field6 = "A" });
                db.Insert(new UniqueTest3 { Field4 = "A", Field5 = "A", Field6 = "B" });

                try
                {
                    db.Insert(new UniqueTest3 { Field4 = "A", Field5 = "A", Field6 = "B" });
                    Assert.Fail("Should throw");
                }
                catch (Exception ex)
                {
                    ex.Message.Print();
                    Assert.That(ex.Message.ToLower().Contains("unique") || ex.Message.ToLower().Contains("duplicate"));
                }
            }
        }

        [UniqueConstraint(nameof(Environment), nameof(Name))]
        public class User
        {
            [AutoId]
            public Guid Id { get; set; }
            public Guid Environment { get; set; }
            public string Name { get; set; }
        }
        
        [Test]
        public void Can_create_User_table_with_Unique_constraints()
        {
            using (var db = OpenDbConnection())
            {
                db.DropTable<User>();
                db.CreateTableIfNotExists<User>();
            }
        }
    }
    

    public class UniqueTest4 : IHasId<int>
    {
        [AutoIncrement, Index(NonClustered = true)]
        public int Id { get; set; }

        [Index(Unique = true)]
        [Required]
        [StringLength(100)]
        public string UniqueDefault { get; set; }

        [Index(Unique = true, Clustered = true)]
        [Required]
        [StringLength(200)]
        public string UniqueClustered { get; set; }

        [Index(Unique = true, NonClustered = true)]
        [Required]
        [StringLength(300)]
        public string UniqueNonClustered { get; set; }
    }    

    public class SqlServer2012UniqueTests : OrmLiteTestBase
    {
        public SqlServer2012UniqueTests() : base(Dialect.SqlServer2012) { }

        [Test]
        public void Does_create_unique_non_null_constraint()
        {
            var sb = new StringBuilder();
            OrmLiteConfig.BeforeExecFilter = cmd => sb.AppendLine(cmd.GetDebugString());
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<UniqueTest4>();
            }
            
//            sb.ToString().Print();
            var sql = sb.ToString();
            Assert.That(sql, Does.Contain("PRIMARY KEY NONCLUSTERED"));
            Assert.That(sql, Does.Contain("VARCHAR(100) NOT NULL,"));
            Assert.That(sql, Does.Contain("VARCHAR(200) NOT NULL,"));
            Assert.That(sql, Does.Contain("VARCHAR(300) NOT NULL"));
            Assert.That(sql, Does.Contain("CREATE UNIQUE INDEX "));
            Assert.That(sql, Does.Contain("CREATE UNIQUE CLUSTERED INDEX "));
            Assert.That(sql, Does.Contain("CREATE UNIQUE NONCLUSTERED INDEX "));
        }
    }

    public class SqlServer2014UniqueTests : OrmLiteTestBase
    {
        public SqlServer2014UniqueTests() : base(Dialect.SqlServer2014) { }

        [Test]
        public void Does_create_unique_non_null_constraint()
        {
            var sb = new StringBuilder();
            OrmLiteConfig.BeforeExecFilter = cmd => sb.AppendLine(cmd.GetDebugString());
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<UniqueTest4>();
            }
            
//            sb.ToString().Print();
            var sql = sb.ToString();
            Assert.That(sql, Does.Contain("PRIMARY KEY NONCLUSTERED"));
            Assert.That(sql, Does.Contain("VARCHAR(100) NOT NULL,"));
            Assert.That(sql, Does.Contain("VARCHAR(200) NOT NULL,"));
            Assert.That(sql, Does.Contain("VARCHAR(300) NOT NULL"));
            Assert.That(sql, Does.Contain("CREATE UNIQUE INDEX "));
            Assert.That(sql, Does.Contain("CREATE UNIQUE CLUSTERED INDEX "));
            Assert.That(sql, Does.Contain("CREATE UNIQUE NONCLUSTERED INDEX "));
        }
    }
    
}