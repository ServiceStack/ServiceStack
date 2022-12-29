// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System.Linq;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Expression
{
    [TestFixtureOrmLite]
    public class MethodExpressionTests : ExpressionsTestBase
    {
        public MethodExpressionTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_select_ints_using_array_contains()
        {
            var ints = new[] { 1, 20, 30 };
            var nullableInts = new int?[] { 5, 30, null, 20 };

            using (var db = OpenDbConnection())
            {
                var int10 = new TestType { IntColumn = 10 };
                var int20 = new TestType { IntColumn = 20 };
                var int30 = new TestType { IntColumn = 30 };

                Init(db, 0, int10, int20, int30);

                var results = db.Select<TestType>(x => ints.Contains(x.IntColumn));
                var resultsNullable = db.Select<TestType>(x => nullableInts.Contains(x.IntColumn));

                CollectionAssert.AreEquivalent(new[] { int20, int30 }, results);
                CollectionAssert.AreEquivalent(new[] { int20, int30 }, resultsNullable);

                Assert.That(db.GetLastSql(), Does.Contain("(@0,@1,@2)").
                                             Or.Contain("(:0,:1,:2)"));
            }
        }

        [Test]
        public void Can_select_ints_using_list_contains()
        {
            var ints = new[] { 1, 20, 30 }.ToList();
            var nullableInts = new int?[] { 5, 30, null, 20 }.ToList();

            using (var db = OpenDbConnection())
            {
                var int10 = new TestType { IntColumn = 10 };
                var int20 = new TestType { IntColumn = 20 };
                var int30 = new TestType { IntColumn = 30 };

                Init(db, 0, int10, int20, int30);

                var results = db.Select<TestType>(x => ints.Contains(x.IntColumn));
                var resultsNullable = db.Select<TestType>(x => nullableInts.Contains(x.IntColumn));

                CollectionAssert.AreEquivalent(new[] { int20, int30 }, results);
                CollectionAssert.AreEquivalent(new[] { int20, int30 }, resultsNullable);

                Assert.That(db.GetLastSql(), Does.Contain("(@0,@1,@2)").
                                             Or.Contain("(:0,:1,:2)"));
            }
        }

        [Test]
        public void Can_select_ints_using_empty_array_contains()
        {
            var ints = new int[] {};

            using (var db = OpenDbConnection())
            {
                Init(db, 5);

                var results = db.Select<TestType>(x => ints.Contains(x.Id));

                CollectionAssert.IsEmpty(results);
                Assert.That(db.GetLastSql(), Does.Contain("(NULL)"));
            }
        }

    }
}