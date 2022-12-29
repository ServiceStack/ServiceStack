using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues
{
    public class TestResultsFilter : OrmLiteResultsFilter
    {
        public TestResultsFilter()
        {
            ResultsFn = TestFilter;
            //RefResultsFn = TestFilter; //used to mock LoadSelect<T> child references
        }

        private IEnumerable TestFilter(IDbCommand dbCmd, Type refType)
        {
            var typedResults = dbCmd.ExecuteReader().ConvertToList(dbCmd.GetDialectProvider(), refType);
            return typedResults;
        }
    }

    public class Parent
    {
        [AutoIncrement]
        public long Id { get; set; }
        public string Content { get; set; }

        [Reference]
        public List<Child> Children { get; set; }
    }


    public class Child
    {
        [AutoIncrement]
        public long Id { get; set; }
        public string Content { get; set; }

        [References(typeof(Parent))]
        public long? ParentId { get; set; }
        [Reference]
        public Parent Parent { get; set; }
    }

    [TestFixtureOrmLite]
    public class LoadSelectResultFilterIssue : OrmLiteProvidersTestBase
    {
        public LoadSelectResultFilterIssue(DialectContext context) : base(context) {}

        [Test]
        public void Can_use_results_filter_in_LoadSelect()
        {
            using (var db = OpenDbConnection())
            {
                try
                {
                    db.DropTable<Parent>();
                    db.DropTable<Child>();
                }
                catch (Exception e)
                {
                    db.DropTable<Child>();
                    db.DropTable<Parent>();
                }

                db.CreateTable<Parent>();
                db.CreateTable<Child>();

                var newParent = new Parent
                {
                    Content = "Test Parent",
                    Children = new List<Child>
                    {
                        new Child {Content = "Test Child 1"},
                        new Child {Content = "Test Child 2"}
                    }
                };

                db.Save(newParent, references: true);
            }

            using (new TestResultsFilter())
            using (var db = OpenDbConnection())
            {
                var savedParents = db.LoadSelect<Parent>();
                Assert.That(savedParents.Count, Is.EqualTo(1));
            }
        }
    }
}