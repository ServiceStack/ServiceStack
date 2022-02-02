using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [TestFixtureOrmLite]
    public class DynamicResultsNamingStrategyTest : OrmLiteProvidersTestBase
    {
        public DynamicResultsNamingStrategyTest(DialectContext context) : base(context) {}

        public class Menu : EntityBase<Menu>
        {
            [AutoIncrement]
            public int Id { get; set; }

            [ForeignKey(typeof(Menu))]
            public int? ParentId { get; set; }

//            [Required]
//            public MenuType Type { get; set; }

            [Required, StringLength(100)]
            public string Name { get; set; }

            [StringLength(100)]
            public string Form { get; set; }

            [StringLength(50)]
            public string Icon { get; set; }

            [StringLength(1000)]
            public string Style { get; set; }

//            [ForeignKey(typeof(User))]
            public int? UserId { get; set; }
        }
        
        public abstract class EntityBase<T> // : IEntity<T>
        {
            [Required, Default(typeof(bool), "{FALSE}")]
            public bool IsDeleted { get; set; }
            [Required, Default(typeof(bool), "{TRUE}")]
            public bool IsActive { get; set; } = true;
            public int? Position { get; set; }
            //public ulong RowVersion { get; set; }
            public Guid RecId { get; set; }
        }
        
        public class DatabaseNamingStrategy : OrmLiteNamingStrategyBase
        {
            public override string GetTableName(string name)
            {
                return ToUnderscoreSeparated(name);
            }

            public override string GetColumnName(string name)
            {
                return ToUnderscoreSeparated(name);
            }


            string ToUnderscoreSeparated(string name)
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
        
        [Test]
        public void Can_select_dynamic_results_from_custom_NamingStrategy()
        {
            OrmLiteConfig.BeforeExecFilter = dbCmd => Console.WriteLine(dbCmd.GetDebugString());

            var hold = SqliteDialect.Provider.NamingStrategy; 
            SqliteDialect.Provider.NamingStrategy = new DatabaseNamingStrategy();
            
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Menu>();

                var rows = new[] {
                    new Menu {
                        Name = "Test List",
                        RecId = new Guid("2F96233B-152E-4D20-BE08-5633431A9EBC")
                    }
                };
                
                db.InsertAll(rows);
                
                var q = db.From<Menu>().Select(x => new { x.Id, x.RecId, x.Name });
                var results = db.Select<(int id, Guid recId, string name)>(q);

                var expected = rows[0];
                Assert.That(results[0].id, Is.EqualTo(1));
                Assert.That(results[0].recId, Is.EqualTo(expected.RecId));
                Assert.That(results[0].name, Is.EqualTo(expected.Name));
            }

            SqliteDialect.Provider.NamingStrategy = hold;
        }
    }
}