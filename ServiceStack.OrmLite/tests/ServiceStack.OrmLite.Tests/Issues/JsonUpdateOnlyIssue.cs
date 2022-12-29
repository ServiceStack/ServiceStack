using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class JsonUpdateOnlyIssue : OrmLiteProvidersTestBase
    {
        public JsonUpdateOnlyIssue(DialectContext context) : base(context) {}

        [Test]
        public void Can_Update_Answer_CustomField_json()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Question>();

                var createTableSql = db.GetLastSql();
                Assert.That(createTableSql, Does.Contain("\"answers\" json NULL"));

                db.Insert(new Question
                {
                    Id = 1,
                    Answers = new List<Answer>
                    {
                        new Answer { Id = 1, Text = "Q1 Answer1" }
                    }
                });

                var question = db.SingleById<Question>(1);
                Assert.That(question.Answers.Count, Is.EqualTo(1));
                Assert.That(question.Answers[0].Text, Is.EqualTo("Q1 Answer1"));

                db.UpdateOnly(() => new Question
                    {
                        Answers = new List<Answer> { new Answer { Id = 1, Text = "Q1 Answer1 Updated" } }
                    },
                    @where: q => q.Id == 1);

                question = db.SingleById<Question>(1);
                Assert.That(question.Answers[0].Text, Is.EqualTo("Q1 Answer1 Updated"));
            }
        }
    }

    public class Question
    {
        public int Id { get; set; }
        public string Text { get; set; }

        [PgSqlJson]
        public List<Answer> Answers { get; set; }
    }

    public class Answer
    {
        public int Id { get; set; }
        public string Text { get; set; }
    }
}