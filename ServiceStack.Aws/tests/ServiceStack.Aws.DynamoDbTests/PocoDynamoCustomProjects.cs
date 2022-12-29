using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class PocoDynamoCustomProjects : DynamoTestBase
    {
        [Test]
        public void Can_select_single_Name_field()
        {
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));
            db.RegisterTable<Customer>();
            db.InitSchema();
                
            db.PutItems(new[] {
                new Customer { Name = "John", Age = 27, Nationality = "Australian" }, 
                new Customer { Name = "Jill", Age = 27, Nationality = "USA" }, 
            });
                
            var q = db.FromScan<Customer>();
            var results = q.Select<Customer>(x => new { x.Name }).Exec();
                
            Assert.That(results.All(x => x.Nationality == null));
            Assert.That(results.All(x => x.Age == null));
            Assert.That(results.Map(x => x.Name), Is.EquivalentTo(new[]{ "John", "Jill" }));
        }

        public class ReservedWords
        {
            [PrimaryKey]
            public string Path { get; set; }
        }
        
        [Test]
        public void Does_include_aliases_for_queries_using_reserved_words()
        {
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));
            db.RegisterTable<ReservedWords>();
            db.InitSchema();

            var results = db.FromScan<ReservedWords>(f => f.Path.StartsWith("foo")).Exec().ToList();
        }
        
        public class FilePathIndex : ILocalIndex<File>
        {
            public Guid Account { get; set; }
            public string S3Location { get; set; }
            [Index]
            public string Path { get; set; }
            public DateTime ModifiedUtc { get; set; }
        }

        [References(typeof(FilePathIndex))]
        [CompositeKey("Account", "S3Location")]
        public class File
        {
            public Guid Account { get; set; }
            public string S3Location { get; set; }

            public string Path { get; set; }
            public DateTime ModifiedUtc { get; set; }
        }
         
        [Test]
        public void Can_query_reserved_word_using_Equals_method()
        {
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));
            db.RegisterTable<File>();
            db.InitSchema();

            var existingFile = db.FromQueryIndex<FilePathIndex>(f => f.Account == Guid.NewGuid() && f.Path.Equals("/the/path")).Exec().ToList();
        }
       
        [Test]
        public void Can_update_with_Reserved_word()
        {
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));
            db.RegisterTable<File>();
            db.InitSchema();

            var request = new File {Account = Guid.NewGuid(), S3Location = "S3", Path = "/the/path"};
            db.PutItem(request);
            
            var updateFile = db.UpdateExpression<File>(request.Account, request.S3Location)
                .Set(() => new File
                {
                    ModifiedUtc = DateTime.UtcNow
                })
                .Condition(f => f.Path.StartsWith("/the/path"));

            db.UpdateItem(updateFile);
        }

    }
}