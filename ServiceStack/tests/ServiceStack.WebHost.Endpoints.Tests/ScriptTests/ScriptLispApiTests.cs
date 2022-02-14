using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.IO;
using ServiceStack.OrmLite;
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
        object eval(string lisp) => context.EvaluateLisp($"(return (let () {lisp}))");

        [Test]
        public void LISP_fn_shorthand()
        {
            Assert.That(eval(@"(#(1+ %) 2)"), Is.EqualTo(3));
            Assert.That(eval(@"(#(+ %1 %2) 2 3)"), Is.EqualTo(5));
        }

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
        public void LISP_empty()
        {
            Assert.That(eval(@"(empty? nil)"), Is.True);
            Assert.That(eval(@"(empty? ())"), Is.True);
            Assert.That(eval(@"(empty? [])"), Is.True);
            Assert.That(eval(@"(empty? (to-list []))"), Is.True);
            
            Assert.That(eval(@"(empty? '(1))"), Is.False);
            Assert.That(eval(@"(empty? [1])"), Is.False);
            Assert.That(eval(@"(empty? (to-list [1]))"), Is.False);
        }

        [Test]
        public void LISP_mapcan()
        {
            Assert.That(eval(@"(mapcan (lambda (x) (and (number? x) (list x))) '(a 1 b c 3 4 d 5))"),
                Is.EqualTo(new[] {1, 3, 4, 5}));
        }

        [Test]
        public void LISP_range()
        {
            Assert.That(eval(@"(range 5)"), Is.EqualTo(new[] {0, 1, 2, 3, 4}));
            Assert.That(eval(@"(range 10 15)"), Is.EqualTo(new[] {10, 11, 12, 13, 14}));
        }

        [Test] //filter only works with cons cells
        public void LISP_filter()
        {
            Assert.That(eval(@"(filter #(<= % 3) [5 4 1 3 9 8 6 7 2 0])"), Is.EqualTo(new[] { 1, 3, 2, 0 }));
            Assert.That(eval(@"(filter #(<= % 3) [-5 -4 -1 -3 -9 -8 -6 -7 -2 -0])"), Is.EqualTo(new[] { -5, -4, -1, -3, -9, -8, -6, -7, -2, 0 }));
            Assert.That(eval(@"(filter #(<= % 3) (to-cons (map #(- %) [5 4 1 3 9 8 6 7 2 0])))"), Is.EqualTo(new[] { -5, -4, -1, -3, -9, -8, -6, -7, -2, 0 }));
            Assert.That(eval(@"(filter   even? (range 10))"), Is.EqualTo(new[] {0, 2, 4, 6, 8}));
        }

        [Test]
        public void LISP_filter_index()
        {
            Assert.That(eval(@"
(setq digits [""zero"" ""one"" ""two"" ""three"" ""four"" ""five"" ""six"" ""seven"" ""eight"" ""nine""])
(filter-index (fn [x i] (> i (length x))) digits)"), 
                Is.EqualTo(new[] { "five", "six", "seven", "eight", "nine", }));
        }

        [Test] //filter works with both IEnumerable + Cells
        public void LISP_where()
        {
            Assert.That(eval(@"(where #(<= % 3) [5 4 1 3 9 8 6 7 2 0])"), Is.EqualTo(new[] { 1, 3, 2, 0 }));
            Assert.That(eval(@"(where #(<= % 3) [-5 -4 -1 -3 -9 -8 -6 -7 -2 -0])"), Is.EqualTo(new[] { -5, -4, -1, -3, -9, -8, -6, -7, -2, 0 }));
            Assert.That(eval(@"(where #(<= % 3) (to-cons (map #(- %) [5 4 1 3 9 8 6 7 2 0])))"), Is.EqualTo(new[] { -5, -4, -1, -3, -9, -8, -6, -7, -2, 0 }));
            Assert.That(eval(@"(where   even? (range 10))"), Is.EqualTo(new[] {0, 2, 4, 6, 8}));
        }

        [Test]
        public void LISP_where_index()
        {
            Assert.That(eval(@"
(setq digits [""zero"" ""one"" ""two"" ""three"" ""four"" ""five"" ""six"" ""seven"" ""eight"" ""nine""])
(where-index (fn [x i] (> i (length x))) digits)"), 
                Is.EqualTo(new[] { "five", "six", "seven", "eight", "nine", }));
        }

        [Test]
        public void LISP_add()
        {
            Assert.That(eval(@"(+ 1)"), Is.EqualTo(1));
            Assert.That(eval(@"(+ 1 2 3 4)"), Is.EqualTo(10));
        }

        [Test]
        public void LISP_multiply()
        {
            Assert.That(eval(@"(* 1)"), Is.EqualTo(1));
            Assert.That(eval(@"(* 1 2 3 4)"), Is.EqualTo(24));
        }

        [Test]
        public void LISP_subtract_minus()
        {
            Assert.That(eval(@"(- 10 1 2 3 4)"), Is.EqualTo(0));
            Assert.That(eval(@"(- 10)"), Is.EqualTo(-10));
            Assert.That(eval(@"-10"), Is.EqualTo(-10));
        }

        [Test]
        public void LISP_divide()
        {
            Assert.That(eval(@"(/ 6 2)"), Is.EqualTo(3));
            Assert.That(eval(@"(/ 5 2)"), Is.EqualTo(2));
            Assert.That(eval(@"(/ 5.0 2)"), Is.EqualTo(2.5));
            Assert.That(eval(@"(/ 5 2.0)"), Is.EqualTo(2.5));
            Assert.That(eval(@"(/ 5.0 2.0)"), Is.EqualTo(2.5));
            Assert.That(eval(@"(/ 4.0)"), Is.EqualTo(0.25));
            Assert.That(eval(@"(/ 4)"), Is.EqualTo(0));
            Assert.That(eval(@"(/ 25 3 2)"), Is.EqualTo(4));
            Assert.That(eval(@"(/ -17 6)"), Is.EqualTo(-2));
        }

        [Test]
        public void LISP_reduce()
        {
            Assert.That(eval(@"(reduce + [1 2 3])"), Is.EqualTo(6));
            Assert.That(eval(@"(reduce + (to-list [1 2 3]))"), Is.EqualTo(6));
            Assert.That(eval(@"(reduce * [2 3 4])"), Is.EqualTo(24));
            Assert.That(eval(@"(reduce * (to-list [2 3 4]))"), Is.EqualTo(24));
            Assert.That(eval(@"(reduce - [10 1 2 3])"), Is.EqualTo(4));
            Assert.That(eval(@"(reduce - (to-list [10 1 2 3]))"), Is.EqualTo(4));
            Assert.That(eval(@"(reduce / [10 1 2])"), Is.EqualTo(5));
            Assert.That(eval(@"(reduce / (to-list [10 1 2]))"), Is.EqualTo(5));
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

        [Test]
        public void Lisp_data_list()
        {
            Assert.That(eval(@"(sum [1 2 3 4])"), Is.EqualTo(10));
            Assert.That(eval(@"(sum [1, 2, 3, 4])"), Is.EqualTo(10));
        }

        [Test]
        public void LISP_do() //https://clojuredocs.org/clojure.core/dorun
        {
            var context = new ScriptContext {
                ScriptLanguages = { ScriptLisp.Language }
            }.Init();

            Assert.That(context.EvaluateLisp(@"(return (do))"), Is.Null);
            Assert.That(context.EvaluateLisp(@"(return (do ()))"), Is.Null);
            Assert.That(context.EvaluateLisp(@"(return (do (+ 1 1) (+ 2 2) ))"), Is.EqualTo(4));
            Assert.That(context.EvaluateLisp(@"(return (do (+ 1 1) (+ 2 2) nil ))"), Is.Null);
        }

        [Test]
        public void LISP_can_clojure_fn_data_list_args()
        {
            Assert.That(render(@"(defn f [] 0)(f)"), Is.EqualTo("0"));
            Assert.That(render(@"(defn f [a] a)(f 1)"), Is.EqualTo("1"));
            Assert.That(render(@"(defn f [a b] (+ a b))(f 1 2)"), Is.EqualTo("3"));
            Assert.That(render(@"(defn f [a b c] (+ a b c))(f 1 2 3)"), Is.EqualTo("6"));
            Assert.That(render(@"((fn [a b c] (+ a b c)) 1 2 3)"), Is.EqualTo("6"));
        }
        
        [Test]
        public void LISP_can_call_xml_ContextBlockFilter()
        {
            var obj = eval(@"(/xml { :a 1 } )");
            Assert.That(obj, Does.Contain("<Key>a</Key>"));
        }

        [Test]
        public void LISP_test_void_variable()
        {
            Assert.That(eval(@"(if (bound? id) 1 -1)"), Is.EqualTo(-1));
            Assert.That(eval(@"(setq id 2)(if (bound? id) 1 -1)"), Is.EqualTo(1));

            Assert.That(eval(@"(if (bound? id id2) 1 -1)"), Is.EqualTo(-1));
            Assert.That(eval(@"(setq id 2)(if (bound? id id2) 1 -1)"), Is.EqualTo(-1));
            Assert.That(eval(@"(setq id 2)(setq id2 3)(if (bound? id id2) 1 -1)"), Is.EqualTo(1));
        }

        [Test]
        public void LISP_butlast()
        {
            Assert.That(eval(@"(butlast [1 2 3 4])"), Is.EqualTo(new[]{ 1, 2, 3 }));
            Assert.That(eval(@"(butlast (to-list [1 2 3 4]))"), Is.EqualTo(new[]{ 1, 2, 3 }));
        }

        [Test]
        public void LISP_reverse()
        {
            Assert.That(eval(@"(reverse [1 2 3 4])"), Is.EqualTo(new[]{ 4, 3, 2, 1 }));
            Assert.That(eval(@"(reverse (to-list [1 2 3 4]))"), Is.EqualTo(new[]{ 4, 3, 2, 1 }));
        }

        [Test]
        public void LISP_first()
        {
            Assert.That(eval(@"(first [10 20 30])"), Is.EqualTo(10));
            Assert.That(eval(@"(first (to-list [10 20 30]))"), Is.EqualTo(10));
            Assert.That(eval(@"(1st [10 20 30])"), Is.EqualTo(10));
        }

        [Test]
        public void LISP_second()
        {
            Assert.That(eval(@"(second [10 20 30])"), Is.EqualTo(20));
            Assert.That(eval(@"(second (to-list [10 20 30]))"), Is.EqualTo(20));
            Assert.That(eval(@"(second [10])"), Is.Null);
            Assert.That(eval(@"(second (to-list [10]))"), Is.Null);
            Assert.That(eval(@"(2nd [10 20 30])"), Is.EqualTo(20));
        }

        [Test]
        public void LISP_third()
        {
            Assert.That(eval(@"(third [10 20 30])"), Is.EqualTo(30));
            Assert.That(eval(@"(third (to-list [10 20 30]))"), Is.EqualTo(30));
            Assert.That(eval(@"(third [10])"), Is.Null);
            Assert.That(eval(@"(third (to-list [10]))"), Is.Null);
            Assert.That(eval(@"(3rd [10 20 30])"), Is.EqualTo(30));
        }

        [Test]
        public void LISP_rest()
        {
            Assert.That(eval(@"(rest [10 20 30])"), Is.EqualTo(new[]{ 20, 30 }));
            Assert.That(eval(@"(rest (to-list [10 20 30]))"), Is.EqualTo(new[]{ 20, 30 }));
            Assert.That(eval(@"(rest [10])"), Is.Null);
            Assert.That(eval(@"(rest (to-list [10]))"), Is.Null);
            Assert.That(eval(@"(next [10 20 30])"), Is.EqualTo(new[]{ 20, 30 }));
        }

        [Test]
        public void LISP_flatten()
        {
            Assert.That(eval(@"(flatten [1 2 3])"), Is.EqualTo(new[]{ 1, 2, 3 }));
            Assert.That(eval(@"(flatten (to-list [1 2 3]))"), Is.EqualTo(new[]{ 1, 2, 3 }));
            Assert.That(eval(@"(flatten [1 2 [3 4] 5 [6 [7 [8 9]]]])"), Is.EqualTo(new[]{ 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
            Assert.That(eval(@"(flatten (to-list [1 2 [3 4] 5 [6 [7 [8 9]]]]))"), Is.EqualTo(new[]{ 1, 2, 3, 4, 5, 6, 7, 8, 9 }));

            object reval(string s) => eval(s.Replace("'", "\""));
            Assert.That(reval(@"(flatten ['A' 'B' 'C'])"), Is.EqualTo(new[]{ "A", "B", "C" }));
            Assert.That(reval(@"(flatten (to-list ['A' 'B' 'C']))"), Is.EqualTo(new[]{ "A", "B", "C" }));
            Assert.That(reval(@"(flatten ['A' 'B' ['C' 'D'] 'E' ['F' ['G' ['H' 'I']]]])"), Is.EqualTo(new[]{ "A", "B", "C", "D", "E", "F", "G", "H", "I" }));
            Assert.That(reval(@"(flatten (to-list ['A' 'B' ['C' 'D'] 'E' ['F' ['G' ['H' 'I']]]]))"), Is.EqualTo(new[]{ "A", "B", "C", "D", "E", "F", "G", "H", "I"  }));
        }

        [Test]
        public void LISP_min()
        {
            Assert.That(eval(@"(min 10 20)"), Is.EqualTo(10));
            Assert.That(eval(@"(min 30 10 20)"), Is.EqualTo(10));
            Assert.That(eval(@"(apply min [5 4 3 9 8 6 7 2])"), Is.EqualTo(2));
            Assert.That(eval(@"(apply min (to-list [5 4 3 9 8 6 7 2]))"), Is.EqualTo(2));
        }

        [Test]
        public void LISP_max()
        {
            Assert.That(eval(@"(max 10 20)"), Is.EqualTo(20));
            Assert.That(eval(@"(max 30 10 20)"), Is.EqualTo(30));
            Assert.That(eval(@"(apply max [5 4 3 9 8 6 7 2])"), Is.EqualTo(9));
            Assert.That(eval(@"(apply max (to-list [5 4 3 9 8 6 7 2]))"), Is.EqualTo(9));
        }

        [Test]
        public void LISP_index()
        {
            Assert.That(eval(@"(:0 ""i"")"), Is.EqualTo('i'));
            Assert.That(eval(@"(nth ""i"" 0)"), Is.EqualTo('i'));
        }

        [Test]
        public void Can_access_page_vars()
        {
            Assert.That(context.EvaluateLisp(@"<!--
id 1
-->

(return (let () 
    (if (bound? id) 1 -1)

))"), Is.EqualTo(1));
            
        }

        [Test]
        public void Can_access_page_vars_with_line_comment_prefix()
        {
            Assert.That(context.EvaluateLisp(@";<!--
; id 1
;-->

(return (let () 
    (if (bound? id) 1 -1)

))"), Is.EqualTo(1));
            
        }

        [Test]
        public void LISP_string_format()
        {
            Assert.That(render(@"(/fmt ""{0} + {1} = {2}"" 1 2 (+ 1 2))"), 
                Is.EqualTo("1 + 2 = 3"));
        }

        [Test]
        public void LISP_Instanceof_tests()
        {
            var context = LoadLispContext(c => c.AllowScriptingOfAllTypes = true);
            object eval(string lisp) => context.EvaluateLisp($"(return (let () {lisp}))");
            
            Assert.That(eval("(instance? 'IEnumerable [1])"), Is.True);
            Assert.That(eval("(instance? \"IEnumerable\" [1])"), Is.True);
            Assert.That(eval("(instance? 'System.Collections.IEnumerable [1])"), Is.True);
            Assert.That(eval("(instance? 'IEnumerable 1)"), Is.Null);
        }

        [Test]
        public void Can_access_db()
        {
            var context = new ScriptContext {
                ScriptLanguages = { ScriptLisp.Language },
                ScriptMethods = {
                    new DbScriptsAsync()
                }
            };
            context.Container.AddSingleton<IDbConnectionFactory>(() => 
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));
            context.Init();

            using (var db = context.Container.Resolve<IDbConnectionFactory>().Open())
            {
                db.CreateTable<Person>();
                
                db.InsertAll(new [] {
                    new Person("A", 1), 
                    new Person("B", 2), 
                });
            }

            var result = context.EvaluateLisp(
                @"(return (map (fn [p] (:Name p)) (/dbSelect ""select Name, Age from Person"")))");
            Assert.That(result, Is.EqualTo(new[] { "A", "B" }));
        }

        private static ScriptContext LoadLispContext(Action<ScriptContext> fn=null)
        {
            var context = new ScriptContext {
                ScriptLanguages = {ScriptLisp.Language},
                ScriptMethods = {new ProtectedScripts()},
                ScriptNamespaces = { // same as SharpPagesFeature
                    "System",
                    "System.Collections",
                    "System.Collections.Generic",
                    "ServiceStack",
                }
            };
            fn?.Invoke(context);
            return context.Init();;
        }

        [Test]
        public void Can_load_scripts()
        {
            var context = LoadLispContext();
            
            context.VirtualFiles.WriteFile("lib1.l", "(defn lib-calc [a b] (+ a b))");
            context.VirtualFiles.WriteFile("/dir/lib2.l", "(defn lib-calc [a b] (* a b))");

            object result;
            
            result = context.EvaluateLisp(@"(load 'lib1)(return (lib-calc 4 5))");
            Assert.That(result, Is.EqualTo(9));
            
            result = context.EvaluateLisp(@"(load ""lib1.l"")(return (lib-calc 4 5))");
            Assert.That(result, Is.EqualTo(9));
            
            result = context.EvaluateLisp(@"(load ""/dir/lib2.l"")(return (lib-calc 4 5))");
            Assert.That(result, Is.EqualTo(20));
            result = context.EvaluateLisp(@"(load 'lib1)(load ""/dir/lib2.l"")(return (lib-calc 4 5))");
            Assert.That(result, Is.EqualTo(20));
            
            // https://gist.github.com/gistlyn/2f14d629ba1852ee55865607f1fa2c3e
        }

//        [Test] // skip integration test
        public void Can_load_scripts_from_gist_and_url()
        {
//            Lisp.AllowLoadingRemoteScripts = false; // uncomment to prevent loading remote scripts

            var context = LoadLispContext();

            LoadLispTests(context);
            LoadLispTests(context); // load twice to check it's using cached downloaded assets
        }

//        [Test]
        public void Can_load_parse_rss_and_evaluate_rss_feed()
        {
            var context = LoadLispContext(c => {
                //c.AllowScriptingOfAllTypes = true;
                c.ScriptTypes.Add(typeof(List<>));
                c.ScriptTypes.Add(typeof(ObjectDictionary));
                c.ScriptTypes.Add(typeof(XDocument));
                c.ScriptTypes.Add(typeof(XLinqExtensions));
            });

            var result = context.EvaluateLisp(@"(load ""index:parse-rss"")(return (parse-rss (/urlContents ""https://news.ycombinator.com/rss"")))");
            result.PrintDump();
        }

        private static void LoadLispTests(ScriptContext context)
        {
            object result;

            result = context.EvaluateLisp(@"(load ""gist:2f14d629ba1852ee55865607f1fa2c3e/lib1.l"")(return (lib-calc 4 5))");
            Assert.That(result, Is.EqualTo(9));

            // imports all gist files and overwrites symbols where last symbol wins
            result = context.EvaluateLisp(@"(load ""gist:2f14d629ba1852ee55865607f1fa2c3e"")(return (lib-calc 4 5))");
            Assert.That(result, Is.EqualTo(20));

            result = context.EvaluateLisp(
                @"(load ""https://gist.githubusercontent.com/gistlyn/2f14d629ba1852ee55865607f1fa2c3e/raw/95cbc5d071d9db3a96866c1a583056dd87ab5f69/lib1.l"")(return (lib-calc 4 5))");
            Assert.That(result, Is.EqualTo(9));

            // import single file from index.md
            result = context.EvaluateLisp(@"(load ""index:lib-calc/lib1.l"")(return (lib-calc 4 5))");
            Assert.That(result, Is.EqualTo(9));

            // imports all gist files and overwrites symbols where last symbol wins
            result = context.EvaluateLisp(@"(load ""index:lib-calc"")(return (lib-calc 4 5))");
            Assert.That(result, Is.EqualTo(20));
        }

//        [Test]
        public void Can_load_src()
        {
            object result;

            var context = LoadLispContext();
            object eval(string lisp) => context.EvaluateLisp($"(return (let () {lisp}))");

            result = eval(@"(load-src ""gist:2f14d629ba1852ee55865607f1fa2c3e/lib1.l"")");
            result.ToString().Print();

            // imports all gist files and overwrites symbols where last symbol wins
            result = eval(@"(load-src ""gist:2f14d629ba1852ee55865607f1fa2c3e"")");
            result.ToString().Print();

            result = eval(@"(load-src ""https://gist.githubusercontent.com/gistlyn/2f14d629ba1852ee55865607f1fa2c3e/raw/95cbc5d071d9db3a96866c1a583056dd87ab5f69/lib1.l"")");
            result.ToString().Print();

            // import single file from index.md
            result = eval(@"(load-src ""index:lib-calc/lib1.l"")");
            result.ToString().Print();

            // imports all gist files and overwrites symbols where last symbol wins
            result = eval(@"(load-src ""index:lib-calc"")");
            result.ToString().Print();
            
            result = eval(@"(load-src ""index:parse-rss"")");
            result.ToString().Print();
        }

        [Test]
        public void Can_set_property_using_property_Name()
        {
            var context = new ScriptContext {
                ScriptLanguages = { ScriptLisp.Language },
                ScriptMethods = { new ProtectedScripts() },
                ScriptAssemblies = {
                    typeof(Property).Assembly,
                    typeof(XDocument).Assembly,
                },
            }.Init();
            
            var result = context.EvaluateLisp("(setq a (Property.)) (.Name a \"foo\") (return (.Name a))");
            Assert.That(result, Is.EqualTo("foo"));

            result = context.EvaluateLisp(@"(setq doc (System.Xml.Linq.XDocument/Parse ""<root>orig</root>""))
(setq root (.Root doc))
(.Value root ""updated"")
(return (.Value root))");
            Assert.That(result, Is.EqualTo("updated"));
        }
    }
    
/* If LISP integration tests are needed in future
    public class ScriptListAppHostTests
    {
        private ServiceStackHost appHost;

        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(ScriptListAppHostTests), typeof(ScriptListAppHostTests).Assembly) { }
            public override void Configure(Container container)
            {
                Plugins.Add(new SharpPagesFeature {
                    ScriptLanguages = { ScriptLisp.Language },
                });
            }
        }

        public ScriptListAppHostTests()
        {
            appHost = new AppHost().Init(); 
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();
 
        string render(string lisp) => appHost.GetPlugin<SharpPagesFeature>().RenderLisp(lisp).NormalizeNewLines();
        object eval(string lisp) => appHost.GetPlugin<SharpPagesFeature>().EvaluateLisp($"(return {lisp})");

//        [Test]
        public void Can_call_urlContents()
        {
            var output = render(@"(/urlContents ""https://api.github.com/repos/ServiceStack/ServiceStack"" { :userAgent ""#Script"" } )");
            output.Print();
        }
    }
*/
}