using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ScriptLispApiTests
    {
        private static ScriptContext CreateContext()
        {
            var context = new ScriptContext {
                ScriptLanguages = {ScriptLisp.Language},
                Args = {
                    ["nums3"] = new[] {0, 1, 2},
                    ["nums10"] = new[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9,},
                }
            };
            return context.Init();
        }

        [SetUp]
        public void Setup() => context = CreateContext();

        private ScriptContext context;

        string render(string lisp) => context.RenderLisp(lisp).NormalizeNewLines();
        void print(string lisp) => render(lisp).Print();
        object eval(string lisp) => context.EvaluateLisp($"(return {lisp})");

        [Test]
        public void LISP_even()
        {
            Assert.That(eval(@"(even? 2)"), Is.True);
            Assert.That(eval(@"(even? 1)"), Is.Null);
        }

        [Test]
        public void LISP_odd()
        {
            Assert.That(eval(@"(odd? 2)"), Is.Null);
            Assert.That(eval(@"(odd? 1)"), Is.True);
        }

        [Test]
        public void LISP_mapcan()
        {
            Assert.That(eval(@"(mapcan (lambda (x) (and (number? x) (list x))) '(a 1 b c 3 4 d 5))"),
                Is.EqualTo(new[] {1, 3, 4, 5}));
        }

        [Test]
        public void LISP_filter()
        {
            Assert.That(eval(@"(filter even? (range 10))"), Is.EqualTo(new[] {0, 2, 4, 6, 8}));
        }

        [Test]
        public void LISP_doseq()
        {
            Assert.That(render(@"(doseq (x nums3) (println x))"), Is.EqualTo("0\n1\n2"));
        }
 
        [Test]
        public void LISP_map_literals()
        {
            var expected = new Dictionary<string, object> {
                {"a", 1},
                {"b", 2},
                {"c", 3},
            };
            Assert.That(eval("(new-map '(a 1) '(b 2) '(c 3) )"), Is.EqualTo(expected));
            Assert.That(eval("{ :a 1 :b 2 :c 3 }"), Is.EqualTo(expected));
            Assert.That(eval("{ :a 1, :b 2, :c 3 }"), Is.EqualTo(expected));

            Assert.That(eval("{ :a 1  :b { :b1 10 :b2 20 }  :c 3 }"), Is.EqualTo(new Dictionary<string, object> {
                {"a", 1},
                {"b", new Dictionary<string, object> {
                    {"b1", 10},
                    {"b2", 20},
                }},
                {"c", 3},
            }));
        }
    }
}