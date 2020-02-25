using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class JsAssignmentTests
    {
        [Test]
        public void Can_assign_local_Variables()
        {
            var context = new ScriptContext().Init();
            
            var pageResult = new PageResult(context.OneTimePage("{{ var a = 1 }}a == {{a}}"));
            var output = pageResult.RenderScript();
            Assert.That(output, Is.EqualTo("a == 1"));

            pageResult = new PageResult(context.OneTimePage("{{ var a = 1, b = 1 + 1 }}b == {{b}}"));
            output = pageResult.RenderScript();
            Assert.That(output, Is.EqualTo("b == 2"));
        }
        
        [Test]
        public void Can_assign_global_Variables()
        {
            var context = new ScriptContext().Init();
            
            var pageResult = new PageResult(context.OneTimePage("a == {{a = 1}}"));
            var output = pageResult.RenderScript();
            Assert.That(output, Is.EqualTo("a == 1"));
            Assert.That(pageResult.Args["a"], Is.EqualTo(1));

            pageResult = new PageResult(context.OneTimePage("g == {{global.g = 1}}"));
            output = pageResult.RenderScript();
            Assert.That(pageResult.Args["g"], Is.EqualTo(1));
            Assert.That(output, Is.EqualTo("g == 1"));

            pageResult = new PageResult(context.OneTimePage("g == {{global['g'] = 2}}"));
            output = pageResult.RenderScript();
            Assert.That(pageResult.Args["g"], Is.EqualTo(2));
            Assert.That(output, Is.EqualTo("g == 2"));
        }

        public class GrandNestedPerson
        {
            public NestedPerson Nested { get; set; }
        }

        public class NestedPerson
        {
            public Person A { get; set; }
        }

        [Test]
        public void Can_assign_collections_and_pocos()
        {
            void populateArgs(Dictionary<string, object> args)
            {
                args["intList"] = new List<int> { 1, 2, 3 };
                args["intArray"] = new[] { 1, 2, 3 };
                args["stringList"] = new List<string> { "a", "b", "c" };
                args["stringArray"] = new [] { "a", "b", "c" };
                args["intMap"] = new Dictionary<string, int> {
                    ["a"] = 1,
                    ["b"] = 2,
                    ["c"] = 3,
                };
                args["stringMap"] = new Dictionary<string, string> {
                    ["a"] = "A",
                    ["b"] = "B",
                    ["c"] = "C",
                };
                args["person"] = new Person {
                    Age = 27,
                    Name = "Kurt",
                };
                args["nestedObjectMap"] = new Dictionary<string, object> {
                    ["person"] = new Person {
                        Age = 27,
                        Name = "Kurt",
                    },
                    ["nestedPerson"] = new NestedPerson {
                        A = new Person
                        {
                            Age = 27,
                            Name = "Kurt",
                        }
                    },
                    ["grandNestedPerson"] = new GrandNestedPerson
                    {
                        Nested = new NestedPerson {
                            A = new Person
                            {
                                Age = 27,
                                Name = "Kurt",
                            }
                        }
                    }
                };
                args["grandNestedPerson"] = new GrandNestedPerson {
                    Nested = new NestedPerson {
                        A = new Person {
                            Age = 27,
                            Name = "Kurt",
                        }
                    }
                };
            }
            
            var context = new ScriptContext();
            populateArgs(context.Args);
            context.Init();

            void assert<T>(string src, string expectedOutput, Func<Dictionary<string,object>, T> actual, T expected)
            {
                var pageResult = new PageResult(context.OneTimePage(src));
                var output = pageResult.RenderScript();
                Assert.That(output, Is.EqualTo(expectedOutput));
                Assert.That(actual(context.Args), Is.EqualTo(expected));

                var local = new ScriptContext().Init();
                pageResult = new PageResult(local.OneTimePage(src));
                populateArgs(pageResult.Args);
                output = pageResult.RenderScript();
                Assert.That(output, Is.EqualTo(expectedOutput));
                Assert.That(actual(pageResult.Args), Is.EqualTo(expected));
            }

            assert("intList[1] == {{ intList[1] = 4 }}", "intList[1] == 4", 
                args => ((List<int>)args["intList"])[1], 4);
            
            assert("intArray[1] == {{ intArray[1] = 4 }}", "intArray[1] == 4", 
                args => ((int[])args["intArray"])[1], 4);
            
            assert("stringList[1] == {{ stringList[1] = 'D' }}", "stringList[1] == D", 
                args => ((List<string>)args["stringList"])[1], "D");
            
            assert("stringArray[1] == {{ stringArray[1] = 'D' }}", "stringArray[1] == D", 
                args => ((string[])args["stringArray"])[1], "D");
            
            assert("intMap['b'] == {{ intMap['b'] = 4 }}", "intMap['b'] == 4", 
                args => ((Dictionary<string, int>)args["intMap"])["b"], 4);
            
            assert("stringMap['b'] == {{ stringMap['b'] = 'D' }}", "stringMap['b'] == D", 
                args => ((Dictionary<string, string>)args["stringMap"])["b"], "D");
            
            assert("intMap.b == {{ intMap.b = 4 }}", "intMap.b == 4", 
                args => ((Dictionary<string, int>)args["intMap"])["b"], 4);
            
            assert("stringMap.b == {{ stringMap.b = 'D' }}", "stringMap.b == D", 
                args => ((Dictionary<string, string>)args["stringMap"])["b"], "D");
            
            assert("person.Age == {{ person.Age = 30 }}", "person.Age == 30", 
                args => ((Person)args["person"]).Age, 30);
            assert("person.Name == {{ person.Name = 'Eddie' }}", "person.Name == Eddie", 
                args => ((Person)args["person"]).Name, "Eddie");

            assert("nestedObjectMap['person'].Age == {{ nestedObjectMap['person'].Age = 30 }}", "nestedObjectMap['person'].Age == 30", 
                args => ((Person)((Dictionary<string,object>)args["nestedObjectMap"])["person"]).Age, 30);
            assert("nestedObjectMap['person'].Name == {{ nestedObjectMap['person'].Name = 'Eddie' }}", "nestedObjectMap['person'].Name == Eddie", 
                args => ((Person)((Dictionary<string,object>)args["nestedObjectMap"])["person"]).Name, "Eddie");
            
            assert("nestedObjectMap.person.Age == {{ nestedObjectMap.person.Age = 30 }}", "nestedObjectMap.person.Age == 30", 
                args => ((Person)((Dictionary<string,object>)args["nestedObjectMap"])["person"]).Age, 30);
            assert("nestedObjectMap.person.Name == {{ nestedObjectMap.person.Name = 'Eddie' }}", "nestedObjectMap.person.Name == Eddie", 
                args => ((Person)((Dictionary<string,object>)args["nestedObjectMap"])["person"]).Name, "Eddie");
            
            assert("nestedObjectMap['nestedPerson'].A.Age == {{ nestedObjectMap['nestedPerson'].A.Age = 30 }}", "nestedObjectMap['nestedPerson'].A.Age == 30", 
                args => ((NestedPerson)((Dictionary<string,object>)args["nestedObjectMap"])["nestedPerson"]).A.Age, 30);
            assert("nestedObjectMap['nestedPerson'].A.Name == {{ nestedObjectMap['nestedPerson'].A.Name = 'Eddie' }}", "nestedObjectMap['nestedPerson'].A.Name == Eddie", 
                args => ((NestedPerson)((Dictionary<string,object>)args["nestedObjectMap"])["nestedPerson"]).A.Name, "Eddie");
            
            assert("nestedObjectMap['grandNestedPerson'].Nested.A.Age == {{ nestedObjectMap['grandNestedPerson'].Nested.A.Age = 30 }}", "nestedObjectMap['grandNestedPerson'].Nested.A.Age == 30", 
                args => ((GrandNestedPerson)((Dictionary<string,object>)args["nestedObjectMap"])["grandNestedPerson"]).Nested.A.Age, 30);
            assert("nestedObjectMap['grandNestedPerson'].Nested.A.Name == {{ nestedObjectMap['grandNestedPerson'].Nested.A.Name = 'Eddie' }}", "nestedObjectMap['grandNestedPerson'].Nested.A.Name == Eddie", 
                args => ((GrandNestedPerson)((Dictionary<string,object>)args["nestedObjectMap"])["grandNestedPerson"]).Nested.A.Name, "Eddie");
            
            assert("grandNestedPerson.Nested.A.Age == {{ grandNestedPerson.Nested.A.Age = 30 }}", "grandNestedPerson.Nested.A.Age == 30", 
                args => ((GrandNestedPerson)args["grandNestedPerson"]).Nested.A.Age, 30);
            assert("grandNestedPerson.Nested.A.Name == {{ grandNestedPerson.Nested.A.Name = 'Eddie' }}", "grandNestedPerson.Nested.A.Name == Eddie", 
                args => ((GrandNestedPerson)args["grandNestedPerson"]).Nested.A.Name, "Eddie");
            
            
            assert("intList[1+1] == {{ intList[1+1] = 4 }}", "intList[1+1] == 4", 
                args => ((List<int>)args["intList"])[1+1], 4);
            
            assert("stringMap[1.isEven() ? 'a' : 'b'] == {{ stringMap[1.isEven() ? 'a' : 'b'] = 'D' }}", "stringMap[1.isEven() ? 'a' : 'b'] == D", 
                args => ((Dictionary<string, string>)args["stringMap"])["b"], "D");
        }
    }
}