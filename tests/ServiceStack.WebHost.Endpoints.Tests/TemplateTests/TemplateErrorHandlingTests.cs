using System;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Templates;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateErrorHandlingTests
    {
        [Test]
        public void Exceptions_in_filter_bubble_by_default()
        {
            var context = new TemplateContext().Init();

            try
            {
                context.EvaluateTemplate("{{ 'in filter' | throw }}");
                Assert.Fail("Should throw");
            }
            catch (Exception ex)
            {
                Assert.That(ex.Message, Is.EqualTo("in filter"));
            }
        }

        [Test]
        public void Can_capture_exception_using_AssignException()
        {
            var context = new TemplateContext
            {
                AssignExceptionsTo = "error"
            }.Init();

            Assert.That(context.EvaluateTemplate(@"{{ 'in filter' | throw }}
<var>{{ error.Message }}</var>"),
                Is.EqualTo("<var>in filter</var>"));
        }

        [Test]
        public void Can_capture_exception_using_assignError()
        {
            var context = new TemplateContext().Init();

            Assert.That(context.EvaluateTemplate(@"{{ 'in filter' | throw({ assignError: 'myError' }) }}
<var>{{ myError.Message }}</var>"),
                Is.EqualTo("<var>in filter</var>"));
        }

        [Test]
        public void Can_use_conditional_filters_with_filter_errors()
        {
            var context = new TemplateContext
            {
                AssignExceptionsTo = "error"
            }.Init();

            Assert.That(context.EvaluateTemplate(@"{{ 'in filter' | throw }}{{ ifError | select: <h1>FAIL! { it.Message }</h1> }}"),
                Is.EqualTo("<h1>FAIL! in filter</h1>"));
            Assert.That(context.EvaluateTemplate(@"{{ 'in filter' | throw }}{{ lastError | select: <h1>FAIL! { it.Message }</h1> }}"),
                Is.EqualTo("<h1>FAIL! in filter</h1>"));
            Assert.That(context.EvaluateTemplate(@"{{ 'in filter' | throw }}{{ lastErrorMessage | format('<h1>FAIL! {0}</h1>') | raw }}"),
                Is.EqualTo("<h1>FAIL! in filter</h1>"));
            Assert.That(context.EvaluateTemplate(@"{{ 'in filter' | throw }}<h1>{{ lastError | ifExists | map: it.Message }}</h1>"),
                Is.EqualTo("<h1>in filter</h1>"));
            

            Assert.That(context.EvaluateTemplate(@"{{ ifNoError | select: <h1>SUCCESS!</h1> }}"),
                Is.EqualTo("<h1>SUCCESS!</h1>"));
            Assert.That(context.EvaluateTemplate(@"<h1>{{ ifNoError | show: SUCCESS! }}</h1>"),
                Is.EqualTo("<h1>SUCCESS!</h1>"));
            Assert.That(context.EvaluateTemplate(@"{{ '<h1>SUCCESS!</h1>' | ifNoError | raw }}"),
                Is.EqualTo("<h1>SUCCESS!</h1>"));
            Assert.That(context.EvaluateTemplate(@"{{ lastError | endIfExists | select: <h1>SUCCESS!</h1> }}"),
                Is.EqualTo("<h1>SUCCESS!</h1>"));
            Assert.That(context.EvaluateTemplate(@"<h1>{{ 'SUCCESS!' | ifNotExists(lastError) }}</h1>"),
                Is.EqualTo("<h1>SUCCESS!</h1>"));
        }

        [Test]
        public void Does_skipExecutingPageFiltersIfError()
        {
            var context = new TemplateContext
            {
                AssignExceptionsTo = "error"
            }.Init();

            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
<body>
{{ page }}
</body>
</html>");
            
            context.VirtualFiles.WriteFile("page.html", @"
<h1>Before Error</h1>
{{ 'in filter' | throw }}
{{ ifError | select: <h1>FAIL! { it.Message }</h1> }}
{{ ifErrorSkipExecutingPageFilters }}

<b>{{ 'never executed' }}</b>

<h1>After Error</h1>
");

            var page = context.GetPage("page");
            var output = new PageResult(page).Result;
            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"<html>
<body>

<h1>Before Error</h1>
<h1>FAIL! in filter</h1>


<b></b>

<h1>After Error</h1>

</body>
</html>".NormalizeNewLines()));            
        }

        [Test]
        public void Does_SkipExecutingPageFiltersIfError_in_Context()
        {
            var context = new TemplateContext
            {
                SkipExecutingPageFiltersIfError = true,
            }.Init();

            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
<body>
{{ page }}
</body>
</html>");
            
            context.VirtualFiles.WriteFile("page.html", @"
<h1>Before Error</h1>
{{ 'in filter' | throw }}
{{ ifError | select: <h1>FAIL! { it.Message }</h1> }}

<b>{{ 'never executed' }}</b>

<h1>After Error</h1>
");

            var page = context.GetPage("page");
            var output = new PageResult(page).Result;
            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"<html>
<body>

<h1>Before Error</h1>
<h1>FAIL! in filter</h1>

<b></b>

<h1>After Error</h1>

</body>
</html>".NormalizeNewLines()));
        }

        [Test]
        public void Does_render_htmlErrorMessage_in_DebugMode()
        {
            var context = new TemplateContext
            {
                SkipExecutingPageFiltersIfError = true,
                DebugMode = true,
            }.Init();

            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
<body>
{{ page }}
</body>
</html>");
            
            context.VirtualFiles.WriteFile("page.html", @"
<h1>Before Error</h1>
{{ 'in filter' | throw }}
{{ htmlError }}

<b>{{ 'never executed' }}</b>

<h1>After Error</h1>
");

            var page = context.GetPage("page");
            var output = new PageResult(page).Result;
            
            Assert.That(output.NormalizeNewLines(), Does.StartWith(@"<html>
<body>

<h1>Before Error</h1>
<pre class=""alert alert-danger"">Exception: in filter
".NormalizeNewLines()));
        }
 
        [Test]
        public void Does_render_htmlErrorMessage_when_not_DebugMode()
        {
            var context = new TemplateContext
            {
                SkipExecutingPageFiltersIfError = true,
                DebugMode = false,
            }.Init();

            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
<body>
{{ page }}
</body>
</html>");
            
            context.VirtualFiles.WriteFile("page.html", @"
<h1>Before Error</h1>
{{ 'in filter' | throw }}
{{ htmlError }}

<b>{{ 'never executed' }}</b>

<h1>After Error</h1>
");

            var page = context.GetPage("page");
            var output = new PageResult(page).Result;
            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"<html>
<body>

<h1>Before Error</h1>
<div class=""alert alert-danger"">in filter</div>

<b></b>

<h1>After Error</h1>

</body>
</html>".NormalizeNewLines()));
        }
        
    }
}