using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Templates;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateMemberExpressionTests
    {
        [Test]
        public void Does_parse_member_expressions()
        {
            JsToken token;

            "a".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsIdentifier("a")));
            "a.b".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsMemberExpression(
                new JsIdentifier("a"),
                new JsIdentifier("b") 
            )));
            "a[key]".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsMemberExpression(
                new JsIdentifier("a"),
                new JsIdentifier("key"),
                computed:true
            )));
            "a['key']".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsMemberExpression(
                new JsIdentifier("a"),
                new JsLiteral("key"),
                computed:true
            )));
            "a.b.c[key]".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsMemberExpression(
                new JsMemberExpression(
                    new JsMemberExpression(
                        new JsIdentifier("a"), 
                        new JsIdentifier("b") 
                    ),
                    new JsIdentifier("c")
                ),
                new JsIdentifier("key"),
                computed:true
            )));
            
            "a[1+1]".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsMemberExpression(
                new JsIdentifier("a"),
                new JsBinaryExpression(
                    new JsLiteral(1),
                    JsAddition.Operator, 
                    new JsLiteral(1)
                ),
                computed:true
            )));
        }

        class A
        {
            public string Name { get; set; }
            public A Prop { get; set; }
            public Dictionary<string, object> StringDictionary { get; set; }
            public A[] Array { get; set; }
            public List<A> List { get; set; }
            public IEnumerable<A> Enumerable { get; set; }
            public string[] ArrayStrings { get; set; }
            public int[] ArrayInts { get; set; }
            public List<string> ListStrings { get; set; }
            public List<int> ListInts { get; set; }
            public Indexer Indexer { get; set; }
            public IntIndexer IntIndexer { get; set; }
            public StringIndexer StringIndexer { get; set; }
        }

        class Indexer
        {
            public A this[string index] => new A { Name = index };
        }
        class IntIndexer
        {
            public int this[int index] => index;
        }
        class StringIndexer
        {
            public string this[string index] => index;
        }

        [Test]
        public void Does_Evaluate_property_binding_expression()
        {
            var a = new A { Name = "foo", Prop = new A { Name = "bar", Prop = new A { Name = "qux" }}};
            var context = new TemplateContext {
                Args = {
                    ["a"] = a
                }
            }.Init();
            
            Assert.That(context.EvaluateTemplate("{{ a.Name }}"), Is.EqualTo("foo"));
            Assert.That(context.EvaluateTemplate("{{ a.Prop.Name }}"), Is.EqualTo("bar"));
            Assert.That(context.EvaluateTemplate("{{ a.Prop.Prop.Name }}"), Is.EqualTo("qux"));
        }

        [Test]
        public void Does_Evaluate_property_collection_binding_expression()
        {
            var queue = new Queue<A>();
            queue.Enqueue(new A { Name = "enumerable[0]" });
            
            var a = new A {
                Prop = new A { Name = "prop" },
                Array = new []{ new A { Name = "array[0]" } },
                List = new List<A> { new A { Name = "list[0]" } }, 
                ArrayStrings = new[] { "A", "B", "C" },
                ArrayInts = new []{ 1 },
                ListStrings = new[] { "A", "B", "C" }.ToList(),
                ListInts = new []{ 1 }.ToList(),
                Enumerable = queue,
                StringDictionary = new Dictionary<string, object> {
                    {"key", new A { Name = "StringDictionary[key]" }}
                },
                Indexer = new Indexer(),
                StringIndexer = new StringIndexer(),
                IntIndexer = new IntIndexer(), 
            };
            var context = new TemplateContext {
                Args = {
                    ["a"] = a,
                    ["keyName"] = "key",
                    ["propName"] = "Prop",
                }
            }.Init();
            
            Assert.That(context.EvaluateTemplate("{{ a.ArrayStrings[0] }}"), Is.EqualTo("A"));
            Assert.That(context.EvaluateTemplate("{{ a.ArrayInts[0] }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateTemplate("{{ a.ListStrings[0] }}"), Is.EqualTo("A"));
            Assert.That(context.EvaluateTemplate("{{ a.ListInts[0] }}"), Is.EqualTo("1"));
            
            Assert.That(context.EvaluateTemplate("{{ a.Array[0].Name }}"), Is.EqualTo("array[0]"));
            Assert.That(context.EvaluateTemplate("{{ a.List[0].Name }}"), Is.EqualTo("list[0]"));
            
            Assert.That(context.EvaluateTemplate("{{ a.Enumerable[0].Name }}"), Is.EqualTo("enumerable[0]"));
            Assert.That(context.EvaluateTemplate("{{ a.StringDictionary['key'].Name }}"), Is.EqualTo("StringDictionary[key]"));
            Assert.That(context.EvaluateTemplate("{{ a.StringDictionary[keyName].Name }}"), Is.EqualTo("StringDictionary[key]"));
            Assert.That(context.EvaluateTemplate("{{ a.StringDictionary.key.Name }}"), Is.EqualTo("StringDictionary[key]"));

            Assert.That(context.EvaluateTemplate("{{ a[propName].Name }}"), Is.EqualTo("prop"));
            Assert.That(context.EvaluateTemplate("{{ a['Prop'].Name }}"), Is.EqualTo("prop"));

            Assert.That(context.EvaluateTemplate("{{ a.Indexer.idx.Name }}"), Is.EqualTo("idx"));
            Assert.That(context.EvaluateTemplate("{{ a.Indexer['idx'].Name }}"), Is.EqualTo("idx"));
            
            Assert.That(context.EvaluateTemplate("{{ a.ArrayStrings[1+1] }}"), Is.EqualTo("C"));
            Assert.That(context.EvaluateTemplate("{{ a.ListStrings[1+1] }}"), Is.EqualTo("C"));
            Assert.That(context.EvaluateTemplate("{{ a['Pr' + 'op'].Name }}"), Is.EqualTo("prop"));
        }

        [Test]
        public void Index_access_to_non_existent_key_returns_null()
        {
            var a = new A {
                StringDictionary = new Dictionary<string, object>()
            };
            var context = new TemplateContext {
                Args = {
                    ["a"] = a,
                    ["keyName"] = "key",
                }
            }.Init();

            Assert.That(context.EvaluateTemplate("{{ a.StringDictionary['notfound'] }}"), Is.EqualTo(""));
        }

        [Test]
        public void Index_access_to_non_existent_property_throws_ArgumentException()
        {
            var a = new A {
                StringDictionary = new Dictionary<string, object>()
            };
            var context = new TemplateContext {
                Args = {
                    ["a"] = a,
                    ["keyName"] = "key",
                }
            }.Init();

            Assert.Throws<BindingExpressionException>(() => 
                context.EvaluateTemplate("{{ a.notfound }}"));
        }
    }
}