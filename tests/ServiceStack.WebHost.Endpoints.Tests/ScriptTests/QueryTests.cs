using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class QueryTests
    {
        [Test]
        public void Take_does_limit_KVP_Objects()
        {
            var context = new ScriptContext {
                Args = {
                    ["items"] = new List<KeyValuePair<string, object>> {
                        new KeyValuePair<string, object>("A", 1),
                        new KeyValuePair<string, object>("B", 2),
                        new KeyValuePair<string, object>("C", 3),
                    }
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{ items | textDump }}").NormalizeNewLines(), Is.EqualTo(@"|||
|-|-|
| A | 1 |
| B | 2 |
| C | 3 |".NormalizeNewLines()));

            Assert.That(context.EvaluateScript("{{ items | take(2) | textDump }}").NormalizeNewLines(), Is.EqualTo(@"|||
|-|-|
| A | 1 |
| B | 2 |".NormalizeNewLines()));
        }

        [Test]
        public void Take_does_limit_KVP_longs()
        {
            var context = new ScriptContext {
                Args = {
                    ["items"] = new List<KeyValuePair<string, long>> {
                        new KeyValuePair<string, long>("A", 1),
                        new KeyValuePair<string, long>("B", 2),
                        new KeyValuePair<string, long>("C", 3),
                    }
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{ items | textDump }}").NormalizeNewLines(), Is.EqualTo(@"|||
|-|-|
| A | 1 |
| B | 2 |
| C | 3 |".NormalizeNewLines()));

            Assert.That(context.EvaluateScript("{{ items | take(2) | textDump }}").NormalizeNewLines(), Is.EqualTo(@"|||
|-|-|
| A | 1 |
| B | 2 |".NormalizeNewLines()));
        }
    }
}