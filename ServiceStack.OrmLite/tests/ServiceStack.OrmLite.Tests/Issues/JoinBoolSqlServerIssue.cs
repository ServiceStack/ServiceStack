using System.Diagnostics;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [TestFixtureOrmLite]
    public class JoinBoolSqlServerIssue : OrmLiteProvidersTestBase
    {
        public JoinBoolSqlServerIssue(DialectContext context) : base(context) {}

        [Test]
        public void Can_Join_on_bool()
        {
            using (var db = OpenDbConnection())
            {
                db.DropTable<TestType>();
                db.DropTable<TestType2>();

                db.CreateTable<TestType2>();
                db.CreateTable<TestType>();

                var q = db.From<TestType>()
                    .LeftJoin<TestType2>((t1, t2) => t2.BoolCol == true && t1.Id == t2.Id);
                var results = db.Select(q);

                q = db.From<TestType>()
                    .LeftJoin<TestType2>((t1, t2) => t2.NullableBoolCol == true && t1.Id == t2.Id);
                results = db.Select(q);

                results.PrintDump();
            }
        }

        [Test]
        public void Can_compare_bool_in_join_expression()
        {
            var db = OpenDbConnection();

            db.DropTable<CardHolder>();
            db.DropTable<Account>();

            db.CreateTable<Account>();
            db.CreateTable<CardHolder>();

            var exp1 = db.From<Account>()
                        .LeftJoin<CardHolder>((a, ch) => a.Id == ch.AccountId && ch.TestBoolB == true);

            Debug.WriteLine(exp1.BodyExpression);
            db.Select(exp1).PrintDump();

            var exp2 = db.From<Account>()
                        .LeftJoin<CardHolder>((a, ch) => a.Id == ch.AccountId && a.TestBoolA == true);

            Debug.WriteLine(exp2.BodyExpression);
            db.Select(exp2).PrintDump();


            var exp3 = db.From<Account>()
                            .Where(a => a.TestBoolA == true);
            Debug.WriteLine(exp3.BodyExpression);
            db.Select(exp3).PrintDump();
        }
    }

    public class Account
    {
        [PrimaryKey]
        public int Id { get; set; }

        public bool TestBoolA { get; set; }
    }

    public class CardHolder
    {
        [PrimaryKey]
        public int Id { get; set; }

        [References(typeof(Account))]
        public int AccountId { get; set; }

        public bool TestBoolB { get; set; }
    }

}