using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Redis.Generic;

namespace ServiceStack.Redis.Tests.Generic
{
    public class Question
    {
        public long Id { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }

        public static Question Create(long id)
        {
            return new Question
            {
                Id = id,
                Content = "Content" + id,
                Title = "Title" + id,
                UserId = "User" + id,
            };
        }
    }

    public class Answer
    {
        public long Id { get; set; }
        public long QuestionId { get; set; }
        public string UserId { get; set; }
        public string Content { get; set; }

        public static Answer Create(long id, long questionId)
        {
            return new Answer
            {
                Id = id,
                QuestionId = questionId,
                UserId = "User" + id,
                Content = "Content" + id,
            };
        }

        public bool Equals(Answer other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Id == Id && other.QuestionId == QuestionId && Equals(other.UserId, UserId) && Equals(other.Content, Content);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(Answer)) return false;
            return Equals((Answer)obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    [TestFixture]
    public class RedisTypedClientAppTests
        : RedisClientTestsBase
    {
        private IRedisTypedClient<Question> redisQuestions;
        readonly Question question1 = Question.Create(1);
        List<Answer> q1Answers;

        public override void OnBeforeEachTest()
        {
            base.OnBeforeEachTest();

            redisQuestions = base.Redis.As<Question>();
            redisQuestions.Db = 10;
            redisQuestions.FlushDb();

            q1Answers = new List<Answer>
              {
                  Answer.Create(1, question1.Id),
                  Answer.Create(2, question1.Id),
                  Answer.Create(3, question1.Id),
                  Answer.Create(4, question1.Id),
                  Answer.Create(5, question1.Id),
              };
        }

        [Test]
        public void Can_StoreRelatedEntities()
        {
            redisQuestions.Store(question1);

            redisQuestions.StoreRelatedEntities(question1.Id, q1Answers);

            var actualAnswers = redisQuestions.GetRelatedEntities<Answer>(question1.Id);
            actualAnswers.Sort((x, y) => x.Id.CompareTo(y.Id));

            Assert.That(actualAnswers.EquivalentTo(q1Answers));
        }

        public class Customer
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        public class CustomerAddress
        {
            public string Id { get; set; }
            public string Address { get; set; }
        }

        [Test]
        public void Can_StoreRelatedEntities_with_StringId()
        {
            var redisCustomers = Redis.As<Customer>();
            var customer = new Customer { Id = "CUST-01", Name = "Customer" };

            redisCustomers.Store(customer);

            var addresses = new[]
            {
                new CustomerAddress { Id = "ADDR-01", Address = "1 Home Street" },
                new CustomerAddress { Id = "ADDR-02", Address = "2 Work Road" },
            };

            redisCustomers.StoreRelatedEntities(customer.Id, addresses);

            var actualAddresses = redisCustomers.GetRelatedEntities<CustomerAddress>(customer.Id);

            Assert.That(actualAddresses.Map(x => x.Id),
                Is.EquivalentTo(new[] { "ADDR-01", "ADDR-02" }));
        }

        [Test]
        public void Can_GetRelatedEntities_When_Empty()
        {
            redisQuestions.Store(question1);

            var answers = redisQuestions.GetRelatedEntities<Answer>(question1.Id);

            Assert.That(answers, Has.Count.EqualTo(0));
        }

        [Test]
        public void Can_DeleteRelatedEntity()
        {
            redisQuestions.Store(question1);

            redisQuestions.StoreRelatedEntities(question1.Id, q1Answers);

            var answerToDelete = q1Answers[3];
            redisQuestions.DeleteRelatedEntity<Answer>(question1.Id, answerToDelete.Id);

            q1Answers.RemoveAll(x => x.Id == answerToDelete.Id);

            var answers = redisQuestions.GetRelatedEntities<Answer>(question1.Id);

            Assert.That(answers.EquivalentTo(answers));
        }

        [Test]
        public void Can_DeleteRelatedEntities()
        {
            redisQuestions.Store(question1);

            redisQuestions.StoreRelatedEntities(question1.Id, q1Answers);

            redisQuestions.DeleteRelatedEntities<Answer>(question1.Id);

            var answers = redisQuestions.GetRelatedEntities<Answer>(question1.Id);

            Assert.That(answers.Count, Is.EqualTo(0));
        }

        [Test]
        public void Can_AddToRecentsList()
        {
            var redisAnswers = Redis.As<Answer>();

            redisAnswers.StoreAll(q1Answers);
            q1Answers.ForEach(redisAnswers.AddToRecentsList);

            var allAnswers = redisAnswers.GetLatestFromRecentsList(0, int.MaxValue);
            allAnswers.Sort((x, y) => x.Id.CompareTo(y.Id));

            Assert.That(allAnswers.EquivalentTo(q1Answers));
        }

        [Test]
        public void Can_GetLatestFromRecentsList()
        {
            var redisAnswers = Redis.As<Answer>();

            redisAnswers.StoreAll(q1Answers);
            q1Answers.ForEach(redisAnswers.AddToRecentsList);

            var latest3Answers = redisAnswers.GetLatestFromRecentsList(0, 3);

            var i = q1Answers.Count;
            var expectedAnswers = new List<Answer>
            {
                q1Answers[--i], q1Answers[--i], q1Answers[--i],
            };

            Assert.That(expectedAnswers.EquivalentTo(latest3Answers));
        }

        [Test]
        public void Can_GetEarliestFromRecentsList()
        {
            var redisAnswers = Redis.As<Answer>();

            redisAnswers.StoreAll(q1Answers);
            q1Answers.ForEach(redisAnswers.AddToRecentsList);

            var earliest3Answers = redisAnswers.GetEarliestFromRecentsList(0, 3);

            var i = 0;
            var expectedAnswers = new List<Answer>
            {
                q1Answers[i++], q1Answers[i++], q1Answers[i++],
            };

            Assert.That(expectedAnswers.EquivalentTo(earliest3Answers));
        }

        [Test]
        public void Can_save_quoted_strings()
        {
            var str = "string \"with\" \"quotes\"";
            var cacheKey = "quotetest";

            Redis.As<string>().SetValue(cacheKey, str);
            var fromRedis = Redis.As<string>().GetValue(cacheKey);
            Assert.That(fromRedis, Is.EqualTo(str));

            Redis.Set(cacheKey, str);
            fromRedis = Redis.Get<string>(cacheKey);
            Assert.That(fromRedis, Is.EqualTo(str));

            Redis.SetValue(cacheKey, str);
            fromRedis = Redis.GetValue(cacheKey);
            Assert.That(fromRedis, Is.EqualTo(str));

            Redis.SetValue(cacheKey, str.ToJson());
            fromRedis = Redis.GetValue(cacheKey).FromJson<string>();
            Assert.That(fromRedis, Is.EqualTo(str));
        }

        [Test]
        public void Does_return_non_existent_keys_as_defaultValue()
        {
            Assert.That(Redis.Get<string>("notexists"), Is.Null);
            Assert.That(Redis.Get<int>("notexists"), Is.EqualTo(0));
        }
    }
}