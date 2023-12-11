using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [TestFixtureOrmLite]
    public class MappingFieldIssues : OrmLiteProvidersTestBase
    {
        public MappingFieldIssues(DialectContext context) : base(context) {}

        public class OriginalTable
        {
            public int Id { get; set; }
            public DateTime SaleDate { get; set; }
            public TimeSpan SaleTime { get; set; }
            public int NumberOfItems { get; set; }
            public decimal Amount { get; set; }
        }

        [Alias("OriginalTable")]
        public class MismatchTable
        {
            public int Id { get; set; }
            public DateTime SaleDate { get; set; }
            public DateTime SaleTime { get; set; }
            public int NumberOfItems { get; set; }
            public decimal Amount { get; set; }
        }

        [Test]
        public void Does_map_remaining_columns_after_failed_mapping()
        {
            var hold = OrmLiteConfig.ThrowOnError; 
            OrmLiteConfig.ThrowOnError = false;
            
            using var db = OpenDbConnection();
            db.DropAndCreateTable<OriginalTable>();
            db.Insert(new OriginalTable
            {
                Id = 1,
                SaleDate = new DateTime(2001, 01, 01),
                SaleTime = new TimeSpan(1,1,1,1),
                NumberOfItems = 2,
                Amount = 3
            });

            var result = db.SingleById<MismatchTable>(1);
            Assert.That(result.NumberOfItems, Is.EqualTo(2));
            Assert.That(result.Amount, Is.EqualTo(3));
            
            OrmLiteConfig.ThrowOnError = hold;
        }

    }
}