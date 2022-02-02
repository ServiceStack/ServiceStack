using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLite]
    public class 
        
        SqlBuilderTests : OrmLiteProvidersTestBase
    {
        public SqlBuilderTests(DialectContext context) : base(context) {}

        [Alias("UsersSqlBuilder")]
        public class UsersSqlBuilder 
        {
            [AutoIncrement]
            public int Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
        }

        [Test]
        public void BuilderSelectClause()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<UsersSqlBuilder>();
                var rand = new Random(8675309);
                var data = new List<UsersSqlBuilder>();
                for (var i = 0; i < 100; i++)
                {
                    var nU = new UsersSqlBuilder {Age = rand.Next(70), Id = i, Name = Guid.NewGuid().ToString()};
                    data.Add(nU);
                    nU.Id = (int) db.Insert(nU, selectIdentity: true);
                }

                var table = db.GetDialectProvider().GetTableName(nameof(UsersSqlBuilder));
                var builder = new SqlBuilder();
                var justId = builder.AddTemplate($"SELECT /**select**/ FROM {table}");
                var all = builder.AddTemplate($"SELECT /**select**/, Name, Age FROM {table}");

                builder.Select("Id");

                var ids = db.Column<int>(justId.RawSql, justId.Parameters);
                var users = db.Select<UsersSqlBuilder>(all.RawSql, all.Parameters);

                foreach (var u in data)
                {
                    Assert.That(ids.Any(i => u.Id == i), "Missing ids in select");
                    Assert.That(users.Any(a => a.Id == u.Id && a.Name == u.Name && a.Age == u.Age),
                        "Missing users in select");
                }
            }
        }

        [Test]
        public void BuilderTemplateWOComposition()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<UsersSqlBuilder>();
                
                var builder = new SqlBuilder();
                var table = db.GetDialectProvider().GetTableName(nameof(UsersSqlBuilder));
                var template = builder.AddTemplate(
                    $"SELECT COUNT(*) FROM {table} WHERE Age = {DialectProvider.ParamString}age",
                    new {age = 5});

                if (template.RawSql == null) throw new Exception("RawSql null");
                if (template.Parameters == null) throw new Exception("Parameters null");

                db.Insert(new UsersSqlBuilder {Age = 5, Name = "Testy McTestington"});

                Assert.That(db.Scalar<int>(template.RawSql, template.Parameters), Is.EqualTo(1));
            }
        }         
    }
}