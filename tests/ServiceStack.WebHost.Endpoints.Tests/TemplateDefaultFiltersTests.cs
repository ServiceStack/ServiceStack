using System;
using System.Globalization;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class TemplateDefaultFiltersTests
    {
        public TemplatePagesContext CreateContext()
        {
            var context = new TemplatePagesContext
            {
                Args =
                {
                    ["foo"] = "bar",
                    ["intVal"] = 1,
                    ["doubleVal"] = 2.2
                }
            };
            return context;
        }

        [Test]
        public async Task Does_default_filter_raw()
        {
            var context = CreateContext();
            context.VirtualFiles.WriteFile("page.html", "<h1>{{ '<script>' }}</h1>");
            context.VirtualFiles.WriteFile("page-raw.html", "<h1>{{ '<script>' | raw }}</h1>");

            var result = await new PageResult(context.GetPage("page")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("<h1>&lt;script&gt;</h1>"));

            result = await new PageResult(context.GetPage("page-raw")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("<h1><script></h1>"));
        }

        [Test]
        public async Task Does_default_filter_json()
        {
            var context = CreateContext();
            context.VirtualFiles.WriteFile("page.html", "var model = {{ model | json }};");

            var result = await new PageResult(context.GetPage("page"))
            {
                Model = new Model
                {
                    Id = 1,
                    Name = "foo",
                }
            }.RenderToStringAsync();

            Assert.That(result, Is.EqualTo("var model = {\"Id\":1,\"Name\":\"foo\"};"));

            result = await new PageResult(context.GetPage("page")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("var model = null;"));

            context.VirtualFiles.WriteFile("page-null.html", "var nil = {{ null | json }};");
            result = await new PageResult(context.GetPage("page-null")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("var nil = null;"));
        }

        [Test]
        public async Task Does_default_filter_appSetting()
        {
            var context = CreateContext().Init();
            context.AppSettings.Set("copyright", "&copy; 2008-2017 ServiceStack");
            context.VirtualFiles.WriteFile("page.html", "<footer>{{ 'copyright' | appSetting | raw }}</footer>");

            var result = await new PageResult(context.GetPage("page")).RenderToStringAsync();

            Assert.That(result, Is.EqualTo("<footer>&copy; 2008-2017 ServiceStack</footer>"));
        }

        [Test]
        public async Task Does_default_filter_arithmetic_using_filter()
        {
            var context = CreateContext().Init();
            context.VirtualFiles.WriteFile("page.html", @"
1 + 1 = {{ 1 | add(1) }}
2 x 2 = {{ 2 | mul(2) }} or {{ 2 | multiply(2) }}
3 - 3 = {{ 3 | sub(3) }} or {{ 3 | subtract(3) }}
4 / 4 = {{ 4 | div(4) }} or {{ 4 | divide(4) }}");

            var result = await new PageResult(context.GetPage("page")).RenderToStringAsync();

            Assert.That(result.SanitizeNewLines(), Is.EqualTo(@"
1 + 1 = 2
2 x 2 = 4 or 4
3 - 3 = 0 or 0
4 / 4 = 1 or 1
".SanitizeNewLines()));
        }

        [Test]
        public async Task Does_default_filter_arithmetic_without_filter()
        {
            var context = CreateContext().Init();
            context.VirtualFiles.WriteFile("page.html", @"
1 + 1 = {{ add(1,1) }}
2 x 2 = {{ mul(2,2) }} or {{ multiply(2,2) }}
3 - 3 = {{ sub(3,3) }} or {{ subtract(3,3) }}
4 / 4 = {{ div(4,4) }} or {{ divide(4,4) }}");

            var html = await new PageResult(context.GetPage("page")).RenderToStringAsync();

            Assert.That(html.SanitizeNewLines(), Is.EqualTo(@"
1 + 1 = 2
2 x 2 = 4 or 4
3 - 3 = 0 or 0
4 / 4 = 1 or 1
".SanitizeNewLines()));
        }

        [Test]
        public async Task Does_default_filter_arithmetic_chained_filters()
        {
            var context = CreateContext().Init();

            context.VirtualFiles.WriteFile("page-chained.html",
                @"(((1 + 2) * 3) / 4) - 5 = {{ 1 | add(2) | multiply(3) | divide(4) | subtract(5) }}");
            var result = await new PageResult(context.GetPage("page-chained")).RenderToStringAsync();
            Assert.That(result.SanitizeNewLines(), Is.EqualTo(@"(((1 + 2) * 3) / 4) - 5 = -2.75".SanitizeNewLines()));

            context.VirtualFiles.WriteFile("page-ordered.html",
                @"1 + 2 * 3 / 4 - 5 = {{ 1 | add( divide(multiply(2,3), 4) ) | subtract(5) }}");
            result = await new PageResult(context.GetPage("page-ordered")).RenderToStringAsync();
            Assert.That(result.SanitizeNewLines(), Is.EqualTo(@"1 + 2 * 3 / 4 - 5 = -2.5".SanitizeNewLines()));
        }

        [Test]
        public async Task Does_default_filter_currency()
        {
            var context = CreateContext().Init();
            context.Args[TemplateConstants.DefaultCulture] = new CultureInfo("en-US");

            context.VirtualFiles.WriteFile("page-default.html", "Cost: {{ 99.99 | currency }}");
            context.VirtualFiles.WriteFile("page-culture.html", "Cost: {{ 99.99 | currency(culture) | raw }}");

            var result = await new PageResult(context.GetPage("page-default")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("Cost: $99.99"));

            result = await new PageResult(context.GetPage("page-culture")) {Args = {["culture"] = "en-AU"}}.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("Cost: $99.99"));

            result = await new PageResult(context.GetPage("page-culture")) {Args = {["culture"] = "en-GB"}}.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("Cost: £99.99"));

            result = await new PageResult(context.GetPage("page-culture")) {Args = {["culture"] = "fr-FR"}}.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("Cost: 99,99 €"));
        }

        [Test]
        public async Task Does_default_filter_format()
        {
            var context = CreateContext().Init();
            context.VirtualFiles.WriteFile("page.html", "{{ 3.14159 | format('{0:N2}') }}");
            
            var result = await new PageResult(context.GetPage("page")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("3.14"));
        }

        [Test]
        public async Task Does_default_filter_dateFormat()
        {
            var context = CreateContext().Init();
            context.VirtualFiles.WriteFile("dateFormat-default.html", "{{ date | dateFormat }}");
            context.VirtualFiles.WriteFile("dateFormat-custom.html", "{{ date | dateFormat(format) }}");
            
            var result = await new PageResult(context.GetPage("dateFormat-default"))
            {
                Args = { ["date"] = new DateTime(2001,01,01,1,1,1,1, DateTimeKind.Utc) }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("2001-01-01"));

            context.Args[TemplateConstants.DefaultDateFormat] = "dd/MM/yyyy";
            result = await new PageResult(context.GetPage("dateFormat-default"))
            {
                Args = { ["date"] = new DateTime(2001,01,01,1,1,1,1, DateTimeKind.Utc) }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("01/01/2001"));

            result = await new PageResult(context.GetPage("dateFormat-custom"))
            {
                Args =
                {
                    ["date"] = new DateTime(2001,01,01,1,1,1,1, DateTimeKind.Utc),
                    ["format"] = "dd.MM.yyyy",
                }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("01.01.2001"));
        }

        [Test]
        public async Task Does_default_filter_dateTimeFormat()
        {
            var context = CreateContext().Init();
            context.VirtualFiles.WriteFile("dateTimeFormat-default.html", "{{ date | dateTimeFormat }}");
            context.VirtualFiles.WriteFile("dateTimeFormat-custom.html", "{{ date | dateFormat(format) }}");
            
            var result = await new PageResult(context.GetPage("dateTimeFormat-default"))
            {
                Args = { ["date"] = new DateTime(2001,01,01,1,1,1,1, DateTimeKind.Utc) }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("2001-01-01 01:01:01Z"));

            context.Args[TemplateConstants.DefaultDateTimeFormat] = "dd/MM/yyyy hh:mm";
            result = await new PageResult(context.GetPage("dateTimeFormat-default"))
            {
                Args = { ["date"] = new DateTime(2001,01,01,1,1,1,1, DateTimeKind.Utc) }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("01/01/2001 01:01"));

            result = await new PageResult(context.GetPage("dateTimeFormat-custom"))
            {
                Args =
                {
                    ["date"] = new DateTime(2001,01,01,1,1,1,1, DateTimeKind.Utc),
                    ["format"] = "dd.MM.yyyy hh.mm.ss",
                }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("01.01.2001 01.01.01"));
        }

        [Test]
        public async Task Does_default_filter_string_filters()
        {
            var context = CreateContext().Init();

            context.VirtualFiles.WriteFile("page-humanize.html", "{{ 'a_varName' | humanize }}");
            var result = await new PageResult(context.GetPage("page-humanize")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("A Var Name"));

            context.VirtualFiles.WriteFile("page-titleCase.html", "{{ 'war and peace' | titleCase }}");
            result = await new PageResult(context.GetPage("page-titleCase")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("War And Peace"));

            context.VirtualFiles.WriteFile("page-lower.html", "{{ 'Title Case' | lower }}");
            result = await new PageResult(context.GetPage("page-lower")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("title case"));

            context.VirtualFiles.WriteFile("page-upper.html", "{{ 'Title Case' | upper }}");
            result = await new PageResult(context.GetPage("page-upper")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("TITLE CASE"));

            context.VirtualFiles.WriteFile("page-pascalCase.html", "{{ 'camelCase' | pascalCase }}");
            result = await new PageResult(context.GetPage("page-pascalCase")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("CamelCase"));

            context.VirtualFiles.WriteFile("page-camelCase.html", "{{ 'PascalCase' | camelCase }}");
            result = await new PageResult(context.GetPage("page-camelCase")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("pascalCase"));

            context.VirtualFiles.WriteFile("page-substring.html", "{{ 'This is a short sentence' | substring(8) }}... {{ 'These three words' | substring(6,5) }}");
            result = await new PageResult(context.GetPage("page-substring")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("a short sentence... three"));

            context.VirtualFiles.WriteFile("page-pad.html", "<h1>{{ '7' | padLeft(3) }}</h1><h2>{{ 'tired' | padRight(10) }}</h2>");
            result = await new PageResult(context.GetPage("page-pad")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("<h1>  7</h1><h2>tired     </h2>"));

            context.VirtualFiles.WriteFile("page-padchar.html", "<h1>{{ '7' | padLeft(3,'0') }}</h1><h2>{{ 'tired' | padRight(10,'z') }}</h2>");
            result = await new PageResult(context.GetPage("page-padchar")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("<h1>007</h1><h2>tiredzzzzz</h2>"));
        }
    }
}