using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.PostgreSQL.Tests
{

    public class CustomSqlUser
    {
        [AutoIncrement]
        public int Id { get; set; }

        public string Email { get; set; }

        [CustomInsert("crypt({0}, gen_salt('bf'))"),
         CustomUpdate("crypt({0}, gen_salt('bf'))")]
        public string Password { get; set; }
    }

    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class CustomSqlTests : OrmLiteProvidersTestBase
    {
        public CustomSqlTests(DialectContext context) : base(context) { }

        [Test]
        public void Can_insert_CustomInsert()
        {
            OrmLiteUtils.PrintSql();

            using var db = OpenDbConnection();
            // db.ExecuteSql("CREATE extension pgcrypto");
            db.DropAndCreateTable<CustomSqlUser>();

            var user = new CustomSqlUser {
                Email = "user@email.com", 
                Password = "secret"
            };
            db.Insert(user);

            var escapedSecret = db.Dialect().GetQuotedValue("secret");
            var q = db.From<CustomSqlUser>()
                .Where(x => x.Password == Sql.Custom($"crypt({escapedSecret}, password)"));
            var row = db.Single(q);
            Assert.That(row.Email, Is.EqualTo(user.Email));

            row = db.Single(db.From<CustomSqlUser>()
                .Where(x => x.Password == Sql.Custom("crypt({0}, password)"),"secret"));
            Assert.That(row.Email, Is.EqualTo(user.Email));

            row = db.Single(db.From<CustomSqlUser>()
                .Where("password = crypt({0}, password)", "secret"));
            Assert.That(row.Email, Is.EqualTo(user.Email));

            db.UpdateOnly(() => new CustomSqlUser {Password = "newsecret"},
                where: x => x.Email == user.Email);

            q = db.From<CustomSqlUser>()
                .Where(x => x.Password == Sql.Custom("crypt('newsecret', password)"));
            row = db.Single(q);
            Assert.That(row.Email, Is.EqualTo(user.Email));
        }
    }
}