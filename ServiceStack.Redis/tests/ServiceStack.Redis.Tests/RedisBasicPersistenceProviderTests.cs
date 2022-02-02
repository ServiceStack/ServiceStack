using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Model;
using ServiceStack.Redis.Generic;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Integration")]
    public class RedisBasicPersistenceProviderTests
        : RedisClientTestsBase
    {
        List<TestModel> testModels;

        public static string TestModelIdsSetKey = "ids:" + typeof(TestModel).Name;

        public class TestModel
            : IHasId<Guid>
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }

            //Thanking R# for the timesaver
            public bool Equals(TestModel other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return other.Id.Equals(Id) && Equals(other.Name, Name) && other.Age == Age;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != typeof(TestModel)) return false;
                return Equals((TestModel)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int result = Id.GetHashCode();
                    result = (result * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                    result = (result * 397) ^ Age;
                    return result;
                }
            }
        }

        public override void OnBeforeEachTest()
        {
            base.OnBeforeEachTest();

            Redis.NamespacePrefix = "RedisBasicPersistenceProviderTests";
            testModels = new List<TestModel>();
            5.Times(i => testModels.Add(
                new TestModel { Id = Guid.NewGuid(), Name = "Name" + i, Age = 20 + i }));
        }

        [Test]
        public void Can_Store()
        {
            testModels.ForEach(x => Redis.Store(x));

            var allModels = Redis.GetAll<TestModel>().OrderBy(x => x.Age).ToList();

            Assert.That(allModels, Is.EquivalentTo(testModels));
        }

        [Test]
        public void Can_StoreAll()
        {
            Redis.StoreAll(testModels);

            var allModels = Redis.GetAll<TestModel>().OrderBy(x => x.Age).ToList();

            Assert.That(allModels, Is.EquivalentTo(testModels));
        }

        [Test]
        public void Can_WriteAll()
        {
            Redis.WriteAll(testModels);

            var testModelIds = testModels.ConvertAll(x => x.Id);

            var allModels = Redis.GetByIds<TestModel>(testModelIds)
                .OrderBy(x => x.Age).ToList();

            Assert.That(allModels, Is.EquivalentTo(testModels));
        }

        [Test]
        public void Can_GetById()
        {
            Redis.StoreAll(testModels);

            var last = testModels.Last();
            var lastById = Redis.GetById<TestModel>(last.Id);

            Assert.That(lastById, Is.EqualTo(last));
        }

        [Test]
        public void Can_GetByIds()
        {
            Redis.StoreAll(testModels);

            var evenTestModels = testModels.Where(x => x.Age % 2 == 0)
                .OrderBy(x => x.Id).ToList();
            var evenTestModelIds = evenTestModels.Select(x => x.Id).ToList();

            var selectedModels = Redis.GetByIds<TestModel>(evenTestModelIds)
                .OrderBy(x => x.Id).ToList();

            Assert.That(selectedModels, Is.EqualTo(evenTestModels));
        }

        [Test]
        public void Can_Delete()
        {
            Redis.StoreAll(testModels);

            var last = testModels.Last();
            Redis.Delete(last);

            testModels.Remove(last);

            var allModels = Redis.GetAll<TestModel>().OrderBy(x => x.Age).ToList();

            Assert.That(allModels, Is.EquivalentTo(testModels));

            //Test internal TestModelIdsSetKey state
            var idsRemaining = Redis.GetAllItemsFromSet(Redis.NamespacePrefix + TestModelIdsSetKey)
                .OrderBy(x => x).Map(x => new Guid(x));

            var testModelIds = testModels.OrderBy(x => x.Id).Map(x => x.Id);

            Assert.That(idsRemaining, Is.EquivalentTo(testModelIds));
        }

        [Test]
        public void Can_DeleteAll()
        {
            Redis.StoreAll(testModels);

            Redis.DeleteAll<TestModel>();

            var allModels = Redis.GetAll<TestModel>();

            Assert.That(allModels, Is.Empty);

            //Test internal TestModelIdsSetKey state
            var idsRemaining = Redis.GetAllItemsFromSet(TestModelIdsSetKey);
            Assert.That(idsRemaining, Is.Empty);
        }

        [Test]
        public void Can_DeleteAll_with_runtime_type()
        {
            Redis.StoreAll(testModels);

            var mi = Redis.GetType().GetMethod(nameof(RedisClient.DeleteAll));
            var genericMi = mi.MakeGenericMethod(typeof(TestModel));
            genericMi.Invoke(Redis, TypeConstants.EmptyObjectArray);

            var allModels = Redis.GetAll<TestModel>();
            Assert.That(allModels, Is.Empty);
            var idsRemaining = Redis.GetAllItemsFromSet(TestModelIdsSetKey);
            Assert.That(idsRemaining, Is.Empty);
        }

        [Test]
        public void Can_As_DeleteAll_with_runtime_type()
        {
            Redis.StoreAll(testModels);

            var mi = Redis.GetType().GetMethod(nameof(RedisClient.As));
            var genericMi = mi.MakeGenericMethod(typeof(TestModel));
            var typedClient = genericMi.Invoke(Redis, TypeConstants.EmptyObjectArray);
            var deleteMi = typedClient.GetType().GetMethod(nameof(IRedisTypedClient<Type>.DeleteAll));
            deleteMi.Invoke(typedClient, TypeConstants.EmptyObjectArray);

            var allModels = Redis.GetAll<TestModel>();
            Assert.That(allModels, Is.Empty);
            var idsRemaining = Redis.GetAllItemsFromSet(TestModelIdsSetKey);
            Assert.That(idsRemaining, Is.Empty);
        }

        [Test]
        public void Can_As_DeleteAll_with_script()
        {
            Redis.StoreAll(testModels);
            
            var context = new ScriptContext {
                ScriptLanguages = { ScriptLisp.Language },
                AllowScriptingOfAllTypes = true,
                ScriptMethods = {
                    new ProtectedScripts()
                },
                Args = {
                    ["redis"] = Redis
                }
            }.Init();

            var type = typeof(TestModel).FullName;
            context.EvaluateCode($"redis.call('DeleteAll<{type}>') |> return");
            context.EvaluateCode($"redis.call('As<{type}>').call('DeleteAll') |> return");
            context.RenderLisp($"(call redis \"DeleteAll<{type}>\")");
            context.RenderLisp($"(call (call redis \"As<{type}>\") \"DeleteAll\")");

            var allModels = Redis.GetAll<TestModel>();
            Assert.That(allModels, Is.Empty);
            var idsRemaining = Redis.GetAllItemsFromSet(TestModelIdsSetKey);
            Assert.That(idsRemaining, Is.Empty);
        }

        [Test]
        public void Can_DeleteByIds()
        {
            Redis.StoreAll(testModels);

            var evenTestModels = testModels.Where(x => x.Age % 2 == 0)
                .OrderBy(x => x.Id).ToList();
            var evenTestModelIds = evenTestModels.Select(x => x.Id).ToList();

            Redis.DeleteByIds<TestModel>(evenTestModelIds);

            evenTestModels.ForEach(x => testModels.Remove(x));

            var allModels = Redis.GetAll<TestModel>().OrderBy(x => x.Age).ToList();

            Assert.That(allModels, Is.EqualTo(testModels));


            //Test internal TestModelIdsSetKey state
            var idsRemaining = Redis.GetAllItemsFromSet(Redis.NamespacePrefix + TestModelIdsSetKey)
                .OrderBy(x => x).Map(x => new Guid(x));

            var testModelIds = testModels.OrderBy(x => x.Id).Map(x => x.Id);

            Assert.That(idsRemaining, Is.EquivalentTo(testModelIds));
        }

    }
}
