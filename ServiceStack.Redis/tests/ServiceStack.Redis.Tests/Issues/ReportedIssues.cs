using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Redis.Generic;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests.Issues
{
    [TestFixture]
    public class ReportedIssues
        : RedisClientTestsBase
    {
        private readonly List<string> storeMembers = new List<string> { "one", "two", "three", "four" };

        [Test]
        public void Add_range_to_set_fails_if_first_command()
        {
            Redis.AddRangeToSet("testset", storeMembers);

            var members = Redis.GetAllItemsFromSet("testset");
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public void Transaction_fails_if_first_command()
        {
            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.IncrementValue("A"));

                trans.Commit();
            }
            Assert.That(Redis.GetValue("A"), Is.EqualTo("1"));
        }

        [Test]
        public void Success_callback_fails_for_pipeline_using_GetItemScoreInSortedSet()
        {
            double score = 0;
            Redis.AddItemToSortedSet("testzset", "value", 1);

            using (var pipeline = Redis.CreatePipeline())
            {
                pipeline.QueueCommand(u => u.GetItemScoreInSortedSet("testzset", "value"), x =>
                {
                    //score should be assigned to 1 here
                    score = x;
                });

                pipeline.Flush();
            }

            Assert.That(score, Is.EqualTo(1));
        }

        public class Test
        {
            public int Id { get; set; }
            public string Name { get; set; }

            protected bool Equals(Test other) => Id == other.Id && Name == other.Name;

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Test) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Id * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                }
            }
        }

        [Test]
        public void Try_simulate_NRE_when_calling_GetAllEntriesFromHash_using_BasicRedisClientManager()
        {
            using (var redisManager = new BasicRedisClientManager(TestConfig.SingleHost))
            using (var redis = redisManager.GetClient())
            {
                IRedisHash<string, Test> testHash = redis.As<Test>()
                    .GetHash<string>("test-hash");
                
                Assert.That(testHash.Count, Is.EqualTo(0));

                var contents = testHash.GetAll();
                Assert.That(contents.Count, Is.EqualTo(0));

                var test1 = new Test { Id = 1, Name = "Name1" };
                var test2 = new Test { Id = 2, Name = "Name2" };
                testHash["A"] = test1;
                testHash["B"] = test2;
                
                contents = testHash.GetAll();
                
                Assert.That(contents, Is.EqualTo(new Dictionary<string, Test> {
                    ["A"] = test1,
                    ["B"] = test2,
                }));

                Assert.That(testHash["A"], Is.EqualTo(test1));
                Assert.That(testHash["B"], Is.EqualTo(test2));
            }
        }
    }
}