using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Integration")]
    public class RedisClientEvalTests : RedisClientTestsBase
    {
        public override void OnBeforeEachTest()
        {
            //base.OnBeforeEachTest();

            //Run on local build server
            Redis = new RedisClient(TestConfig.SingleHost);
            Redis.FlushAll();
        }

        [Test]
        public void Can_Eval_int()
        {
            var intVal = Redis.ExecLuaAsInt("return 3141591");
            Assert.That(intVal, Is.EqualTo(3141591));
        }

        [Test]
        public void Can_EvalSha_int()
        {
            var luaBody = "return 3141591";
            Redis.ExecLuaAsInt(luaBody);
            var sha1 = Redis.CalculateSha1(luaBody);
            var intVal = Redis.ExecLuaShaAsInt(sha1);
            Assert.That(intVal, Is.EqualTo(3141591));
        }

        [Test]
        public void Can_Eval_int_with_args()
        {
            var intVal = Redis.ExecLuaAsInt("return 3141591", "20", "30", "40");
            Assert.That(intVal, Is.EqualTo(3141591));
        }

        [Test]
        public void Can_Eval_int_with_keys_and_args()
        {
            var intVal = Redis.ExecLuaAsInt("return KEYS[1] + ARGV[1]", new[] { "20" }, new[] { "30", "40" });
            Assert.That(intVal, Is.EqualTo(50));
        }

        [Test]
        public void Can_Eval_int2()
        {
            var intVal = Redis.ExecLuaAsInt("return ARGV[1] + ARGV[2]", "10", "20");
            Assert.That(intVal, Is.EqualTo(30));
        }

        [Test]
        public void Can_Eval_string()
        {
            var strVal = Redis.ExecLuaAsString(@"return 'abc'");
            Assert.That(strVal, Is.EqualTo("abc"));
        }

        [Test]
        public void Can_Eval_HelloWorld_string()
        {
            var strVal = Redis.ExecLuaAsString(@"return 'Hello, ' .. ARGV[1] .. '!'", "Redis Lua");
            Assert.That(strVal, Is.EqualTo("Hello, Redis Lua!"));
        }

        [Test]
        public void Can_Eval_string_with_args()
        {
            var strVal = Redis.ExecLuaAsString(@"return 'abc'", "at", "dot", "com");
            Assert.That(strVal, Is.EqualTo("abc"));
        }

        [Test]
        public void Can_Eval_string_with_keys_an_args()
        {
            var strVal = Redis.ExecLuaAsString(@"return KEYS[1] .. ARGV[1]", new[] { "at" }, new[] { "dot", "com" });
            Assert.That(strVal, Is.EqualTo("atdot"));
        }

        [Test]
        public void Can_Eval_multidata_with_args()
        {
            var strVals = Redis.ExecLuaAsList(@"return {ARGV[1],ARGV[2],ARGV[3]}", "at", "dot", "com");
            Assert.That(strVals, Is.EquivalentTo(new List<string> { "at", "dot", "com" }));
        }

        [Test]
        public void Can_Eval_multidata_with_keys_and_args()
        {
            var strVals = Redis.ExecLuaAsList(@"return {KEYS[1],ARGV[1],ARGV[2]}", new[] { "at" }, new[] { "dot", "com" });
            Assert.That(strVals, Is.EquivalentTo(new List<string> { "at", "dot", "com" }));
        }

        [Test]
        public void Can_Load_and_Exec_script()
        {
            var luaBody = "return 'load script and exec'";
            var sha1 = Redis.LoadLuaScript(luaBody);
            var result = Redis.ExecLuaShaAsString(sha1);
            Assert.That(result, Is.EqualTo("load script and exec"));
        }

        [Test]
        public void Does_flush_all_scripts()
        {
            var luaBody = "return 'load script and exec'";
            var sha1 = Redis.LoadLuaScript(luaBody);
            var result = Redis.ExecLuaShaAsString(sha1);
            Assert.That(result, Is.EqualTo("load script and exec"));

            Redis.RemoveAllLuaScripts();

            try
            {
                result = Redis.ExecLuaShaAsString(sha1);
                Assert.Fail("script shouldn't exist");
            }
            catch (RedisResponseException ex)
            {
                Assert.That(ex.Message, Does.Contain("NOSCRIPT"));
            }
        }

        [Test]
        public void Can_detect_which_scripts_exist()
        {
            var sha1 = Redis.LoadLuaScript("return 'script1'");
            var sha2 = Redis.CalculateSha1("return 'script2'");
            var sha3 = Redis.LoadLuaScript("return 'script3'");

            Assert.That(Redis.HasLuaScript(sha1));

            var existsMap = Redis.WhichLuaScriptsExists(sha1, sha2, sha3);
            Assert.That(existsMap[sha1]);
            Assert.That(!existsMap[sha2]);
            Assert.That(existsMap[sha3]);
        }

        [Test]
        public void Can_create_ZPop_with_lua()
        {
            var luaBody = @"
                local val = redis.call('zrange', KEYS[1], 0, ARGV[1]-1)
                if val then redis.call('zremrangebyrank', KEYS[1], 0, ARGV[1]-1) end
                return val";

            var i = 0;
            var alphabet = 26.Times(c => ((char)('A' + c)).ToString());
            alphabet.ForEach(x => Redis.AddItemToSortedSet("zalphabet", x, i++));

            var letters = Redis.ExecLuaAsList(luaBody, keys: new[] { "zalphabet" }, args: new[] { "3" });

            letters.PrintDump();
            Assert.That(letters, Is.EquivalentTo(new[] { "A", "B", "C" }));
        }

        [Test]
        public void Can_create_ZRevPop_with_lua()
        {
            var luaBody = @"
                local val = redis.call('zrange', KEYS[1], -ARGV[1], -1)
                if val then redis.call('zremrangebyrank', KEYS[1], -ARGV[1], -1) end
                return val";

            var i = 0;
            var alphabet = 26.Times(c => ((char)('A' + c)).ToString());
            alphabet.ForEach(x => Redis.AddItemToSortedSet("zalphabet", x, i++));

            var letters = Redis.ExecLuaAsList(luaBody, keys: new[] { "zalphabet" }, args: new[] { "3" });

            letters.PrintDump();
            Assert.That(letters, Is.EquivalentTo(new[] { "X", "Y", "Z" }));
        }

        [Test]
        public void Can_return_DaysOfWeek_as_list()
        {
            Enum.GetNames(typeof(DayOfWeek)).ToList()
                .ForEach(x => Redis.AddItemToList("DaysOfWeek", x));
            Redis.ExecLuaAsList("return redis.call('LRANGE', 'DaysOfWeek', 0, -1)").PrintDump();
        }
    }
}