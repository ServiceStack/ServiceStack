using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Script;
using ServiceStack.Testing;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class SharpPageContextScriptMethodsTests
    {
        [Test]
        public void Can_pass_variables_into_partials()
        {
            var context = new ScriptContext
            {
                Args = { ["defaultMessage"] = "this is the default message" }
            }.Init();

            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
  <title>{{ title }}</title>
</head>
<body>
{{ 'header' |> partial({ id: 'the-page', message: 'in your header' }) }}
{{ page }}
</body>");

            context.VirtualFiles.WriteFile("header.html", @"
<header id='{{ id |> otherwise('header') }}'>
  {{ message |> otherwise(defaultMessage) }}
</header>");

            context.VirtualFiles.WriteFile("page.html", @"<h1>{{ title }}</h1>");

            var result = new PageResult(context.GetPage("page")) 
            {
                Args = { ["title"] = "The title" }
            }.Result;
            Assert.That(result.NormalizeNewLines(), Is.EqualTo(@"
<html>
  <title>The title</title>
</head>
<body>

<header id='the-page'>
  in your header
</header>
<h1>The title</h1>
</body>
".NormalizeNewLines()));            
        }

        [Test]
        public void Can_load_page_with_partial_and_scoped_variables()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["myPartial"] = "my-partial"
                }
            }.Init();

            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
  <title>{{ title }}</title>
</head>
<body>
{{ 'my-partial' |> partial({ title: 'with-partial', tag: 'h2' }) }}
{{ myPartial |> partial({ title: 'with-partial-binding', tag: 'h2' }) }}
<footer>{{ title }}</footer>
</body>");
            
            context.VirtualFiles.WriteFile("my-partial.html", "<{{ tag }}>{{ title }}</{{ tag }}>");
            
            var result = new PageResult(context.GetPage("my-partial"))
            {
                Args = { ["title"] = "The title" }
            }.Result;
            Assert.That(result.NormalizeNewLines(), Is.EqualTo(@"
<html>
  <title>The title</title>
</head>
<body>
<h2>with-partial</h2>
<h2>with-partial-binding</h2>
<footer>The title</footer>
</body>
".NormalizeNewLines()));
        }

        [Test]
        public void Can_load_page_with_page_or_partial_with_scoped_variables_containing_bindings()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["myPartial"] = "my-partial",
                    ["headingTag"] = "h2",
                }
            }.Init();

            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
  <title>{{ title }}</title>
</head>
<body>
{{ 'my-partial' |> partial({ title: title, tag: headingTag }) }}
{{ myPartial |> partial({ title: partialTitle, tag: headingTag }) }}
</body>");
            
            context.VirtualFiles.WriteFile("my-partial.html", "<{{ tag }}>{{ title }}</{{ tag }}>");
            
            var result = new PageResult(context.GetPage("my-partial"))
            {
                Args =
                {
                    ["title"] = "The title",
                    ["partialTitle"] = "Partial Title",
                }
            }.Result;
            Assert.That(result.NormalizeNewLines(), Is.EqualTo(@"
<html>
  <title>The title</title>
</head>
<body>
<h2>The title</h2>
<h2>Partial Title</h2>
</body>
".NormalizeNewLines()));
        }

        [Test]
        public void Does_replace_bindings()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["contextTitle"] = "The title",
                    ["contextPartial"] = "bind-partial",
                    ["contextTag"] = "h2",
                    ["a"] = "foo",
                    ["b"] = "bar",
                }
            }.Init();

            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
  <title>{{ title }}</title>
</head>
<body>
{{ contextPartial |> partial({ title: contextTitle, tag: contextTag, items: [a,b] }) }}
{{ page }}
</body>");
            
            context.VirtualFiles.WriteFile("bind-partial.html", @"
<{{ tag }}>{{ title |> upper }}</{{ tag }}>
<p>{{ items |> join(', ') }}</p>");
            
            context.VirtualFiles.WriteFile("bind-page.html", @"
<section>
{{ pagePartial |> partial({ tag: pageTag, items: items }) }}
</section>
");
            
            var result = new PageResult(context.GetPage("bind-page"))
            {
                Args =
                {
                    ["title"] = "Page title",
                    ["pagePartial"] = "bind-partial",
                    ["pageTag"] = "h3",
                    ["items"] = new[] { 1, 2, 3 },
                }
            }.Result;

            Assert.That(result.NormalizeNewLines(), Is.EqualTo(@"
<html>
  <title>Page title</title>
</head>
<body>

<h2>THE TITLE</h2>
<p>foo, bar</p>

<section>

<h3>PAGE TITLE</h3>
<p>1, 2, 3</p>
</section>

</body>
".NormalizeNewLines()));

        }

        [Test]
        public void Can_repeat_templates_using_selectEach()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["letters"] = new[]{ "A", "B", "C" },
                    ["numbers"] = new[]{ 1, 2, 3 },
                }
            }.Init();
            
            Assert.That(new PageResult(context.OneTimePage("<ul> {{ '<li> {{it}} </li>' |> selectEach(letters) }} </ul>")).Result,
                Is.EqualTo("<ul> <li> A </li><li> B </li><li> C </li> </ul>"));

            Assert.That(new PageResult(context.OneTimePage("<ul> {{ '<li> {{it}} </li>' |> selectEach(numbers) }} </ul>")).Result,
                Is.EqualTo("<ul> <li> 1 </li><li> 2 </li><li> 3 </li> </ul>"));
        }

        [Test]
        public void Can_use_escaped_chars_in_selectEach()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["letters"] = new[]{ "A", "B", "C" },
                }
            }.Init();

            var result = context.EvaluateScript("<ul>\n{{ '<li> {{it}} </li>\n' |> selectEach(letters) }}</ul>");
            Assert.That(result.NormalizeNewLines(),
                Is.EqualTo(@"<ul>
<li> A </li>
<li> B </li>
<li> C </li>
</ul>".NormalizeNewLines()));
        }

        [Test]
        public void Can_repeat_templates_using_forEach_in_page_and_layouts()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["numbers"] = new[]{ 1, 2, 3 },
                }
            }.Init();
            
            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
<body>
<header>
<ul> {{ '<li> {{it}} </li>' |> selectEach(numbers) }} </ul>
</header>
<section>
{{ page }}
</section>
</body>
</html>
");
            context.VirtualFiles.WriteFile("page.html", "<ul> {{ '<li> {{it}} </li>' |> selectEach(letters) }} </ul>");
            
            var result = new PageResult(context.GetPage("page"))
            {
                Args =
                {
                    ["letters"] = new[]{ "A", "B", "C" },
                }
            }.Result;
            
            Assert.That(result.NormalizeNewLines(),
                Is.EqualTo(@"
<html>
<body>
<header>
<ul> <li> 1 </li><li> 2 </li><li> 3 </li> </ul>
</header>
<section>
<ul> <li> A </li><li> B </li><li> C </li> </ul>
</section>
</body>
</html>".NormalizeNewLines()));
        }

        [Test]
        public void Can_repeat_templates_with_bindings_using_selectEach()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["items"] = new[]
                    {
                        new ModelBinding { Object = new NestedModelBinding { Prop = "A" }}, 
                        new ModelBinding { Object = new NestedModelBinding { Prop = "B" }}, 
                        new ModelBinding { Object = new NestedModelBinding { Prop = "C" }}, 
                    },
                }
            }.Init();
            
            Assert.That(new PageResult(context.OneTimePage("<ul> {{ '<li> {{ it.Object.Prop }} </li>' |> selectEach(items) }} </ul>")).Result,
                Is.EqualTo("<ul> <li> A </li><li> B </li><li> C </li> </ul>"));
        }

        [Test]
        public void Can_repeat_templates_with_bindings_and_custom_scope_using_selectEach()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["items"] = new[]
                    {
                        new ModelBinding { Object = new NestedModelBinding { Prop = "A" }}, 
                        new ModelBinding { Object = new NestedModelBinding { Prop = "B" }}, 
                        new ModelBinding { Object = new NestedModelBinding { Prop = "C" }}, 
                    },
                }
            }.Init();
            
            Assert.That(new PageResult(context.OneTimePage("<ul> {{ '<li> {{ item.Object.Prop }} </li>' |> selectEach(items, { it: 'item' } ) }} </ul>")).Result,
                Is.EqualTo("<ul> <li> A </li><li> B </li><li> C </li> </ul>"));
            
            // Equivalent with select:
            Assert.That(new PageResult(context.OneTimePage("<ul> {{ items |> select: <li> { it.Object.Prop } </li> }} </ul>")).Result,
                Is.EqualTo("<ul> <li> A </li><li> B </li><li> C </li> </ul>"));
        }

        [Test]
        public void Can_use_forEach_with_markdown()
        {
            using (new BasicAppHost().Init())
            {
                var context = new SharpPagesFeature
                {
                    Args =
                    {
                        ["items"] = new[]{ "foo", "bar", "qux" }
                    }
                }.Init();
             
                Assert.That(new PageResult(context.OneTimePage("{{ ' - {{it}}\n' |> selectEach(items) |> markdown }}")).Result.RemoveAllWhitespace(), 
                    Is.EqualTo("<ul><li>foo</li><li>bar</li><li>qux</li></ul>".RemoveAllWhitespace()));
            }
        }

        [Test]
        public void Can_access_partial_arguments()
        {
            var context = new ScriptContext().Init();
            
            context.VirtualFiles.WriteFile("component.html", @"{{ files |> toList |> select: { it.Key }: { it.Value }\n }}");
            
            context.VirtualFiles.WriteFile("page.html", "{{ 'component' |> partial({ files: { 'a': 'foo', 'b': 'bar' } }) }}");
            
            var output = new PageResult(context.GetPage("page")).Result;
            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
a: foo
b: bar
".NormalizeNewLines()));
        }

    }
}