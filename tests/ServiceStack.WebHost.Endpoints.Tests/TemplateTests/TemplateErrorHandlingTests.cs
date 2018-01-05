﻿using System;
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
        public void Exceptions_in_filter_bubble_by_default_async()
        {
            var context = new TemplateContext
            {
            }.Init();

            try
            {
                context.EvaluateTemplate("{{ 'in filter' | throwAsync }}");
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

            Assert.That(context.EvaluateTemplate(@"{{ 'in filter' | throw({ assignError: 'myError' }) }}
<var>{{ myError.Message }}</var><pre>{{ lastErrorStackTrace }}</pre>"),
                Does.StartWith("<var>in filter</var><pre>   at "));
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
        public void Can_throw_on_conditions()
        {
            var context = new TemplateContext
            {
                AssignExceptionsTo = "error"
            }.Init();

            Assert.That(context.EvaluateTemplate(@"{{ 'in filter' | throwIf(true) }}{{ ifError | select: <h1>FAIL! { it.Message }</h1> }}"),
                Is.EqualTo("<h1>FAIL! in filter</h1>"));
            Assert.That(context.EvaluateTemplate(@"{{ 'in filter' | throwIf(true) }}{{ error | ifExists | select: <h1>FAIL! { it.Message }</h1> }}"),
                Is.EqualTo("<h1>FAIL! in filter</h1>"));
            Assert.That(context.EvaluateTemplate(@"{{ true | ifThrow('in filter') }}{{ ifError | select: <h1>FAIL! { it.Message }</h1> }}"),
                Is.EqualTo("<h1>FAIL! in filter</h1>"));
            Assert.That(context.EvaluateTemplate(@"{{ true | ifThrow('in filter') }}{{ error | ifExists | select: <h1>FAIL! { it.Message }</h1> }}"),
                Is.EqualTo("<h1>FAIL! in filter</h1>"));

            Assert.That(context.EvaluateTemplate(@"{{ 'in filter' | throwIf(false) }}{{ ifError | select: <h1>FAIL! { it.Message }</h1> }}"),
                Is.EqualTo(""));
            Assert.That(context.EvaluateTemplate(@"{{ 'in filter' | throwIf(false) }}{{ error | ifExists | select: <h1>FAIL! { it.Message }</h1> }}"),
                Is.EqualTo(""));
            Assert.That(context.EvaluateTemplate(@"{{ false | ifThrow('in filter') }}{{ ifError | select: <h1>FAIL! { it.Message }</h1> }}"),
                Is.EqualTo(""));
            Assert.That(context.EvaluateTemplate(@"{{ false | ifThrow('in filter') }}{{ error | ifExists | select: <h1>FAIL! { it.Message }</h1> }}"),
                Is.EqualTo(""));
        }

        [Test]
        public void Can_throw_on_conditions_with_assignError()
        {
            var context = new TemplateContext
            {
                AssignExceptionsTo = "error"
            }.Init();

            Assert.That(context.EvaluateTemplate(@"{{ 'in filter' | throwIf(true, { assignError: 'ex' }) }}{{ ex | ifExists | select: <h1>FAIL! { it.Message }</h1> }}"),
                Is.EqualTo("<h1>FAIL! in filter</h1>"));
            Assert.That(context.EvaluateTemplate(@"{{ true | ifThrow('in filter', { assignError: 'ex' }) }}{{ ex | ifExists | select: <h1>FAIL! { it.Message }</h1> }}"),
                Is.EqualTo("<h1>FAIL! in filter</h1>"));

            Assert.That(context.EvaluateTemplate(@"{{ 'in filter' | throwIf(false, { assignError: 'ex' }) }}{{ ex | ifExists | select: <h1>FAIL! { it.Message }</h1> }}"),
                Is.EqualTo(""));
            Assert.That(context.EvaluateTemplate(@"{{ false | ifThrow('in filter', { assignError: 'ex' }) }}{{ ex | ifExists | select: <h1>FAIL! { it.Message }</h1> }}"),
                Is.EqualTo(""));
        }

        [Test]
        public void Can_throw_different_exception_types()
        {
            var context = new TemplateContext
            {
                AssignExceptionsTo = "error"
            }.Init();

            Assert.That(context.EvaluateTemplate(@"{{ true | ifThrowArgumentNullException('p') }}{{ ifError | select: <h1>{ it | typeName }! { it.Message }</h1> }}")
                .NormalizeNewLines(),
                Is.EqualTo("<h1>ArgumentNullException! Value cannot be null.\nParameter name: p</h1>"));
            Assert.That(context.EvaluateTemplate(@"{{ true | ifThrowArgumentNullException('p', { assignError: 'ex' }) }}{{ ex | ifExists | select: <h1>{ it | typeName }! { it.Message }</h1> }}")
                .NormalizeNewLines(),
                Is.EqualTo("<h1>ArgumentNullException! Value cannot be null.\nParameter name: p</h1>"));

            Assert.That(context.EvaluateTemplate(@"{{ true | ifThrowArgumentException('bad arg', 'p') }}{{ ifError | select: <h1>{ it | typeName }! { it.Message }</h1> }}")
                .NormalizeNewLines(),
                Is.EqualTo("<h1>ArgumentException! bad arg\nParameter name: p</h1>"));
            Assert.That(context.EvaluateTemplate(@"{{ true | ifThrowArgumentException('bad arg', 'p', { assignError: 'ex' }) }}{{ ex | ifExists | select: <h1>{ it | typeName }! { it.Message }</h1> }}")
                .NormalizeNewLines(),
                Is.EqualTo("<h1>ArgumentException! bad arg\nParameter name: p</h1>"));
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
{{ skipExecutingFiltersOnError }}
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
        public void Does_SkipExecutingPageFiltersIfError_in_Context()
        {
            var context = new TemplateContext
            {
                SkipExecutingFiltersIfError = true,
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
        public void Does_render_htmlErrorDebug_in_DebugMode()
        {
            var context = new TemplateContext
            {
                SkipExecutingFiltersIfError = true,
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
                SkipExecutingFiltersIfError = true,
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

        [Test]
        public void Does_render_htmlErrorDebug_with_StackTraces()
        {
            var context = new TemplateContext
            {
                SkipExecutingFiltersIfError = true,
            }.Init();
            
            context.VirtualFiles.WriteFile("page.html", @"
<h1>Before Error</h1>
{{ 'in filter' | throw }}
{{ htmlErrorDebug }}

<b>{{ 'never executed' }}</b>

<h1>After Error</h1>
");

            var page = context.GetPage("page");
            var output = new PageResult(page).Result;
            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
<h1>Before Error</h1>
<pre class=""alert alert-danger"">Exception: in filter

StackTrace:
   at Expression (String): ""in filter""
   at Page: page.html
</pre>


<b></b>

<h1>After Error</h1>".NormalizeNewLines()));
        }

        [Test]
        public void Can_continue_executing_filters_with_continueExecutingFiltersOnError()
        {
            var context = new TemplateContext
            {
                SkipExecutingFiltersIfError = true,
            }.Init();
            
            context.VirtualFiles.WriteFile("page.html", @"
{{ continueExecutingFiltersOnError }}
<h1>Before Error</h1>
{{ 'in filter' | throw }}
{{ htmlErrorDebug }}

<b>{{ 'is evaluated' }}</b>

<h1>After Error</h1>
");

            var page = context.GetPage("page");
            var output = new PageResult(page).Result;
            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
<h1>Before Error</h1>
<pre class=""alert alert-danger"">Exception: in filter

StackTrace:
   at Expression (String): ""in filter""
   at Page: page.html
</pre>


<b>is evaluated</b>

<h1>After Error</h1>".NormalizeNewLines()));
        }

        [Test]
        public void Calling_ensureAllArgsNotNull_throws_if_any_args_are_null()
        {
            var context = new TemplateContext
            {
                SkipExecutingFiltersIfError = true,
                DebugMode = false,
                Args =
                {
                    ["arg"] = "value",
                    ["empty"] = "",
                }
            }.Init();
            
            context.VirtualFiles.WriteFile("page-arg.html", @"{{ { arg }     | ensureAllArgsNotNull | select: { it.arg } }}{{ htmlError }}");
            context.VirtualFiles.WriteFile("page-empty.html", @"{{ { empty } | ensureAllArgsNotNull | select: { it.empty } }}{{ htmlError }}");
            context.VirtualFiles.WriteFile("page-noarg.html", @"{{ { noArg } | ensureAllArgsNotNull | select: { it.arg } }}{{ htmlError }}");
            context.VirtualFiles.WriteFile("page-msg.html", @"{{ { noArg }   | ensureAllArgsNotNull({ message: '{0} required' }) | select: { it.arg } }}{{ htmlError }}");
            
            Assert.That(new PageResult(context.GetPage("page-arg")).Result, Is.EqualTo(@"value"));
            Assert.That(new PageResult(context.GetPage("page-empty")).Result, Is.EqualTo(@""));
            Assert.That(new PageResult(context.GetPage("page-noarg")).Result.NormalizeNewLines(), 
                Is.EqualTo("<div class=\"alert alert-danger\">Value cannot be null.\nParameter name: noArg</div>"));
            Assert.That(new PageResult(context.GetPage("page-msg")).Result.NormalizeNewLines(), 
                Is.EqualTo("<div class=\"alert alert-danger\">noArg required</div>"));            
        }

        [Test]
        public void Calling_ensureAllArgsNotEmpty_throws_if_any_args_are_empty()
        {
            var context = new TemplateContext
            {
                SkipExecutingFiltersIfError = true,
                DebugMode = false,
                Args =
                {
                    ["arg"] = "value",
                    ["empty"] = "",
                }
            }.Init();
            
            context.VirtualFiles.WriteFile("page-arg.html", @"{{ { arg }     | ensureAllArgsNotEmpty | select: { it.arg } }}{{ htmlError }}");
            context.VirtualFiles.WriteFile("page-empty.html", @"{{ { empty } | ensureAllArgsNotEmpty | select: { it.empty } }}{{ htmlError }}");
            context.VirtualFiles.WriteFile("page-noarg.html", @"{{ { noArg } | ensureAllArgsNotEmpty | select: { it.arg } }}{{ htmlError }}");
            context.VirtualFiles.WriteFile("page-msg.html", @"{{ { noArg }   | ensureAllArgsNotEmpty({ message: '{0} required' }) | select: { it.arg } }}{{ htmlError }}");
            
            Assert.That(new PageResult(context.GetPage("page-arg")).Result, Is.EqualTo(@"value"));
            Assert.That(new PageResult(context.GetPage("page-empty")).Result.NormalizeNewLines(),
                Is.EqualTo("<div class=\"alert alert-danger\">Value cannot be null.\nParameter name: empty</div>"));
            Assert.That(new PageResult(context.GetPage("page-noarg")).Result.NormalizeNewLines(), 
                Is.EqualTo("<div class=\"alert alert-danger\">Value cannot be null.\nParameter name: noArg</div>"));
            Assert.That(new PageResult(context.GetPage("page-msg")).Result.NormalizeNewLines(), 
                Is.EqualTo("<div class=\"alert alert-danger\">noArg required</div>"));            
        }
 
        [Test]
        public void Calling_ensureAnyArgsNotNull_throws_if_all_args_are_null()
        {
            var context = new TemplateContext
            {
                SkipExecutingFiltersIfError = true,
                DebugMode = false,
                Args =
                {
                    ["arg"] = "value",
                    ["empty"] = "",
                }
            }.Init();
            
            context.VirtualFiles.WriteFile("page-arg.html", @"{{ { arg }          | ensureAnyArgsNotNull | select: { it.arg } }}{{ htmlError }}");
            context.VirtualFiles.WriteFile("page-empty.html", @"{{ { arg, noArg } | ensureAnyArgsNotNull | select: { it.arg } }}{{ htmlError }}");
            context.VirtualFiles.WriteFile("page-noarg.html", @"{{ { noArg }      | ensureAnyArgsNotNull | select: { it.arg } }}{{ htmlError }}");
            context.VirtualFiles.WriteFile("page-msg.html", @"{{ { noArg, empty } | ensureAnyArgsNotNull({ message: '{0} required' }) | select: { it.empty } }}{{ htmlError }}");
            
            Assert.That(new PageResult(context.GetPage("page-arg")).Result, Is.EqualTo(@"value"));
            Assert.That(new PageResult(context.GetPage("page-empty")).Result, Is.EqualTo(@"value"));
            Assert.That(new PageResult(context.GetPage("page-noarg")).Result.NormalizeNewLines(), 
                Is.EqualTo("<div class=\"alert alert-danger\">Value cannot be null.\nParameter name: noArg</div>"));
            Assert.That(new PageResult(context.GetPage("page-msg")).Result.NormalizeNewLines(), 
                Is.EqualTo(""));            
        }
 
        [Test]
        public void Calling_ensureAnyArgsNotEmpty_throws_if_all_args_are_empty()
        {
            var context = new TemplateContext
            {
                SkipExecutingFiltersIfError = true,
                DebugMode = false,
                Args =
                {
                    ["arg"] = "value",
                    ["empty"] = "",
                }
            }.Init();
            
            context.VirtualFiles.WriteFile("page-arg.html", @"{{ { arg }          | ensureAnyArgsNotEmpty | select: { it.arg } }}{{ htmlError }}");
            context.VirtualFiles.WriteFile("page-empty.html", @"{{ { arg, noArg } | ensureAnyArgsNotEmpty | select: { it.arg } }}{{ htmlError }}");
            context.VirtualFiles.WriteFile("page-noarg.html", @"{{ { noArg }      | ensureAnyArgsNotEmpty | select: { it.arg } }}{{ htmlError }}");
            context.VirtualFiles.WriteFile("page-msg.html", @"{{ { noArg, empty } | ensureAnyArgsNotEmpty({ message: '{0} required' }) | select: { it.empty } }}{{ htmlError }}");
            
            Assert.That(new PageResult(context.GetPage("page-arg")).Result, Is.EqualTo(@"value"));
            Assert.That(new PageResult(context.GetPage("page-empty")).Result, Is.EqualTo(@"value"));
            Assert.That(new PageResult(context.GetPage("page-noarg")).Result.NormalizeNewLines(), 
                Is.EqualTo("<div class=\"alert alert-danger\">Value cannot be null.\nParameter name: noArg</div>"));
            Assert.That(new PageResult(context.GetPage("page-msg")).Result.NormalizeNewLines(), 
                Is.EqualTo("<div class=\"alert alert-danger\">empty required</div>"));            
        }
    }
}