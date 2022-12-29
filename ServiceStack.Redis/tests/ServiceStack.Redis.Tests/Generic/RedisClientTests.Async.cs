using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests.Generic
{
    [TestFixture, Category("Integration"), Category("Async")]
    public class RedisClientTestsAsync : RedisClientTestsBaseAsync
    {
        [OneTimeSetUp]
        public void TestFixture()
        {
        }

        public override void OnBeforeEachTest()
        {
            base.OnBeforeEachTest();
            RedisRaw.NamespacePrefix = "GenericRedisClientTests";
        }

        [Test]
        public async Task Can_Set_and_Get_string()
        {
            const string value = "value";
            await RedisAsync.SetValueAsync("key", value);
            var valueString = await RedisAsync.GetValueAsync("key");

            Assert.That(valueString, Is.EqualTo(value));
        }

        [Test]
        public async Task Can_Set_and_Get_key_with_all_byte_values()
        {
            const string key = "bytesKey";

            var value = new byte[256];
            for (var i = 0; i < value.Length; i++)
            {
                value[i] = (byte)i;
            }

            await NativeAsync.SetAsync(key, value);
            var resultValue = await NativeAsync.GetAsync(key);

            Assert.That(resultValue, Is.EquivalentTo(value));
        }

        public List<T> Sort<T>(IEnumerable<T> list)
        {
            var sortedList = list.ToList();
            sortedList.Sort((x, y) =>
                x.GetId().ToString().CompareTo(y.GetId().ToString()));

            return sortedList;
        }

        [Test]
        public async Task Can_SetBit_And_GetBit_And_BitCount()
        {
            const string key = "BitKey";
            const int offset = 100;
            await NativeAsync.SetBitAsync(key, offset, 1);
            Assert.AreEqual(1, await NativeAsync.GetBitAsync(key, offset));
            Assert.AreEqual(1, await NativeAsync.BitCountAsync(key));
        }

        public class Dummy
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Test]
        public async Task Can_Delete()
        {
            var dto = new Dummy { Id = 1, Name = "Name" };

            await RedisAsync.StoreAsync(dto);

            Assert.That((await RedisAsync.GetAllItemsFromSetAsync(RedisRaw.NamespacePrefix + "ids:Dummy")).ToArray()[0], Is.EqualTo("1"));
            Assert.That(await RedisAsync.GetByIdAsync<Dummy>(1), Is.Not.Null);

            await RedisAsync.DeleteAsync(dto);

            Assert.That((await RedisAsync.GetAllItemsFromSetAsync(RedisRaw.NamespacePrefix + "ids:Dummy")).Count, Is.EqualTo(0));
            Assert.That(await RedisAsync.GetByIdAsync<Dummy>(1), Is.Null);
        }

        [Test]
        public async Task Can_DeleteById()
        {
            var dto = new Dummy { Id = 1, Name = "Name" };
            await RedisAsync.StoreAsync(dto);

            Assert.That((await RedisAsync.GetAllItemsFromSetAsync(RedisRaw.NamespacePrefix + "ids:Dummy")).ToArray()[0], Is.EqualTo("1"));
            Assert.That(await RedisAsync.GetByIdAsync<Dummy>(1), Is.Not.Null);

            await RedisAsync.DeleteByIdAsync<Dummy>(dto.Id);

            Assert.That((await RedisAsync.GetAllItemsFromSetAsync(RedisRaw.NamespacePrefix + "ids:Dummy")).Count, Is.EqualTo(0));
            Assert.That(await RedisAsync.GetByIdAsync<Dummy>(1), Is.Null);
        }

        [Test]
        public async Task Can_save_via_string()
        {
            var dtos = 10.Times(i => new Dummy { Id = i, Name = "Name" + i });

            await RedisAsync.SetValueAsync("dummy:strings", dtos.ToJson());

            var fromDtos = (await RedisAsync.GetValueAsync("dummy:strings")).FromJson<List<Dummy>>();

            Assert.That(fromDtos.Count, Is.EqualTo(10));
        }

        [Test]
        public async Task Can_save_via_types()
        {
            var dtos = 10.Times(i => new Dummy { Id = i, Name = "Name" + i });

            await RedisAsync.SetAsync("dummy:strings", dtos);

            var fromDtos = await RedisAsync.GetAsync<List<Dummy>>("dummy:strings");

            Assert.That(fromDtos.Count, Is.EqualTo(10));
        }
    }
}
