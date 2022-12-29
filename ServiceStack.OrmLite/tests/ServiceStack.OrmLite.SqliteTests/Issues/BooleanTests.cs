using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [Alias("users")]
    public class User
    {
        [Alias("id"), PrimaryKey, Required, AutoIncrement]
        public int Id { get; set; }

        [Alias("username")]
        public string Username { get; set; }

        [Alias("password")]
        public string Password { get; set; }

        [Alias("system_admin"), Required]
        public bool IsSystemAdmin { get; set; }

        [Alias("system_user"), Required]
        public bool IsSystemUser { get; set; }

        [Alias("is_admin"), Required]
        public bool IsAdmin { get; set; }

        [Alias("notes")]
        public string Notes { get; set; }
    }
    
    public class BooleanTests
    {
        [Test]
        public async Task Can_create_user_with_BOOLEAN_columns()
        {
            var factory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
            using var db = await factory.OpenAsync();

            db.ExecuteSql(@"
CREATE TABLE IF NOT EXISTS `users` (
`id`	INTEGER NOT NULL,
`username`	VARCHAR,
`system_user`	BOOLEAN,
`system_admin`	BOOLEAN,
`is_admin`	BOOLEAN,
`password`	VARCHAR,
`notes`	VARCHAR,
PRIMARY KEY(`id`)
);");

            var row = new User {
                Notes = "notes", 
                IsAdmin = true, 
                Username = "user", 
                Password = "pass"
            };

            await db.SaveAsync(row);

            var dbRow = await db.SingleByIdAsync<User>(row.Id);
            Assert.That(dbRow.Id, Is.EqualTo(row.Id));
            Assert.That(dbRow.Notes, Is.EqualTo(row.Notes));
            Assert.That(dbRow.IsAdmin, Is.EqualTo(row.IsAdmin));
            Assert.That(dbRow.Username, Is.EqualTo(row.Username));
            Assert.That(dbRow.Password, Is.EqualTo(row.Password));
        }
    }
}