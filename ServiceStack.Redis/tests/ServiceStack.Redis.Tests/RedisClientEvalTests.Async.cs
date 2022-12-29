using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Integration")]
    public class RedisClientEvalTestsAsync : RedisClientTestsBaseAsync
    {
        public override void OnBeforeEachTest()
        {
            //base.OnBeforeEachTest();

            //Run on local build server
            RedisRaw = new RedisClient(TestConfig.SingleHost);
            RedisRaw.FlushAll();
        }

        [Test]
        public async Task Can_Eval_int()
        {
            var intVal = await RedisAsync.ExecLuaAsIntAsync("return 3141591", Array.Empty<string>());
            Assert.That(intVal, Is.EqualTo(3141591));
        }

        [Test]
        public async Task Can_EvalSha_int()
        {
            var luaBody = "return 3141591";
            await RedisAsync.ExecLuaAsIntAsync(luaBody, Array.Empty<string>());
            var sha1 = await RedisAsync.CalculateSha1Async(luaBody);
            var intVal = await RedisAsync.ExecLuaShaAsIntAsync(sha1, Array.Empty<string>());
            Assert.That(intVal, Is.EqualTo(3141591));
        }

        [Test]
        public async Task Can_Eval_int_with_args()
        {
            var intVal = await RedisAsync.ExecLuaAsIntAsync("return 3141591", new[] { "20", "30", "40" });
            Assert.That(intVal, Is.EqualTo(3141591));
        }

        [Test]
        public async Task Can_Eval_int_with_keys_and_args()
        {
            var intVal = await RedisAsync.ExecLuaAsIntAsync("return KEYS[1] + ARGV[1]", new[] { "20" }, new[] { "30", "40" });
            Assert.That(intVal, Is.EqualTo(50));
        }

        [Test]
        public async Task Can_Eval_int2()
        {
            var intVal = await RedisAsync.ExecLuaAsIntAsync("return ARGV[1] + ARGV[2]", new[] { "10", "20" });
            Assert.That(intVal, Is.EqualTo(30));
        }

        [Test]
        public async Task Can_Eval_string()
        {
            var strVal = await RedisAsync.ExecLuaAsStringAsync(@"return 'abc'", new string[0]);
            Assert.That(strVal, Is.EqualTo("abc"));
        }

        [Test]
        public async Task Can_Eval_HelloWorld_string()
        {
            var strVal = await RedisAsync.ExecLuaAsStringAsync(@"return 'Hello, ' .. ARGV[1] .. '!'", new[] { "Redis Lua" });
            Assert.That(strVal, Is.EqualTo("Hello, Redis Lua!"));
        }

        [Test]
        public async Task Can_Eval_string_with_args()
        {
            var strVal = await RedisAsync.ExecLuaAsStringAsync(@"return 'abc'", new[] { "at", "dot", "com" });
            Assert.That(strVal, Is.EqualTo("abc"));
        }

        [Test]
        public async Task Can_Eval_string_with_keys_an_args()
        {
            var strVal = await RedisAsync.ExecLuaAsStringAsync(@"return KEYS[1] .. ARGV[1]", new[] { "at" }, new[] { "dot", "com" });
            Assert.That(strVal, Is.EqualTo("atdot"));
        }

        [Test]
        public async Task Can_Eval_multidata_with_args()
        {
            var strVals = await RedisAsync.ExecLuaAsListAsync(@"return {ARGV[1],ARGV[2],ARGV[3]}", new[] { "at", "dot", "com" });
            Assert.That(strVals, Is.EquivalentTo(new List<string> { "at", "dot", "com" }));
        }

        [Test]
        public async Task Can_Eval_multidata_with_keys_and_args()
        {
            var strVals = await RedisAsync.ExecLuaAsListAsync(@"return {KEYS[1],ARGV[1],ARGV[2]}", new[] { "at" }, new[] { "dot", "com" });
            Assert.That(strVals, Is.EquivalentTo(new List<string> { "at", "dot", "com" }));
        }

        [Test]
        public async Task Can_Load_and_Exec_script()
        {
            var luaBody = "return 'load script and exec'";
            var sha1 = await RedisAsync.LoadLuaScriptAsync(luaBody);
            var result = await RedisAsync.ExecLuaShaAsStringAsync(sha1, new string[0]);
            Assert.That(result, Is.EqualTo("load script and exec"));
        }

        [Test]
        public async Task Does_flush_all_scripts()
        {
            var luaBody = "return 'load script and exec'";
            var sha1 = await RedisAsync.LoadLuaScriptAsync(luaBody);
            var result = await RedisAsync.ExecLuaShaAsStringAsync(sha1, new string[0]);
            Assert.That(result, Is.EqualTo("load script and exec"));

            await RedisAsync.RemoveAllLuaScriptsAsync();

            try
            {
                result = await RedisAsync.ExecLuaShaAsStringAsync(sha1, new string[0]);
                Assert.Fail("script shouldn't exist");
            }
            catch (RedisResponseException ex)
            {
                Assert.That(ex.Message, Does.Contain("NOSCRIPT"));
            }
        }

        [Test]
        public async Task Can_detect_which_scripts_exist()
        {
            var sha1 = await RedisAsync.LoadLuaScriptAsync("return 'script1'");
            var sha2 = await RedisAsync.CalculateSha1Async("return 'script2'");
            var sha3 = await RedisAsync.LoadLuaScriptAsync("return 'script3'");

            Assert.That(await RedisAsync.HasLuaScriptAsync(sha1));

            var existsMap = await RedisAsync.WhichLuaScriptsExistsAsync(new[] { sha1, sha2, sha3 });
            Assert.That(existsMap[sha1]);
            Assert.That(!existsMap[sha2]);
            Assert.That(existsMap[sha3]);
        }

        [Test]
        public async Task Can_create_ZPop_with_lua()
        {
            var luaBody = @"
                local val = redis.call('zrange', KEYS[1], 0, ARGV[1]-1)
                if val then redis.call('zremrangebyrank', KEYS[1], 0, ARGV[1]-1) end
                return val";

            var i = 0;
            var alphabet = 26.Times(c => ((char)('A' + c)).ToString());
            foreach (var x in alphabet)
            {
                await RedisAsync.AddItemToSortedSetAsync("zalphabet", x, i++);
            }

            var letters = await RedisAsync.ExecLuaAsListAsync(luaBody, keys: new[] { "zalphabet" }, args: new[] { "3" });

            letters.PrintDump();
            Assert.That(letters, Is.EquivalentTo(new[] { "A", "B", "C" }));
        }

        [Test]
        public async Task Can_create_ZRevPop_with_lua()
        {
            var luaBody = @"
                local val = redis.call('zrange', KEYS[1], -ARGV[1], -1)
                if val then redis.call('zremrangebyrank', KEYS[1], -ARGV[1], -1) end
                return val";

            var i = 0;
            var alphabet = 26.Times(c => ((char)('A' + c)).ToString());
            foreach(var x in alphabet)
            {
                await RedisAsync.AddItemToSortedSetAsync("zalphabet", x, i++);
            }

            var letters = await RedisAsync.ExecLuaAsListAsync(luaBody, keys: new[] { "zalphabet" }, args: new[] { "3" });

            letters.PrintDump();
            Assert.That(letters, Is.EquivalentTo(new[] { "X", "Y", "Z" }));
        }

        [Test]
        public async Task Can_return_DaysOfWeek_as_list()
        {
            foreach(var x in Enum.GetNames(typeof(DayOfWeek)).ToList())
            {
                await RedisAsync.AddItemToListAsync("DaysOfWeek", x);
            }
            (await RedisAsync.ExecLuaAsListAsync("return redis.call('LRANGE', 'DaysOfWeek', 0, -1)", new string[0])).PrintDump();
        }
    }
}