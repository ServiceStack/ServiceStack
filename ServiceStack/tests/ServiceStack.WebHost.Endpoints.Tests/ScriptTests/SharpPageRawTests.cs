using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Caching;
using ServiceStack.IO;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ModelBinding
    {
        public int Int { get; set;  }
            
        public string Prop { get; set; }
            
        public NestedModelBinding Object { get; set; }
            
        public Dictionary<string, ModelBinding> Dictionary { get; set; }
            
        public List<ModelBinding> List { get; set; }
            
        public ModelBinding this[int i]
        {
            get => List[i];
            set => List[i] = value;
        }
    }
        
    public class NestedModelBinding
    {
        public int Int { get; set;  }
            
        public string Prop { get; set; }
            
        public ModelBinding Object { get; set; }
            
        public AltNested AltNested { get; set; }
            
        public Dictionary<string, ModelBinding> Dictionary { get; set; }
            
        public List<ModelBinding> List { get; set; }
    }
        
    public class AltNested
    {
        public string Field { get; set; }
    }


    public class SharpPageRawTests
    {
        [Test]
        public async Task Can_generate_html_template_with_layout_in_memory()
        {
            var context = new ScriptContext().Init();

            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
  <title>{{ title }}</title>
</head>
<body>
  {{ page }}
</body>");

            context.VirtualFiles.WriteFile("page.html", @"<h1>{{ title }}</h1>");

            var page = context.GetPage("page");
            var result = new PageResult(page)
            {
                Args =
                {
                    {"title", "The Title"},
                }
            };

            var html = await result.RenderToStringAsync();
            
            Assert.That(html, Is.EqualTo(@"
<html>
  <title>The Title</title>
</head>
<body>
  <h1>The Title</h1>
</body>"));
        }

        [Test]
        public async Task Can_generate_markdown_template_with_layout_in_memory()
        {
            var context = new ScriptContext
            {
                PageFormats =
                {
                    new MarkdownPageFormat()
                }
            }.Init();;
            
            context.VirtualFiles.WriteFile("_layout.md", @"
# {{ title }}

Brackets in Layout < & > 

{{ page }}");

            context.VirtualFiles.WriteFile("page.md",  @"## {{ title }}");

            var page = context.GetPage("page");
            var result = new PageResult(page)
            {
                Args =
                {
                    {"title", "The Title"},
                },
                ContentType = MimeTypes.Html,
                OutputTransformers = { MarkdownPageFormat.TransformToHtml },
            };

            var html = await result.RenderToStringAsync();
            
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"<h1>The Title</h1>
<p>Brackets in Layout &lt; &amp; &gt; </p>
<h2>The Title</h2>".NormalizeNewLines()));
            
        }

        [Test]
        public async Task Can_generate_markdown_template_with_html_layout_in_memory()
        {
            var context = new ScriptContext
            {
                PageFormats =
                {
                    new MarkdownPageFormat()
                }
            }.Init();;
            
            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
  <title>{{ title }}</title>
</head>
<body>
  {{ page }}
</body>");

            context.VirtualFiles.WriteFile("page.md",  @"### {{ title }}");

            var page = context.GetPage("page");
            var result = new PageResult(page)
            {
                Args =
                {
                    {"title", "The Title"},
                },
                ContentType = MimeTypes.Html,
                PageTransformers = { MarkdownPageFormat.TransformToHtml },
            };

            var html = await result.RenderToStringAsync();
            
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"<html>
  <title>The Title</title>
</head>
<body>
  <h3>The Title</h3>

</body>".NormalizeNewLines()));
        }

        [Test]
        public async Task Does_explode_Model_properties_into_scope()
        {
            var context = new ScriptContext().Init();
            
            context.VirtualFiles.WriteFile("page.html", @"Id: {{ Id }}, Name: {{ Name }}");
            
            var result = await new PageResult(context.GetPage("page"))
            {
                Model = new Model { Id = 1, Name = "<foo>" }
            }.RenderToStringAsync();
            
            Assert.That(result, Is.EqualTo("Id: 1, Name: &lt;foo&gt;"));
        }

        [Test]
        public async Task Does_explode_Model_properties_of_anon_object_into_scope()
        {
            var context = new ScriptContext().Init();
            
            context.VirtualFiles.WriteFile("page.html", @"Id: {{ Id }}, Name: {{ Name }}");
            
            var result = await new PageResult(context.GetPage("page"))
            {
                Model = new { Id = 1, Name = "<foo>" }
            }.RenderToStringAsync();
            
            Assert.That(result, Is.EqualTo("Id: 1, Name: &lt;foo&gt;"));
        }

        [Test]
        public async Task Does_reload_modified_page_contents_in_DebugMode()
        {
            var context = new ScriptContext
            {
                DebugMode = true, //default
            }.Init();
            
            context.VirtualFiles.WriteFile("page.html", "<h1>Original</h1>");
            Assert.That(await new PageResult(context.GetPage("page")).RenderToStringAsync(), Is.EqualTo("<h1>Original</h1>"));

            await Task.Delay(1); //Memory VFS is too fast!
            
            context.VirtualFiles.WriteFile("page.html", "<h1>Updated</h1>");
            Assert.That(await new PageResult(context.GetPage("page")).RenderToStringAsync(), Is.EqualTo("<h1>Updated</h1>"));
        }

        [Test]
        public void Context_Throws_FileNotFoundException_when_page_does_not_exist()
        {
            var context = new ScriptContext();

            Assert.That(context.Pages.GetPage("not-exists.html"), Is.Null);

            try
            {
                var page = context.GetPage("not-exists.html");
                Assert.Fail("Should throw");
            }
            catch (FileNotFoundException e)
            {
                e.ToString().Print();
            }
        }

        class MyFilter : ScriptMethods
        {
            public string echo(string text) => $"{text} {text}";
            public double squared(double value) => value * value;
            public string greetArg(string key) => $"Hello {Context.Args[key]}";
            
            public ICacheClient Cache { get; set; }
            public string fromCache(string key) => Cache.Get<string>(key);
        }

        [Test]
        public void Does_use_custom_filter()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["contextArg"] = "foo"
                },
                ScriptMethods = { new MyFilter() }
            }.Init();
            
            var output = context.EvaluateScript("<p>{{ 'contextArg' |> greetArg }}</p>"); 
            Assert.That(output, Is.EqualTo("<p>Hello foo</p>"));

            output = context.EvaluateScript("<p>{{ 10 |> squared }}</p>");
            Assert.That(output, Is.EqualTo("<p>100</p>"));
            
            output = new PageResult(context.OneTimePage("<p>{{ 'hello' |> echo }}</p>"))
            {
                ScriptMethods = { new MyFilter() }
            }.Result;
            Assert.That(output, Is.EqualTo("<p>hello hello</p>"));

            context = new ScriptContext
            {
                ScanTypes = { typeof(MyFilter) },
            };
            context.Container.AddSingleton<ICacheClient>(() => new MemoryCacheClient());
            context.Container.Resolve<ICacheClient>().Set("key", "foo");
            context.Init();
            
            output = context.EvaluateScript("<p>{{ 'key' |> fromCache }}</p>");
            Assert.That(output, Is.EqualTo("<p>foo</p>"));
        }

        [Test]
        public async Task Does_embed_partials()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["copyright"] = "Copyright &copy; ServiceStack 2008-2017",
                    ["footer"] = "global-footer"
                }
            }.Init();
            
            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
<head><title>{{ title }}</title></head>
<body>
{{ 'header' |> partial }}
<div id='content'>{{ page }}</div>
{{ footer |> partial }}
</body>
</html>
");
            context.VirtualFiles.WriteFile("header.html", "<header>{{ pageTitle |> titleCase }}</header>");
            context.VirtualFiles.WriteFile("page.html", "<h2>{{ contentTitle }}</h2><section>{{ 'page-content' |> partial }}</section>");
            context.VirtualFiles.WriteFile("page-content.html", "<p>{{ contentBody |> padRight(20,'.') }}</p>");
            context.VirtualFiles.WriteFile("global-footer.html", "<footer>{{ copyright |> raw }}</footer>");
            
            var result = await new PageResult(context.GetPage("page"))
            {
                Args =
                {
                    ["pageTitle"] = "I'm in your header",
                    ["contentTitle"] = "Content is King!",
                    ["contentBody"] = "About this page",
                }
            }.RenderToStringAsync();
            
            Assert.That(result.NormalizeNewLines(), Is.EqualTo(@"
<html>
<head><title></title></head>
<body>
<header>I&#39;m In Your Header</header>
<div id='content'><h2>Content is King!</h2><section><p>About this page.....</p></section></div>
<footer>Copyright &copy; ServiceStack 2008-2017</footer>
</body>
</html>
".NormalizeNewLines()));
        }

        private static ModelBinding CreateModelBinding()
        {
            var model = new ModelBinding
            {
                Int = 1,
                Prop = "The Prop",
                Object = new NestedModelBinding
                {
                    Int = 2,
                    Prop = "Nested Prop",
                    Object = new ModelBinding
                    {
                        Int = 21,
                        Prop = "Nested Nested Prop",
                    },
                    AltNested = new AltNested
                    {
                        Field = "Object AltNested Field"
                    }
                },
                Dictionary = new Dictionary<string, ModelBinding>
                {
                    {
                        "map-key",
                        new ModelBinding
                        {
                            Int = 3,
                            Prop = "Dictionary Prop",
                            Object = new NestedModelBinding
                            {
                                Int = 5,
                                Prop = "Nested Dictionary Prop",
                                AltNested = new AltNested
                                {
                                    Field = "Dictionary AltNested Field"
                                }
                            }
                        }
                    },
                },
                List = new List<ModelBinding>
                {
                    new ModelBinding
                    {
                        Int = 4,
                        Prop = "List Prop",
                        Object = new NestedModelBinding {Int = 5, Prop = "Nested List Prop"}
                    }
                }
            };
            return model;
        }

        [Test]
        public async Task Does_evaluate_variable_binding_expressions()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["key"] = "the-key",
                }
            }.Init();
            
            context.VirtualFiles.WriteFile("page.html", @"Prop = {{ Prop }}");

            var model = CreateModelBinding();

            var pageResultArg = new NestedModelBinding
            {
                Int = 2,
                Prop = "Nested Prop",
                Object = new ModelBinding
                {
                    Int = 21,
                    Prop = "Nested Nested Prop",
                },
                AltNested = new AltNested
                {
                    Field = "Object AltNested Field"
                }
            };
            
            var result = await new PageResult(context.GetPage("page"))
            {
                Model = model,
                Args = { ["pageResultArg"] = pageResultArg }
            }.Init();

            var scope = result.CreateScope();

            object value;

            value = scope.EvaluateExpression("key");
            Assert.That(value, Is.EqualTo("the-key"));
            value = scope.EvaluateExpression("Prop");
            Assert.That(value, Is.EqualTo(model.Prop));

            value = scope.EvaluateExpression("model.Prop");
            Assert.That(value, Is.EqualTo(model.Prop));
            value = scope.EvaluateExpression("model.Object.Prop");
            Assert.That(value, Is.EqualTo(model.Object.Prop));
            value = scope.EvaluateExpression("model.Object.Object.Prop");
            Assert.That(value, Is.EqualTo(model.Object.Object.Prop));
            value = scope.EvaluateExpression("model.Object.AltNested.Field");
            Assert.That(value, Is.EqualTo(model.Object.AltNested.Field));
            value = scope.EvaluateExpression("model[0].Prop");
            Assert.That(value, Is.EqualTo(model[0].Prop));
            value = scope.EvaluateExpression("model[0].Object.Prop");
            Assert.That(value, Is.EqualTo(model[0].Object.Prop));
            value = scope.EvaluateExpression("model.List[0]");
            Assert.That(value, Is.EqualTo(model.List[0]));
            value = scope.EvaluateExpression("model.List[0].Prop");
            Assert.That(value, Is.EqualTo(model.List[0].Prop));
            value = scope.EvaluateExpression("model.List[0].Object.Prop");
            Assert.That(value, Is.EqualTo(model.List[0].Object.Prop));
            value = scope.EvaluateExpression("model.Dictionary[\"map-key\"].Prop");
            Assert.That(value, Is.EqualTo(model.Dictionary["map-key"].Prop));
            value = scope.EvaluateExpression("model.Dictionary['map-key'].Object.Prop");
            Assert.That(value, Is.EqualTo(model.Dictionary["map-key"].Object.Prop));
            value = scope.EvaluateExpression("model.Dictionary['map-key'].Object.AltNested.Field");
            Assert.That(value, Is.EqualTo(model.Dictionary["map-key"].Object.AltNested.Field));
            value = scope.EvaluateExpression("Object.AltNested.Field");
            Assert.That(value, Is.EqualTo(model.Object.AltNested.Field));
            
            value = scope.EvaluateExpression("pageResultArg.Object.Prop");
            Assert.That(value, Is.EqualTo(pageResultArg.Object.Prop));
            value = scope.EvaluateExpression("pageResultArg.AltNested.Field");
            Assert.That(value, Is.EqualTo(pageResultArg.AltNested.Field));
        }

        [Test]
        public async Task Does_evaluate_variable_binding_expressions_in_template()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["key"] = "the-key",
                }
            }.Init();
            
            context.VirtualFiles.WriteFile("page.html", @"
Object.Object.Prop = '{{ Object.Object.Prop }}'
model.Object.Object.Prop = '{{ model.Object.Object.Prop }}'
model.Dictionary['map-key'].Object.AltNested.Field = '{{ model.Dictionary['map-key'].Object.AltNested.Field }}'
model.Dictionary['map-key'].Object.AltNested.Field |> lower = '{{ model.Dictionary['map-key'].Object.AltNested.Field |> lower }}'
");

            var model = CreateModelBinding();
            
            var result = await new PageResult(context.GetPage("page")) { Model = model }.RenderToStringAsync();
            
            Assert.That(result.NormalizeNewLines(), Is.EqualTo(@"
Object.Object.Prop = 'Nested Nested Prop'
model.Object.Object.Prop = 'Nested Nested Prop'
model.Dictionary['map-key'].Object.AltNested.Field = 'Dictionary AltNested Field'
model.Dictionary['map-key'].Object.AltNested.Field |> lower = 'dictionary altnested field'
".NormalizeNewLines()));
        }

        [Test]
        public void Can_render_onetime_page_and_layout()
        {
            var context = new ScriptContext
            {                
                Args = { ["key"] = "the-key" }
            }.Init();

            var adhocPage = context.OneTimePage("<h1>{{ key }}</h1>", "html");
            var result = new PageResult(adhocPage) { Model = CreateModelBinding() }.Result;
            Assert.That(result, Is.EqualTo("<h1>the-key</h1>"));
            
            adhocPage = context.OneTimePage("<h1>{{ model.Dictionary['map-key'].Object.AltNested.Field |> lower }}</h1>", "html");
            result = new PageResult(adhocPage) { Model = CreateModelBinding() }.Result;
            Assert.That(result, Is.EqualTo("<h1>dictionary altnested field</h1>"));
            
            adhocPage = context.OneTimePage("<h1>{{ key }}</h1>", "html");
            result = new PageResult(adhocPage)
            {
                LayoutPage = context.OneTimePage("<html><title>{{ model.List[0].Object.Prop |> lower }}</title><body>{{ page }}</body></html>", "html"),
                Model = CreateModelBinding()
            }.Result;
            Assert.That(result, Is.EqualTo("<html><title>nested list prop</title><body><h1>the-key</h1></body></html>"));
        }

        [Test]
        public async Task Can_render_onetime_page_with_real_layout()
        {
            var context = new ScriptContext
            {                
                Args = { ["key"] = "the-key" }
            }.Init();
            
            context.VirtualFiles.WriteFile("_layout.html", "<html><title>{{ model.List[0].Object.Prop |> lower }}</title><body>{{ page }}</body></html>");

            var adhocPage = context.OneTimePage(@"<h1>{{ key }}</h1>", "html");
            var result = await new PageResult(adhocPage)
            {
                LayoutPage = context.GetPage("_layout"),
                Model = CreateModelBinding()
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("<html><title>nested list prop</title><body><h1>the-key</h1></body></html>"));
        }

        public class ModelWithMethods
        {
            public string Name { get; set; }

            public string GetName() => Name;
            
            public ModelWithMethods Nested { get; set; }
        }

        [Test]
        public void Does_parse_MemberExpression_methods()
        {
            JsToken token;

            "model.GetName()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(new JsIdentifier("model"), new JsIdentifier("GetName"))
            )));

            "model.Nested.GetName()".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsCallExpression(
                new JsMemberExpression(
                    new JsMemberExpression(
                        new JsIdentifier("model"),
                        new JsIdentifier("Nested")
                    ), 
                    new JsIdentifier("GetName")
                )
            )));
        }

        [Test]
        public void Does_not_allow_invoking_method_on_MemberExpression()
        {
            var context = new ScriptContext().Init();

            var model = new ModelWithMethods { Nested = new ModelWithMethods { Name = "Nested" } };
            
            try
            {
                var r = new PageResult(context.OneTimePage("{{ model.GetName() }}")){ Model = model }.Result;
                Assert.Fail("Should throw");
            }
            catch (BindingExpressionException e)
            {
                e.Message.Print();
            }

            try
            {
                var r = new PageResult(context.OneTimePage("{{ model.Nested.GetName() }}")){ Model = model }.Result;
                Assert.Fail("Should throw");
            }
            catch (BindingExpressionException e)
            {
                e.Message.Print();
            }
        }

        [Test]
        public void Binding_expressions_with_null_references_evaluate_to_null()
        {
            var context = new ScriptContext().Init();

            Assert.That(new PageResult(context.OneTimePage("{{ model.Object.Prop }}")) { Model = new ModelBinding() }.Result, Is.Empty);
            Assert.That(new PageResult(context.OneTimePage("{{ Object.Prop }}")) { Model = new ModelBinding() }.Result, Is.Empty);
            Assert.That(new PageResult(context.OneTimePage("{{ model.Object.Object.Prop }}")) { Model = new ModelBinding() }.Result, Is.Empty);
            Assert.That(new PageResult(context.OneTimePage("{{ model[0].Prop }}")) { Model = new ModelBinding() }.Result, Is.Empty);
            Assert.That(new PageResult(context.OneTimePage("{{ model.List[0].Prop }}")) { Model = new ModelBinding() }.Result, Is.Empty);
            Assert.That(new PageResult(context.OneTimePage("{{ model.Dictionary['key'].Prop }}")) { Model = new ModelBinding() }.Result, Is.Empty);
        }

        [Test]
        public void when_only_shows_code_when_true()
        {
            var context = new ScriptContext().Init();

            Assert.That(new PageResult(context.OneTimePage("{{ 'Is Authenticated' |> when(auth) }}"))
            {
                Args = {["auth"] = true }
            }.Result, Is.EqualTo("Is Authenticated"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'Is Authenticated' |> when(auth) }}"))
            {
                Args = {["auth"] = (bool?)true }
            }.Result, Is.EqualTo("Is Authenticated"));

            Assert.That(new PageResult(context.OneTimePage("{{ 'Is Authenticated' |> when(auth) }}"))
            {
                Args = {["auth"] = null}
            }.Result, Is.Empty);
            Assert.That(new PageResult(context.OneTimePage("{{ 'Is Authenticated' |> when(auth) }}"))
            {
                Args = {["auth"] = false}
            }.Result, Is.Empty);
            Assert.That(new PageResult(context.OneTimePage("{{ 'Is Authenticated' |> when(auth) }}"))
            {
                Args = {["auth"] = new AuthUserSession().IsAuthenticated}
            }.Result, Is.Empty);
            Assert.That(new PageResult(context.OneTimePage("{{ 'Is Authenticated' |> when(auth) }}")).Result, Is.Empty);
        }

        [Test]
        public void unless_shows_code_when_not_true()
        {
            var context = new ScriptContext().Init();

            Assert.That(new PageResult(context.OneTimePage("{{ 'Not Authenticated' |> unless(auth) }}"))
            {
                Args = {["auth"] = true }
            }.Result, Is.Empty);
            Assert.That(new PageResult(context.OneTimePage("{{ 'Not Authenticated' |> unless(auth) }}"))
            {
                Args = {["auth"] = (bool?)true }
            }.Result, Is.Empty);

            Assert.That(new PageResult(context.OneTimePage("{{ 'Not Authenticated' |> unless(auth) }}"))
            {
                Args = {["auth"] = null}
            }.Result, Is.EqualTo("Not Authenticated"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'Not Authenticated' |> unless(auth) }}"))
            {
                Args = {["auth"] = false}
            }.Result, Is.EqualTo("Not Authenticated"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'Not Authenticated' |> unless(auth) }}"))
            {
                Args = {["auth"] = new AuthUserSession().IsAuthenticated}
            }.Result, Is.EqualTo("Not Authenticated"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'Not Authenticated' |> unless(auth) }}")) {                
            }.Result, Is.EqualTo("Not Authenticated"));
        }

        [Test]
        public void can_use_if_and_ifNot_as_alias_to_when_and_unless()
        {
            var context = new ScriptContext().Init();

            Assert.That(new PageResult(context.OneTimePage("{{ 'Is Authenticated' |> if(auth) }}"))
            {
                Args = {["auth"] = true }
            }.Result, Is.EqualTo("Is Authenticated"));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'Not Authenticated' |> ifNot(auth) }}"))
            {
                Args = {["auth"] = true }
            }.Result, Is.Empty);
        }

        [Test]
        public void Can_use_else_and_otherwise_filter_to_show_alternative_content()
        {
            var context = new ScriptContext().Init();

            Assert.That(new PageResult(context.OneTimePage("{{ 'Not Authenticated' |> unlessElse(auth, 'Is Authenticated') }}"))
            {
                Args = {["auth"] = false }
            }.Result, Is.EqualTo("Not Authenticated"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'Not Authenticated' |> unlessElse(auth, 'Is Authenticated') }}"))
            {
                Args = {["auth"] = true }
            }.Result, Is.EqualTo("Is Authenticated"));
            

            Assert.That(new PageResult(context.OneTimePage("{{ 'Is Authenticated' |> ifElse(auth, 'Not Authenticated') }}"))
            {
                Args = {["auth"] = false }
            }.Result, Is.EqualTo("Not Authenticated"));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'Not Authenticated' |> ifNotElse(auth, 'Is Authenticated') }}"))
            {
                Args = {["auth"] = true }
            }.Result, Is.EqualTo("Is Authenticated"));
        }

        [Test]
        public void Returns_original_string_with_unknown_variable()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["serverArg"] = "defined" 
                }
            }.Init();

            Assert.That(new PageResult(context.OneTimePage("{{ undefined }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ serverArg }}")).Result, Is.EqualTo("defined"));
            Assert.That(new PageResult(context.OneTimePage("{{ serverArg |> unknownFilter }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ undefined |> titleCase }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ '' }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ null }}")).Result, Is.EqualTo(""));
        }

        [Test]
        public void Filters_with_HandleUnknownValueAttribute_handles_unkownn_values()
        {
            var context = new ScriptContext().Init();

            Assert.That(new PageResult(context.OneTimePage("{{ undefined |> otherwise('undefined serverArg') }}")).Result, Is.EqualTo("undefined serverArg"));
        }

        [Test]
        public void Handles_truthy_and_falsy_conditions()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(new PageResult(context.OneTimePage("{{ undefined |> falsy('undefined value') }}")).Result, Is.EqualTo("undefined value"));
            Assert.That(new PageResult(context.OneTimePage("{{ null      |> falsy('null value') }}")).Result, Is.EqualTo("null value"));
            Assert.That(new PageResult(context.OneTimePage("{{ ''        |> falsy('empty string') }}")).Result, Is.EqualTo("empty string"));
            Assert.That(new PageResult(context.OneTimePage("{{ false     |> falsy('false value') }}")).Result, Is.EqualTo("false value"));
            Assert.That(new PageResult(context.OneTimePage("{{ 0         |> falsy('0') }}")).Result, Is.EqualTo("0"));

            Assert.That(new PageResult(context.OneTimePage("{{ true      |> falsy('true value') }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ ' '       |> falsy('0') }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 1         |> falsy('one value') }}")).Result, Is.EqualTo(""));

            Assert.That(new PageResult(context.OneTimePage("{{ undefined |> truthy('undefined value') }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ true      |> truthy('true value') }}")).Result, Is.EqualTo("true value"));
            Assert.That(new PageResult(context.OneTimePage("{{ ' '       |> truthy('whitespace') }}")).Result, Is.EqualTo("whitespace"));
            Assert.That(new PageResult(context.OneTimePage("{{ 1         |> truthy('one value') }}")).Result, Is.EqualTo("one value"));

            Assert.That(new PageResult(context.OneTimePage("{{ null      |> truthy('null value') }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ ''        |> truthy('empty string') }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ false     |> truthy('false value') }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 0         |> truthy('0') }}")).Result, Is.EqualTo(""));
        }

        [Test]
        public void Handles_ifTruthy_and_ifFalsy_conditions()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'undefined value' |> ifFalsy(undefined) }}")).Result, Is.EqualTo("undefined value"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'null value'      |> ifFalsy(null) }}")).Result, Is.EqualTo("null value"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'empty string'    |> ifFalsy('') }}")).Result, Is.EqualTo("empty string"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'false value'     |> ifFalsy(false) }}")).Result, Is.EqualTo("false value"));
            Assert.That(new PageResult(context.OneTimePage("{{ 0                 |> ifFalsy(0) }}")).Result, Is.EqualTo("0"));

            Assert.That(new PageResult(context.OneTimePage("{{ 'true value'      |> ifFalsy(true) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'whitespace'      |> ifFalsy(' ') }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'one value'       |> ifFalsy(1) }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'undefined value' |> ifTruthy(undefined) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'null value'      |> ifTruthy(null) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'empty string'    |> ifTruthy('') }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'false value'     |> ifTruthy(false) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 0                 |> ifTruthy(0) }}")).Result, Is.EqualTo(""));

            Assert.That(new PageResult(context.OneTimePage("{{ 'true value'      |> ifTruthy(true) }}")).Result, Is.EqualTo("true value"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'whitespace'      |> ifTruthy(' ') }}")).Result, Is.EqualTo("whitespace"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'one value'       |> ifTruthy(1) }}")).Result, Is.EqualTo("one value"));
        }

        [Test]
        public void Handles_strict_if_and_else_conditions()
        {
            var context = new ScriptContext().Init();

            Assert.That(new PageResult(context.OneTimePage("{{ 'undefined value' |> ifNot(undefined) }}")).Result, Is.EqualTo("undefined value"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'null value'      |> ifNot(null) }}")).Result, Is.EqualTo("null value"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'empty string'    |> ifNot('') }}")).Result, Is.EqualTo("empty string"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'false value'     |> ifNot(false) }}")).Result, Is.EqualTo("false value"));
            Assert.That(new PageResult(context.OneTimePage("{{ 0                 |> ifNot(0) }}")).Result, Is.EqualTo("0"));

            Assert.That(new PageResult(context.OneTimePage("{{ 'true value'      |> ifNot(true) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'whitespace'      |> ifNot(' ') }}")).Result, Is.EqualTo("whitespace"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'one value'       |> ifNot(1) }}")).Result, Is.EqualTo("one value"));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'undefined value' |> if(undefined) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'null value'      |> if(null) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'empty string'    |> if('') }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'false value'     |> if(false) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 0                 |> if(0) }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'true value'      |> if(true) }}")).Result, Is.EqualTo("true value"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'whitespace'      |> if(' ') }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'one value'       |> if(1) }}")).Result, Is.EqualTo(""));
        }

        [Test]
        public void Null_exceptions_render_empty_string()
        {
            var context = new ScriptContext
            {
//                RenderExpressionExceptions = true,
                Args =
                {
                    ["contextModel"] = new ModelBinding()
                }
            }.Init();
            
            Assert.That(new PageResult(context.OneTimePage("{{ contextModel.Object.Prop }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ contextModel.Object.Prop |> otherwise('there is nothing') }}")).Result, Is.EqualTo("there is nothing"));
        }

        [Test]
        public void Can_use_whitespace_for_last_string_arg()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["ten"] = 10
                }
            }.Init();
            
            Assert.That(context.EvaluateScript(@"{{ ten |> multiply(ten) |> assignTo: result }}
                10 x 10 = {{ result }}").Trim(), Is.EqualTo("10 x 10 = 100"));
        }

        [Test]
        public void Can_emit_var_fragment_example()
        {
            var context = new ScriptContext().Init();

            var output = context.EvaluateScript("The time is now:{{ pass: now |> dateFormat('HH:mm:ss') }}");
            Assert.That(output, Is.EqualTo("The time is now:{{ now |> dateFormat('HH:mm:ss') }}"));
        }

        [Test]
        public void Does_escape_quotes_in_strings()
        {
            var context = new ScriptContext().Init();

            Assert.That(context.EvaluateScript("{{ \"string \\\"in\\\" quotes\" |> raw }}"), Is.EqualTo("string \"in\" quotes"));
            Assert.That(context.EvaluateScript("{{ 'string \\'in\\' quotes' |> raw }}"), Is.EqualTo("string 'in' quotes"));
            Assert.That(context.EvaluateScript("{{ `string \\`in\\` quotes` |> raw }}"), Is.EqualTo("string `in` quotes"));
        }

        [Test]
        public void Does_not_exceed_MaxQuota()
        {
            //times / range / itemsOf / repeat / repeating / padLeft / padRight
            
            var context = new ScriptContext().Init();

            Assert.Throws<ScriptException>(() => context.EvaluateScript("{{ 10001  |> times }}"));
            Assert.Throws<ScriptException>(() => context.EvaluateScript("{{ range(10001) }}"));
            Assert.Throws<ScriptException>(() => context.EvaluateScript("{{ range(1,10001) }}"));
            Assert.Throws<ScriptException>(() => context.EvaluateScript("{{ 10001  |> itemsOf(1) }}"));
            Assert.Throws<ScriptException>(() => context.EvaluateScript("{{ 'text' |> repeat(10001) }}"));
            Assert.Throws<ScriptException>(() => context.EvaluateScript("{{ 10001  |> repeating('text') }}"));
            Assert.Throws<ScriptException>(() => context.EvaluateScript("{{ 'text' |> padLeft(10001) }}"));
            Assert.Throws<ScriptException>(() => context.EvaluateScript("{{ 'text' |> padLeft(10001,'.') }}"));
            Assert.Throws<ScriptException>(() => context.EvaluateScript("{{ 'text' |> padRight(10001) }}"));
            Assert.Throws<ScriptException>(() => context.EvaluateScript("{{ 'text' |> padRight(10001,'.') }}"));
        }

        [Test]
        public void Can_execute_filters_in_let_binding()
        {
            var context = new ScriptContext().Init();

            var output = context.EvaluateScript(
            @"{{ [{name:'Alice',score:50},{name:'Bob',score:40}] |> assignTo:scoreRecords }}
{{ scoreRecords 
   |> let({ name: `it['name']`, score: `it['score']`, i:`incr(index)` })
   |> select: {i}) {name} = {score}\n }}");
            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
1) Alice = 50
2) Bob = 40
".NormalizeNewLines()));
        }

        [Test]
        public void Can_use_map_to_transform_lists_into_dictionaries()
        {
            var context = new ScriptContext().Init();

            var output = context.EvaluateScript(@"{{ [[1,-1],[2,-2],[3,-3]] |> assignTo:coords }}
{{ coords 
   |> map('{ x: it[0], y: it[1] }')
   |> scopeVars
   |> select: {index |> incr}. ({x}, {y})\n
}}");
            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
1. (1, -1)
2. (2, -2)
3. (3, -3)
".NormalizeNewLines()));
        }

        [Test]
        public void Can_control_whats_emitted_on_Unhandled_expression()
        {
            var context = new ScriptContext
            {
                OnUnhandledExpression = var => var.OriginalTextUtf8
            }.Init();

            Assert.That(context.EvaluateScript("{{ unknownArg |> lower }}"), Is.EqualTo("{{ unknownArg |> lower }}"));

            context.OnUnhandledExpression = var => null;
            Assert.That(context.EvaluateScript("{{ unknownArg |> lower }}"), Is.EqualTo(""));
        }

        [Test]
        public void null_binding_on_existing_object_renders_empty_string()
        {
            var c = new Dictionary<string, object> { {"name", "the name"} };
            var context = new ScriptContext
            {
                Args =
                {
                    ["c"] = c,
                    ["it"] = new Dictionary<string, object> { {"customer", c} }
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{ c.name }}"), Is.EqualTo("the name"));
            Assert.That(context.EvaluateScript("{{ c.missing }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ it.customer |> assignTo: c }}{{ c.missing }}"), Is.EqualTo(""));
        }

    }
}