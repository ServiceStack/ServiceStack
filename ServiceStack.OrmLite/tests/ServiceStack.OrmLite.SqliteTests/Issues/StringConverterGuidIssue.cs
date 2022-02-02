using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Tests.Issues
{
    public class StringConverterGuidIssue : OrmLiteTestBase
    {
        [Alias("location")]
        public class DbPoco
        {
            [Alias("poco_id"), PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            [Alias("poco_uuid")]
            public Guid UniqueId { get; set; } = Guid.NewGuid();
        }

        /// <summary>
        /// Example Dto with Guid as a string.
        /// </summary>
        public class Dto
        {
            public string Id { get; set; }
            
            // Fails when using ServiceStack.OrmLite.Sqlite / System.Data.SQLite.Core due to reader.GetValue() on Guid:
            // https://forums.servicestack.net/t/ormlite-sqllite-stringconverter-behavior-change-5-4-5-6/
            // Passes when using ServiceStack.OrmLite.Sqlite.Data / Microsoft.Data.SQLite
            public string UniqueId { get; set; }
        }

        public class MySqliteStringConverter : StringConverter
        {
            public override object FromDbValue(Type fieldType, object value)
            {
                object result;

                if (value is byte[] byteValue)
                {
                    var res = new Guid(byteValue).ToString();
                    result = res;
                }
                else
                {
                    result = base.FromDbValue(fieldType, value);
                }

                return result;
            }
        }
        
        [Test]
        public void Does_convert_Guid_with_Custom_String_Converter()
        {
            var dialectProvider = SqliteDialect.Provider;
            dialectProvider.RegisterConverter<string>(new MySqliteStringConverter());
            var dbFactory = new OrmLiteConnectionFactory(":memory:", dialectProvider);

            var uuid = Guid.NewGuid();
            using (var db = dbFactory.Open())
            {
                db.CreateTable(false, typeof(DbPoco));
                db.Insert(new DbPoco { Id = 1, UniqueId = uuid });

                var result = db.Single<Dto>(db.From<DbPoco>().Where(poco => poco.UniqueId == uuid));

                Assert.That(result.UniqueId, Is.EqualTo(uuid.ToString()));                
//                Assert.That(result.UniqueId, Is.EqualTo(uuid));                
            }
        }
        
    }
}