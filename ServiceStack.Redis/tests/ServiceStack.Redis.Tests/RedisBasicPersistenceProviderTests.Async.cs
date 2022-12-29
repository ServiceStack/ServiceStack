using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.Model;
using ServiceStack.Redis.Generic;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Integration")]
    public class RedisBasicPersistenceProviderTestsAsync
        : RedisClientTestsBaseAsync
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
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return other.Id.Equals(Id) && Equals(other.Name, Name) && other.Age == Age;
            }

            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != typeof(TestModel)) return false;
                return Equals((TestModel)obj);
            }

            [SuppressMessage("Style", "IDE0070:Use 'System.HashCode'", Justification = "not in netfx")]
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

            RedisRaw.NamespacePrefix = "RedisBasicPersistenceProviderTests";
            testModels = new List<TestModel>();
            5.Times(i => testModels.Add(
                new TestModel { Id = Guid.NewGuid(), Name = "Name" + i, Age = 20 + i }));
        }

        [Test]
        public async Task Can_Store()
        {
            foreach (var x in testModels)
            {
                await RedisAsync.StoreAsync(x);
            }

            var allModels = (await RedisAsync.As<TestModel>().GetAllAsync()).OrderBy(x => x.Age).ToList();

            Assert.That(allModels, Is.EquivalentTo(testModels));
        }

        [Test]
        public async Task Can_StoreAll()
        {
            await RedisAsync.StoreAllAsync(testModels);

            var allModels = (await RedisAsync.As<TestModel>().GetAllAsync()).OrderBy(x => x.Age).ToList();

            Assert.That(allModels, Is.EquivalentTo(testModels));
        }

        [Test]
        public async Task Can_WriteAll()
        {
            await RedisAsync.WriteAllAsync(testModels);

            var testModelIds = testModels.ConvertAll(x => x.Id);

            var allModels = (await RedisAsync.GetByIdsAsync<TestModel>(testModelIds))
                .OrderBy(x => x.Age).ToList();

            Assert.That(allModels, Is.EquivalentTo(testModels));
        }

        [Test]
        public async Task Can_GetById()
        {
            await RedisAsync.StoreAllAsync(testModels);

            var last = testModels.Last();
            var lastById = await RedisAsync.GetByIdAsync<TestModel>(last.Id);

            Assert.That(lastById, Is.EqualTo(last));
        }

        [Test]
        public async Task Can_GetByIds()
        {
            await RedisAsync.StoreAllAsync(testModels);

            var evenTestModels = testModels.Where(x => x.Age % 2 == 0)
                .OrderBy(x => x.Id).ToList();
            var evenTestModelIds = evenTestModels.Select(x => x.Id).ToList();

            var selectedModels = (await RedisAsync.GetByIdsAsync<TestModel>(evenTestModelIds))
                .OrderBy(x => x.Id).ToList();

            Assert.That(selectedModels, Is.EqualTo(evenTestModels));
        }

        [Test]
        public async Task Can_Delete()
        {
            await RedisAsync.StoreAllAsync(testModels);

            var last = testModels.Last();
            await RedisAsync.DeleteAsync(last);

            testModels.Remove(last);

            var allModels = (await RedisAsync.As<TestModel>().GetAllAsync()).OrderBy(x => x.Age).ToList();

            Assert.That(allModels, Is.EquivalentTo(testModels));

            //Test internal TestModelIdsSetKey state
            var idsRemaining = (await RedisAsync.GetAllItemsFromSetAsync(RedisRaw.NamespacePrefix + TestModelIdsSetKey))
                .OrderBy(x => x).Map(x => new Guid(x));

            var testModelIds = testModels.OrderBy(x => x.Id).Map(x => x.Id);

            Assert.That(idsRemaining, Is.EquivalentTo(testModelIds));
        }

        [Test]
        public async Task Can_DeleteAll()
        {
            await RedisAsync.StoreAllAsync(testModels);

            await RedisAsync.DeleteAllAsync<TestModel>();

            var allModels = await RedisAsync.As<TestModel>().GetAllAsync();

            Assert.That(allModels, Is.Empty);

            //Test internal TestModelIdsSetKey state
            var idsRemaining = await RedisAsync.GetAllItemsFromSetAsync(TestModelIdsSetKey);
            Assert.That(idsRemaining, Is.Empty);
        }

        [Test]
        public async Task Can_DeleteAll_with_runtime_type()
        {
            await RedisAsync.StoreAllAsync(testModels);

            var mi = typeof(IEntityStoreAsync).GetMethod(nameof(IEntityStoreAsync.DeleteAllAsync));
            var genericMi = mi.MakeGenericMethod(typeof(TestModel));
            await (Task)genericMi.Invoke(RedisAsync, new object[] { CancellationToken.None });

            var allModels = await RedisAsync.As<TestModel>().GetAllAsync();
            Assert.That(allModels, Is.Empty);
            var idsRemaining = await RedisAsync.GetAllItemsFromSetAsync(TestModelIdsSetKey);
            Assert.That(idsRemaining, Is.Empty);
        }

        [Test]
        public async Task Can_As_DeleteAll_with_runtime_type()
        {
            await RedisAsync.StoreAllAsync(testModels);

            var mi =  typeof(IRedisClientAsync).GetMethod(nameof(IRedisClientAsync.As));
            var genericMi = mi.MakeGenericMethod(typeof(TestModel));
            var typedClient = genericMi.Invoke(RedisAsync, TypeConstants.EmptyObjectArray);
            var deleteMi = typeof(IEntityStoreAsync<TestModel>).GetMethod(nameof(IEntityStoreAsync<Type>.DeleteAllAsync));
            await (Task)deleteMi.Invoke(typedClient, new object[] { CancellationToken.None });

            var allModels = await RedisAsync.As<TestModel>().GetAllAsync();
            Assert.That(allModels, Is.Empty);
            var idsRemaining = await RedisAsync.GetAllItemsFromSetAsync(TestModelIdsSetKey);
            Assert.That(idsRemaining, Is.Empty);
        }

        [Test]
        public async Task Can_As_DeleteAll_with_script()
        {
            await RedisAsync.StoreAllAsync(testModels);

            var context = new ScriptContext
            {
                ScriptLanguages = { ScriptLisp.Language },
                AllowScriptingOfAllTypes = true,
                ScriptMethods = {
                    new ProtectedScripts()
                },
                Args = {
                    ["redis"] = RedisAsync
                }
            }.Init();

            var type = typeof(TestModel).FullName;
#if DEBUG            
            RedisRaw.DebugAllowSync = true; // not reasonable to allow async from Lisp
#endif
            context.EvaluateCode($"redis.call('DeleteAll<{type}>') |> return");
            context.EvaluateCode($"redis.call('As<{type}>').call('DeleteAll') |> return");
            context.RenderLisp($"(call redis \"DeleteAll<{type}>\")");
            context.RenderLisp($"(call (call redis \"As<{type}>\") \"DeleteAll\")");
#if DEBUG            
            RedisRaw.DebugAllowSync = false;
#endif

            var allModels = await RedisAsync.As<TestModel>().GetAllAsync();
            Assert.That(allModels, Is.Empty);
            var idsRemaining = await RedisAsync.GetAllItemsFromSetAsync(TestModelIdsSetKey);
            Assert.That(idsRemaining, Is.Empty);
        }

        [Test]
        public async Task Can_DeleteByIds()
        {
            await RedisAsync.StoreAllAsync(testModels);

            var evenTestModels = testModels.Where(x => x.Age % 2 == 0)
                .OrderBy(x => x.Id).ToList();
            var evenTestModelIds = evenTestModels.Select(x => x.Id).ToList();

            await RedisAsync.DeleteByIdsAsync<TestModel>(evenTestModelIds);

            evenTestModels.ForEach(x => testModels.Remove(x));

            var allModels = (await RedisAsync.As<TestModel>().GetAllAsync()).OrderBy(x => x.Age).ToList();

            Assert.That(allModels, Is.EqualTo(testModels));


            //Test internal TestModelIdsSetKey state
            var idsRemaining = (await RedisAsync.GetAllItemsFromSetAsync(RedisRaw.NamespacePrefix + TestModelIdsSetKey))
                .OrderBy(x => x).Map(x => new Guid(x));

            var testModelIds = testModels.OrderBy(x => x.Id).Map(x => x.Id);

            Assert.That(idsRemaining, Is.EquivalentTo(testModelIds));
        }

    }
}
