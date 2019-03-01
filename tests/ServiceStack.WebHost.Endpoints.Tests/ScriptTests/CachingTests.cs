using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class CachingTests
    {
        [Test]
        public void Can_use_ReadOnlyMemory_as_Dictionary_key()
        {
            var i = 0;
            var map = new ConcurrentDictionary<ReadOnlyMemory<char>,int>();
            var key = "key".AsMemory();
            map.GetOrAdd(key, _ => ++i);
            map.GetOrAdd(key, _ => ++i);

            Assert.That(i, Is.EqualTo(1));
            Assert.That(map.Count, Is.EqualTo(1));

            map.GetOrAdd("key".AsMemory(), _ => ++i);
            map.GetOrAdd("key".AsMemory(), _ => ++i);
            Assert.That(i, Is.EqualTo(1));
            Assert.That(map.Count, Is.EqualTo(1));
        }

        [Test]
        public void Cant_use_ReadOnlyMemory_from_different_string_as_Dictionary_Key()
        {
            var i = 0;
            var map = new ConcurrentDictionary<ReadOnlyMemory<char>, int>();
            var key = "key".AsMemory();
            map.GetOrAdd("1key".AsMemory().Slice(1), _ => ++i);
            map.GetOrAdd("2key".AsMemory().Slice(1), _ => ++i);

            Assert.That("1key".AsMemory().Slice(1).ToString(), Is.EqualTo("key"));
            Assert.That("2key".AsMemory().Slice(1).ToString(), Is.EqualTo("key"));

            Assert.That(i, Is.EqualTo(2));
            Assert.That(map.Count, Is.EqualTo(2));
        }

        [Test]
        public void Unique_Template_Should_Be_Cached_Only_Once()
        {
            var context = new ScriptContext
            {
                ScriptBlocks = { new EvalScriptBlock() },
                Args = {
                    ["templates"] = new List<string> {
                        "1. {{income ?? 1000}} - {{expenses}}",
                        "2. {{income ?? 2000}} - {{expenses}}",
                        "3. {{income ?? 3000}} - {{expenses}}",
                    }
                }
            }.Init();

            10000.Times(() =>
            {
                var result = context.EvaluateScript(@"{{#each templates}}{{index}}{{/each}}");
                Assert.That(result, Is.EqualTo("012"));
            });

            Assert.That(context.Cache.Count, Is.EqualTo(1));
        }    
    }
}