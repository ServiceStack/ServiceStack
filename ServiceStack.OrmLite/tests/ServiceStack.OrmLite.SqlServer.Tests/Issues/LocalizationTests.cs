using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;

namespace ServiceStack.OrmLite.SqlServerTests.Issues
{
    public class LocalizationTest
    {
        [AutoIncrement]
        public int Id { get; set; }
        
        public string Field { get; set; }
    }
       
    public class LocalizationTests : OrmLiteTestBase
    {
        public LocalizationTests()
        {
            LogManager.LogFactory = new ConsoleLogFactory(debugEnabled:true);            
        }

        [Test]
        public void Can_save_arabic_numbers()
        {
            OrmLiteConfig.DialectProvider.GetStringConverter().UseUnicode = true;
            
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<LocalizationTest>();

                var model = new LocalizationTest {
                    Field = "۳۹۹۳"
                };

                db.Save(model);

                var row = db.SingleById<LocalizationTest>(model.Id);
                
                Assert.That(row.Field, Is.EqualTo(model.Field));
            }
            
            OrmLiteConfig.DialectProvider.GetStringConverter().UseUnicode = false;
        }        
    }
}