using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class DefaultScriptsTests
    {
        public ScriptContext CreateContext(Dictionary<string, object> args = null)
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["foo"] = "bar",
                    ["intVal"] = 1,
                    ["doubleVal"] = 2.2
                }
            }.Init();
            
            args.Each((key,val) => context.Args[key] = val);
            
            return context;
        }

        [Test]
        public async Task Does_default_filter_raw()
        {
            var context = CreateContext();
            context.VirtualFiles.WriteFile("page.html", "<h1>{{ '<script>' }}</h1>");
            context.VirtualFiles.WriteFile("page-raw.html", "<h1>{{ '<script>' |> raw }}</h1>");

            var result = await new PageResult(context.GetPage("page")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("<h1>&lt;script&gt;</h1>"));

            result = await new PageResult(context.GetPage("page-raw")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("<h1><script></h1>"));
        }

        [Test]
        public async Task Does_default_filter_json()
        {
            var context = CreateContext();
            context.VirtualFiles.WriteFile("page.html", "var model = {{ model |> json }};");

            var result = await new PageResult(context.GetPage("page"))
            {
                Model = new Model
                {
                    Id = 1,
                    Name = "foo"
                }
            }.RenderToStringAsync();

            Assert.That(result, Is.EqualTo("var model = {\"Id\":1,\"Name\":\"foo\"};"));

            result = await new PageResult(context.GetPage("page")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("var model = null;"));

            context.VirtualFiles.WriteFile("page-null.html", "var nil = {{ null |> json }};");
            result = await new PageResult(context.GetPage("page-null")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("var nil = null;"));
        }

        [Test]
        public void Can_quote_strings()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript(@"
{{ 'single quotes' }}
{{ ""double quotes"" }}
{{ `backticks` }}
{{ ′prime quoutes′ }}
".NormalizeNewLines()), Is.EqualTo(@"
single quotes
double quotes
backticks
prime quoutes
".NormalizeNewLines()));
        }

        [Test]
        public void Can_escape_strings()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["json"] = "{\"key\":\"single'back`tick\"}",
                    ["prime"] = "{\"key\":\"single'prime′quote\"}",
                    ["hasNewLines"] = "has\nnew\r\nlines"
                }
            }.Init();

            Assert.That(context.EvaluateScript("var s = '{{ json  |> escapeSingleQuotes |> raw }}'"), Is.EqualTo("var s = '{\"key\":\"single\\'back`tick\"}'"));
            Assert.That(context.EvaluateScript("var s = `{{ json  |> escapeBackticks    |> raw }}`"), Is.EqualTo("var s = `{\"key\":\"single'back\\`tick\"}`"));
            Assert.That(context.EvaluateScript("var s = ′{{ prime |> escapePrimeQuotes  |> raw }}′"), Is.EqualTo("var s = ′{\"key\":\"single'prime\\′quote\"}′"));
            Assert.That(context.EvaluateScript("var s = '{{ json  |> jsString }}'"), Is.EqualTo("var s = '{\"key\":\"single\\'back`tick\"}'"));
            Assert.That(context.EvaluateScript("var s = {{ json   |> jsQuotedString }}"), Is.EqualTo("var s = '{\"key\":\"single\\'back`tick\"}'"));

            Assert.That(context.EvaluateScript("var s = '{{ hasNewLines |> jsString }}'"), Is.EqualTo(@"var s = 'has\nnew\r\nlines'"));

            Assert.That(context.EvaluateScript(@"{{ [{x:1,y:2},{x:3,y:4}] |> json |> assignTo:json }}var s = '{{ json |> jsString }}';"), 
                Is.EqualTo("var s = '[{\"x\":1,\"y\":2},{\"x\":3,\"y\":4}]';"));

            Assert.That(context.EvaluateScript(@"{{ `[
  {""name"":""Mc Donald's""}
]` |> raw |> assignTo:json }}
var obj = {{ json }};
var str = '{{ json |> jsString }}';
var str = {{ json |> jsQuotedString }};
var escapeSingle = '{{ ""single' quote's"" |> escapeSingleQuotes |> escapeNewLines |> raw }}';
var escapeDouble = ""{{ 'double"" quote""s' |> escapeDoubleQuotes |> escapeNewLines |> raw }}"";
".NormalizeNewLines()), Is.EqualTo(@"
var obj = [
  {""name"":""Mc Donald's""}
];
var str = '[\n  {""name"":""Mc Donald\'s""}\n]';
var str = '[\n  {""name"":""Mc Donald\'s""}\n]';
var escapeSingle = 'single\' quote\'s';
var escapeDouble = ""double\"" quote\""s"";
".NormalizeNewLines()));

        }

        [Test]
        public async Task Does_default_filter_appSetting()
        {
            var context = CreateContext().Init();
            context.AppSettings.Set("copyright", "&copy; 2008-2017 ServiceStack");
            context.VirtualFiles.WriteFile("page.html", "<footer>{{ 'copyright' |> appSetting |> raw }}</footer>");

            var result = await new PageResult(context.GetPage("page")).RenderToStringAsync();

            Assert.That(result, Is.EqualTo("<footer>&copy; 2008-2017 ServiceStack</footer>"));
        }

        [Test]
        public async Task Does_default_filter_arithmetic_using_filter()
        {
            var context = CreateContext().Init();
            context.VirtualFiles.WriteFile("page.html", @"
1 + 1 = {{ 1 |> add(1) }}
2 x 2 = {{ 2 |> mul(2) }} or {{ 2 |> multiply(2) }}
3 - 3 = {{ 3 |> sub(3) }} or {{ 3 |> subtract(3) }}
4 / 4 = {{ 4 |> div(4) }} or {{ 4 |> divide(4) }}");

            var result = await new PageResult(context.GetPage("page")).RenderToStringAsync();

            Assert.That(result.NormalizeNewLines(), Is.EqualTo(@"
1 + 1 = 2
2 x 2 = 4 or 4
3 - 3 = 0 or 0
4 / 4 = 1 or 1
".NormalizeNewLines()));
        }

        [Test]
        public async Task Does_default_filter_arithmetic_using_pipeline_operator()
        {
            var context = CreateContext().Init();
            context.VirtualFiles.WriteFile("page.html", @"
1 + 1 = {{ 1 |> add(1) }}
2 x 2 = {{ 2 |> mul(2) }} or {{ 2 |> multiply(2) }}
3 - 3 = {{ 3 |> sub(3) }} or {{ 3 |> subtract(3) }}
4 / 4 = {{ 4 |> div(4) }} or {{ 4 |> divide(4) }}");

            var result = await new PageResult(context.GetPage("page")).RenderToStringAsync();

            Assert.That(result.NormalizeNewLines(), Is.EqualTo(@"
1 + 1 = 2
2 x 2 = 4 or 4
3 - 3 = 0 or 0
4 / 4 = 1 or 1
".NormalizeNewLines()));
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

            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
1 + 1 = 2
2 x 2 = 4 or 4
3 - 3 = 0 or 0
4 / 4 = 1 or 1
".NormalizeNewLines()));
        }

        [Test]
        public void Can_use_default_filter_arithmetic_with_shorthand_notation()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["num"] = 1
                }
            }.Init();

            context.VirtualFiles.WriteFile("page.html", @"
{{ num |> add(9) |> assignTo('ten') }}
square = {{ 'square-partial' |> partial({ ten }) }}
");
            
            context.VirtualFiles.WriteFile("square-partial.html", "{{ ten }} x {{ ten }} = {{ ten |> multiply(ten) }}");
            
            Assert.That(new PageResult(context.GetPage("page")).Result.NormalizeNewLines(), Is.EqualTo(@"
square = 10 x 10 = 100".NormalizeNewLines()));
        }
        
        [Test]
        public void Can_increment_and_decrement()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["ten"] = 10
                }
            }.Init();
            
            Assert.That(new PageResult(context.OneTimePage("{{ 1 |> incr }}")).Result, Is.EqualTo("2"));
            Assert.That(new PageResult(context.OneTimePage("{{ ten |> incr }}")).Result, Is.EqualTo("11"));
            Assert.That(new PageResult(context.OneTimePage("{{ 1 |> incrBy(2) }}")).Result, Is.EqualTo("3"));
            Assert.That(new PageResult(context.OneTimePage("{{ ten |> incrBy(2) }}")).Result, Is.EqualTo("12"));
            Assert.That(new PageResult(context.OneTimePage("{{ incr(1) }}")).Result, Is.EqualTo("2"));
            Assert.That(new PageResult(context.OneTimePage("{{ incr(ten) }}")).Result, Is.EqualTo("11"));
            Assert.That(new PageResult(context.OneTimePage("{{ incrBy(ten,2) }}")).Result, Is.EqualTo("12"));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 1 |> decr }}")).Result, Is.EqualTo("0"));
            Assert.That(new PageResult(context.OneTimePage("{{ ten |> decrBy(2) }}")).Result, Is.EqualTo("8"));
        }
        
        [Test]
        public void Can_increment_and_decrement_pipeline_operator()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["ten"] = 10
                }
            }.Init();
            
            Assert.That(new PageResult(context.OneTimePage("{{ 1 |> incr }}")).Result, Is.EqualTo("2"));
            Assert.That(new PageResult(context.OneTimePage("{{ ten |> incr }}")).Result, Is.EqualTo("11"));
            Assert.That(new PageResult(context.OneTimePage("{{ 1 |> incrBy(2) }}")).Result, Is.EqualTo("3"));
            Assert.That(new PageResult(context.OneTimePage("{{ ten |> incrBy(2) }}")).Result, Is.EqualTo("12"));
            Assert.That(new PageResult(context.OneTimePage("{{ 1 |> decr }}")).Result, Is.EqualTo("0"));
            Assert.That(new PageResult(context.OneTimePage("{{ ten |> decrBy(2) }}")).Result, Is.EqualTo("8"));
        }

        [Test]
        public void Can_compare_numbers()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["two"] = 2
                }
            }.Init();
            
            Assert.That(new PageResult(context.OneTimePage("{{ 2 |> greaterThan(1) }}")).Result, Is.EqualTo("True"));
            Assert.That(new PageResult(context.OneTimePage("{{ two |> greaterThan(1) }}")).Result, Is.EqualTo("True"));
            Assert.That(new PageResult(context.OneTimePage("{{ greaterThan(two,1) }}")).Result, Is.EqualTo("True"));
            Assert.That(new PageResult(context.OneTimePage("{{ greaterThan(2,2) }}")).Result, Is.EqualTo("False"));
            Assert.That(new PageResult(context.OneTimePage("{{ greaterThan(two,2) }}")).Result, Is.EqualTo("False"));
            Assert.That(new PageResult(context.OneTimePage("{{ greaterThan(two,two) }}")).Result, Is.EqualTo("False"));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'two > 1'    |> if(gt(two,1)) |> raw }}")).Result, Is.EqualTo("two > 1"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'two > 2'    |> if(greaterThan(two,2)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'two > 3'    |> if(greaterThan(two,3)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'two > two'  |> if(greaterThan(two,two)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'two >= two' |> if(greaterThanEqual(two,two)) |> raw }}")).Result, Is.EqualTo("two >= two"));

            Assert.That(new PageResult(context.OneTimePage("{{ '1 >= 2' |> if(greaterThanEqual(1,2)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 >= 2' |> if(greaterThanEqual(2,2)) |> raw }}")).Result, Is.EqualTo("2 >= 2"));
            Assert.That(new PageResult(context.OneTimePage("{{ '3 >= 2' |> if(greaterThanEqual(3,2)) |> raw }}")).Result, Is.EqualTo("3 >= 2"));

            Assert.That(new PageResult(context.OneTimePage("{{ '1 > 2'  |> if(greaterThan(1,2)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 > 2'  |> if(greaterThan(2,2)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ '3 > 2'  |> if(greaterThan(3,2)) |> raw }}")).Result, Is.EqualTo("3 > 2"));

            Assert.That(new PageResult(context.OneTimePage("{{ '1 <= 2' |> if(lessThanEqual(1,2)) |> raw }}")).Result, Is.EqualTo("1 <= 2"));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 <= 2' |> if(lessThanEqual(2,2)) |> raw }}")).Result, Is.EqualTo("2 <= 2"));
            Assert.That(new PageResult(context.OneTimePage("{{ '3 <= 2' |> if(lessThanEqual(3,2)) }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ '1 < 2'  |> if(lessThan(1,2)) |> raw }}")).Result, Is.EqualTo("1 < 2"));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 < 2'  |> if(lessThan(2,2)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ '3 < 2'  |> if(lessThan(3,2)) }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ '2 >  2' |> if(gt(2,2)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 >= 2' |> if(gte(2,2)) |> raw }}")).Result, Is.EqualTo("2 >= 2"));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 <= 2' |> if(lte(2,2)) |> raw }}")).Result, Is.EqualTo("2 <= 2"));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 <  2' |> if(lt(2,2)) }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ '2 == 2' |> if(equals(2,2)) }}")).Result, Is.EqualTo("2 == 2"));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 == 2' |> if(eq(2,2)) }}")).Result, Is.EqualTo("2 == 2"));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 != 2' |> if(notEquals(2,2)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ '2 != 2' |> if(not(2,2)) }}")).Result, Is.EqualTo(""));
        }

        [Test]
        public void Can_compare_strings()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["foo"] = "foo",
                    ["bar"] = "bar"
                }
            }.Init();
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'foo >  \"foo\"' |> if(gt(foo,\"foo\")) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'foo >= \"foo\"' |> if(gte(foo,\"foo\")) |> raw }}")).Result, Is.EqualTo("foo >= \"foo\""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'foo <= \"foo\"' |> if(lte(foo,\"foo\")) |> raw }}")).Result, Is.EqualTo("foo <= \"foo\""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'foo <  \"foo\"' |> if(lt(foo,\"foo\")) }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'bar >  \"foo\"' |> if(gt(bar,\"foo\")) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'bar >= \"foo\"' |> if(gte(bar,\"foo\")) |> raw }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'bar <= \"foo\"' |> if(lte(bar,\"foo\")) |> raw }}")).Result, Is.EqualTo("bar <= \"foo\""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'bar <  \"foo\"' |> if(lt(bar,\"foo\")) |> raw }}")).Result, Is.EqualTo("bar <  \"foo\""));
        }

        [Test]
        public void Can_compare_DateTime()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["year2000"] = new DateTime(2000,1,1),
                    ["year2100"] = new DateTime(2100,1,1)
                }
            }.Init();
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'now >  year2000' |> if(gt(now,year2000)) |> raw }}")).Result, Is.EqualTo("now >  year2000"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'now >= year2000' |> if(gte(now,year2000)) |> raw }}")).Result, Is.EqualTo("now >= year2000"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'now <= year2000' |> if(lte(now,year2000)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'now <  year2000' |> if(lt(now,year2000)) }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'now >  year2100' |> if(gt(now,year2100)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'now >= year2100' |> if(gte(now,year2100)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'now <= year2100' |> if(lte(now,year2100)) |> raw }}")).Result, Is.EqualTo("now <= year2100"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'now <  year2100' |> if(lt(now,year2100)) |> raw }}")).Result, Is.EqualTo("now <  year2100"));
            
            Assert.That(new PageResult(context.OneTimePage("{{ '\"2001-01-01\" >  year2100' |> if(gt(\"2001-01-01\",year2100)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ '\"2001-01-01\" >= year2100' |> if(gte(\"2001-01-01\",year2100)) }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ '\"2001-01-01\" <= year2100' |> if(lte(\"2001-01-01\",year2100)) |> raw }}")).Result, Is.EqualTo("\"2001-01-01\" <= year2100"));
            Assert.That(new PageResult(context.OneTimePage("{{ '\"2001-01-01\" <  year2100' |> if(lt(\"2001-01-01\",year2100)) |> raw }}")).Result, Is.EqualTo("\"2001-01-01\" <  year2100"));
        }

        [Test]
        public void Can_use_logical_boolean_operators()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["foo"] = "foo",
                    ["bar"] = "bar",
                    ["year2000"] = new DateTime(2000,1,1),
                    ["year2100"] = new DateTime(2100,1,1),
                    ["contextTrue"] = true,
                    ["contextFalse"] = false
                }
            }.Init();
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'OR(true,true)' |> if(OR(true,true)) |> raw }}")).Result, Is.EqualTo("OR(true,true)"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'OR(true,false)' |> if(OR(true,false)) |> raw }}")).Result, Is.EqualTo("OR(true,false)"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'OR(false,false)' |> if(OR(false,false)) |> raw }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'AND(true,true)' |> if(AND(true,true)) |> raw }}")).Result, Is.EqualTo("AND(true,true)"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'AND(true,false)' |> if(AND(true,false)) |> raw }}")).Result, Is.EqualTo(""));
            Assert.That(new PageResult(context.OneTimePage("{{ 'AND(false,false)' |> if(AND(false,false)) |> raw }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'OR(contextTrue,contextTrue)' |> if(OR(contextTrue,contextTrue)) |> raw }}")).Result, Is.EqualTo("OR(contextTrue,contextTrue)"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'OR(contextTrue,contextFalse)' |> if(OR(contextTrue,contextFalse)) |> raw }}")).Result, Is.EqualTo("OR(contextTrue,contextFalse)"));
            Assert.That(new PageResult(context.OneTimePage("{{ 'OR(contextFalse,contextFalse)' |> if(OR(contextFalse,contextFalse)) |> raw }}")).Result, Is.EqualTo(""));
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'OR(gt(now,year2000),eq(\"foo\",bar))' |> if(OR(gt(now,year2000),eq(\"foo\",bar))) |> raw }}")).Result, 
                Is.EqualTo("OR(gt(now,year2000),eq(\"foo\",bar))"));

            Assert.That(new PageResult(context.OneTimePage(@"{{ 'OR(gt(now,year2000),eq(""foo"",bar))' |> 
            if (
                OR (
                    gt ( now, year2000 ),
                    eq ( ""foo"",  bar )
                )
            ) |> raw }}")).Result, 
                Is.EqualTo("OR(gt(now,year2000),eq(\"foo\",bar))"));

            
            Assert.That(new PageResult(context.OneTimePage(@"{{ 'OR(AND(gt(now,year2000),eq(""foo"",bar)),AND(gt(now,year2000),eq(""foo"",foo)))' |> 
            if ( 
                OR (
                    AND (
                        gt ( now, year2000 ),
                        eq ( ""foo"", bar  )
                    ),
                    AND (
                        gt ( now, year2000 ),
                        eq ( ""foo"", foo  )
                    )
                ) 
            ) |> raw }}")).Result, 
                Is.EqualTo(@"OR(AND(gt(now,year2000),eq(""foo"",bar)),AND(gt(now,year2000),eq(""foo"",foo)))"));
        }

        [Test]
        public async Task Does_default_filter_arithmetic_chained_filters()
        {
            var context = CreateContext().Init();

            Assert.That((((1 + 2) * 3) / 4) - 5, Is.EqualTo(- 3));
            Assert.That((((1 + 2) * 3) / 4d) - 5, Is.EqualTo(-2.75));
            Assert.That(1 + 2 * 3 / 4 - 5, Is.EqualTo(-3));
            Assert.That(1 + 2 * 3 / 4d - 5, Is.EqualTo(-2.5));
            
            context.VirtualFiles.WriteFile("page-chained.html",
                @"(((1 + 2) * 3) / 4) - 5 = {{ 1 |> add(2) |> multiply(3) |> divide(4) |> subtract(5) }}");
            var result = await new PageResult(context.GetPage("page-chained")).RenderToStringAsync();
            Assert.That(result.NormalizeNewLines(), Is.EqualTo(@"(((1 + 2) * 3) / 4) - 5 = -2.75".NormalizeNewLines()));

            context.VirtualFiles.WriteFile("page-ordered.html",
                @"1 + 2 * 3 / 4 - 5 = {{ 1 |> add( divide(multiply(2,3), 4) ) |> subtract(5) }}");
            result = await new PageResult(context.GetPage("page-ordered")).RenderToStringAsync();
            Assert.That(result.NormalizeNewLines(), Is.EqualTo(@"1 + 2 * 3 / 4 - 5 = -2.5".NormalizeNewLines()));
        }

        [Test]
        public async Task Does_default_filter_currency()
        {
            var context = CreateContext().Init();
            context.Args[nameof(ScriptConfig.DefaultCulture)] = new CultureInfo("en-US");

            context.VirtualFiles.WriteFile("page-default.html", "Cost: {{ 99.99 |> currency }}");
            context.VirtualFiles.WriteFile("page-culture.html", "Cost: {{ 99.99 |> currency(culture) |> raw }}");

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
            context.VirtualFiles.WriteFile("page.html", "{{ 3.14159 |> format('N2') }}");
            
            var result = await new PageResult(context.GetPage("page")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("3.14"));
        }

        [Test]
        public async Task Does_default_filter_dateFormat()
        {
            var context = CreateContext().Init();
            context.VirtualFiles.WriteFile("dateFormat-default.html", "{{ date |> dateFormat }}");
            context.VirtualFiles.WriteFile("dateFormat-custom.html", "{{ date |> dateFormat(format) }}");
            
            var result = await new PageResult(context.GetPage("dateFormat-default"))
            {
                Args = { ["date"] = new DateTime(2001,01,01,1,1,1,1, DateTimeKind.Utc) }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("2001-01-01"));

            context.Args[ScriptConstants.DefaultDateFormat] = "dd/MM/yyyy";
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
                    ["format"] = "dd.MM.yyyy"
                }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("01.01.2001"));
        }

        [Test]
        public void Does_default_time_format()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["time"] = new TimeSpan(1,2,3,4,5),
                    ["date"] = new DateTime(2001,2,3,4,5,6,7)
                }
            }.Init();

            var result = context.EvaluateScript("Time: {{ time |> timeFormat }}");
            Assert.That(result, Is.EqualTo("Time: 2:03:04"));
            
            result = context.EvaluateScript("Time: {{ time |> timeFormat('g') }}");
            Assert.That(result, Is.EqualTo("Time: 1:2:03:04.005"));
            
            result = context.EvaluateScript("Time: {{ date.TimeOfDay |> timeFormat('g') }}");
            Assert.That(result, Is.EqualTo("Time: 4:05:06.007"));

            // Normal quoted strings pass string verbatim
            result = context.EvaluateScript(@"Time: {{ date.TimeOfDay |> timeFormat(′h\:mm\:ss′) }}");
            Assert.That(result, Is.EqualTo("Time: 4:05:06"));

            // Template literals unescapes strings
            result = context.EvaluateScript(@"Time: {{ date.TimeOfDay |> timeFormat(`h\\:mm\\:ss`) }}");
            Assert.That(result, Is.EqualTo("Time: 4:05:06"));
        }
        
        [Test]
        public void Does_unescape_string()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{′′}}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{``}}"), Is.EqualTo(""));

            // `backticks` unescape strings, all other quoted strings use verbatim strings
            Assert.That(context.EvaluateScript(@"{{′\ ′[0] |> toCharCode}}"), Is.EqualTo("92")); //= [\]
            Assert.That(context.EvaluateScript(@"{{`\ `[0] |> toCharCode}}"), Is.EqualTo("32")); //= [ ]
            Assert.That(context.EvaluateScript(@"{{`\\` |> toCharCode}}"), Is.EqualTo("92")); //= [/]

            Assert.That(context.EvaluateScript("{{′\n′}}"), Is.EqualTo("\n"));
            Assert.That(context.EvaluateScript("{{`a\nb`}}"), Is.EqualTo("a\nb"));
            Assert.That(context.EvaluateScript("{{′\"′|raw}}"), Is.EqualTo("\""));
            Assert.That(context.EvaluateScript("{{`\"`|raw}}"), Is.EqualTo("\""));
        }

        [Test]
        public async Task Does_default_filter_dateTimeFormat()
        {
            var context = CreateContext().Init();
            context.VirtualFiles.WriteFile("dateTimeFormat-default.html", "{{ date |> dateTimeFormat }}");
            context.VirtualFiles.WriteFile("dateTimeFormat-custom.html", "{{ date |> dateFormat(format) }}");
            
            var result = await new PageResult(context.GetPage("dateTimeFormat-default"))
            {
                Args = { ["date"] = new DateTime(2001,01,01,1,1,1,1, DateTimeKind.Utc) }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("2001-01-01 01:01:01Z"));

            context.Args[ScriptConstants.DefaultDateTimeFormat] = "dd/MM/yyyy hh:mm";
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
                    ["format"] = "dd.MM.yyyy hh.mm.ss"
                }
            }.RenderToStringAsync();
            Assert.That(result, Is.EqualTo("01.01.2001 01.01.01"));
        }

        [Test]
        public void Does_default_spaces_and_indents()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{ indent }}"), Is.EqualTo("\t"));
            Assert.That(context.EvaluateScript("{{ 4 |> indents }}"), Is.EqualTo("\t\t\t\t"));
            
            Assert.That(context.EvaluateScript("{{ space }}"), Is.EqualTo(" "));
            Assert.That(context.EvaluateScript("{{ 4 |> spaces }}"), Is.EqualTo("    "));

            Assert.That(context.EvaluateScript("{{ 4 |> repeating('  ') }}"), Is.EqualTo("        "));
            Assert.That(context.EvaluateScript("{{ '  ' |> repeat(4) }}"),    Is.EqualTo("        "));
            Assert.That(context.EvaluateScript("{{ '.' |> repeat(3) }}"), Is.EqualTo("..."));

            var newLine = Environment.NewLine;
            Assert.That(context.EvaluateScript("{{ newLine }}"), Is.EqualTo(newLine));
            Assert.That(context.EvaluateScript("{{ 4 |> newLines }}"), Is.EqualTo(newLine + newLine + newLine + newLine));
            
            context = new ScriptContext
            {
                Args =
                {
                    [ScriptConstants.DefaultIndent] = "  ",
                    [ScriptConstants.DefaultNewLine] = "\n"
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{ indent }}"), Is.EqualTo("  "));
            Assert.That(context.EvaluateScript("{{ 4 |> newLines }}"), Is.EqualTo("\n\n\n\n"));
        }

        [Test]
        public async Task Does_default_filter_string_filters()
        {
            var context = CreateContext().Init();

            context.VirtualFiles.WriteFile("page-humanize.html", "{{ 'a_varName' |> humanize }}");
            var result = await new PageResult(context.GetPage("page-humanize")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("A Var Name"));

            context.VirtualFiles.WriteFile("page-titleCase.html", "{{ 'war and peace' |> titleCase }}");
            result = await new PageResult(context.GetPage("page-titleCase")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("War And Peace"));

            context.VirtualFiles.WriteFile("page-lower.html", "{{ 'Title Case' |> lower }}");
            result = await new PageResult(context.GetPage("page-lower")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("title case"));

            context.VirtualFiles.WriteFile("page-upper.html", "{{ 'Title Case' |> upper }}");
            result = await new PageResult(context.GetPage("page-upper")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("TITLE CASE"));

            context.VirtualFiles.WriteFile("page-pascalCase.html", "{{ 'camelCase' |> pascalCase }}");
            result = await new PageResult(context.GetPage("page-pascalCase")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("CamelCase"));

            context.VirtualFiles.WriteFile("page-camelCase.html", "{{ 'PascalCase' |> camelCase }}");
            result = await new PageResult(context.GetPage("page-camelCase")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("pascalCase"));

            context.VirtualFiles.WriteFile("page-substring.html", "{{ 'This is a short sentence' |> substring(8) }}... {{ 'These three words' |> substring(6,5) }}");
            result = await new PageResult(context.GetPage("page-substring")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("a short sentence... three"));

            context.VirtualFiles.WriteFile("page-pad.html", "<h1>{{ '7' |> padLeft(3) }}</h1><h2>{{ 'tired' |> padRight(10) }}</h2>");
            result = await new PageResult(context.GetPage("page-pad")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("<h1>  7</h1><h2>tired     </h2>"));

            context.VirtualFiles.WriteFile("page-padchar.html", "<h1>{{ '7' |> padLeft(3,'0') }}</h1><h2>{{ 'tired' |> padRight(10,'z') }}</h2>");
            result = await new PageResult(context.GetPage("page-padchar")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("<h1>007</h1><h2>tiredzzzzz</h2>"));

            context.VirtualFiles.WriteFile("page-repeat.html", "<h1>long time ago{{ ' ...' |> repeat(3) }}</h1>");
            result = await new PageResult(context.GetPage("page-repeat")).RenderToStringAsync();
            Assert.That(result, Is.EqualTo("<h1>long time ago ... ... ...</h1>"));
        }

        [Test]
        public void Does_default_filter_with_no_args()
        {
            var context = CreateContext().Init();

            Assert.That(new PageResult(context.OneTimePage("{{ now |> dateFormat('yyyy-MM-dd') }}")).Result, Is.EqualTo(DateTime.Now.ToString("yyyy-MM-dd")));
            Assert.That(new PageResult(context.OneTimePage("{{ utcNow |> dateFormat('yyyy-MM-dd') }}")).Result, Is.EqualTo(DateTime.UtcNow.ToString("yyyy-MM-dd")));
        }

        [Test]
        public void Can_build_urls_using_filters()
        {
            var context = CreateContext(new Dictionary<string, object>{ {"baseUrl", "http://example.org" }}).Init();

            Assert.That(new PageResult(context.OneTimePage("{{ baseUrl |> addPaths(['customers',1,'orders']) |> raw }}")).Result, 
                Is.EqualTo("http://example.org/customers/1/orders"));

            Assert.That(new PageResult(context.OneTimePage("{{ baseUrl |> addQueryString({ id: 1, foo: 'bar' }) |> raw }}")).Result, 
                Is.EqualTo("http://example.org?id=1&foo=bar"));

            Assert.That(new PageResult(context.OneTimePage("{{ baseUrl |> addQueryString({ id: 1, foo: 'bar' }) |> addHashParams({ hash: 'value' }) |> raw }}")).Result, 
                Is.EqualTo("http://example.org?id=1&foo=bar#hash=value"));
        }

        [Test]
        public void Can_build_urls_using_empty_strings()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{ '' |> addQueryString({ redirect:null }) }}"),
                Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ '/' |> addQueryString({ redirect:null }) }}"),
                Is.EqualTo("/"));
        }

        [Test]
        public void Can_assign_result_to_variable()
        {
            string result;
            var context = new ScriptContext
            {
                Args =
                {
                    ["num"] = 1,
                    ["items"] = new[]{ "foo", "bar", "qux" }
                },
                FilterTransformers =
                {
                    ["markdown"] = MarkdownPageFormat.TransformToHtml
                }
            }.Init();

            result = new PageResult(context.OneTimePage(@"
{{ num |> incr |> assignTo('result') }}
result={{ result }}
")).Result;
            Assert.That(result.NormalizeNewLines(), Is.EqualTo("result=2"));
            
            result = new PageResult(context.OneTimePage(@"
{{ '<li> {{it}} </li>' |> selectEach(items) |> assignTo('result') }}
<ul>{{ result |> raw }}</ul>
")).Result;            
            Assert.That(result.NormalizeNewLines(), Is.EqualTo("<ul><li> foo </li><li> bar </li><li> qux </li></ul>"));
            
            result = new PageResult(context.OneTimePage(@"
{{ ' - {{it}}' |> appendLine |> selectEach(items) |> markdown |> assignTo('result') }}
<div>{{ result |> raw }}</div>
")).Result;            
            Assert.That(result.NormalizeNewLines(), Is.EqualTo("<div><ul>\n<li>foo</li>\n<li>bar</li>\n<li>qux</li>\n</ul>\n</div>"));

            ConsoleLogFactory.Configure();
            result = new PageResult(context.OneTimePage(@"
{{ ' - {{it}}' |> appendLine |> selectEach(items) |> markdown |> assignTo('result') }}
<div>{{ result |> raw }}</div>
")).Result;            
            Assert.That(result.NormalizeNewLines(), Is.EqualTo("<div><ul>\n<li>foo</li>\n<li>bar</li>\n<li>qux</li>\n</ul>\n</div>"));
        }

        [Test]
        public void Can_assign_to_array_index()
        {
            var context = new ScriptContext().Init();

            Assert.That(context.EvaluateScript(@"
{{ [1,2,3,4,5] |> assignTo: numbers }}
{{ numbers |> do: assign('numbers[index]', multiply(numbers[index], numbers[index])) }}
{{ numbers |> join }}").Trim(), Is.EqualTo("1,4,9,16,25"));

            Assert.That(context.EvaluateScript(@"
{{ [1,2,3,4,5] |> assignTo: numbers }}
{{ numbers |> do: assign('numbers[index]', numbers[index] * numbers[index]) }}
{{ numbers |> join }}").Trim(), Is.EqualTo("1,4,9,16,25"));
        }

        [Test]
        public void Can_assign_to_array_index_with_arrow_function()
        {
            var context = new ScriptContext().Init();

            Assert.That(context.EvaluateScript(@"
{{ [1,2,3,4,5] |> assignTo => numbers }}
{{ numbers |> do => assign('numbers[index]', numbers[index] * numbers[index]) }}
{{ numbers |> join }}").Trim(), Is.EqualTo("1,4,9,16,25"));

            Assert.That(context.EvaluateScript(@"
{{ [1,2,3,4,5] |> assignTo => numbers }}
{{ numbers |> do => assign(`num${index}`, it * it) }}
{{ num0 }},{{ num1 }},{{ num2 }},{{ num3 }},{{ num4 }}").Trim(), Is.EqualTo("1,4,9,16,25"));
        }

        [Test]
        public void Can_assign_to_variables_in_partials()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["num"] = 1
                }
            }.Init();

            context.VirtualFiles.WriteFile("_layout.html", @"
<html>
<body>
<header>
layout num = {{ num }}
pageMetaTitle = {{ pageMetaTitle }}
inlinePageTitle = {{ inlinePageTitle }}
pageResultTitle = {{ pageResultTitle }}
</header>
{{ 'add-partial' |> partial({ num: 100 }) }} 
{{ page }}
{{ 'add-partial' |> partial({ num: 400 }) }} 
<footer>
layout num = {{ num }}
inlinePageTitle = {{ inlinePageTitle }}
</footer>
</body>
</html>
");
            
            context.VirtualFiles.WriteFile("page.html", @"
<!--
pageMetaTitle: page meta title
-->
<section>
{{ 'page inline title' |> upper |> assignTo('inlinePageTitle') }}
{{ 'add-partial' |> partial({ num: 200 }) }} 
{{ num |> add(1) |> assignTo('num') }}
<h2>page num = {{ num }}</h2>
{{ 'add-partial' |> partial({ num: 300 }) }} 
</section>");
            
            context.VirtualFiles.WriteFile("add-partial.html", @"
{{ num |> add(10) |> assignTo('num') }}
<h3>partial num = {{ num }}</h3>");
            
            var result = new PageResult(context.GetPage("page"))
            {
                Args =
                {
                    ["pageResultTitle"] = "page result title"
                }
            }.Result;
            
            /* NOTES: 
              1. Page Args and Page Result Args are *always* visible to Layout as they're known before page is executed
              2. Args created during Page execution are *only* visible in Layout after page is rendered (i.e. executed)
              3. Args assigned in partials are retained within their scope
            */
            
            Assert.That(result.RemoveNewLines(), Is.EqualTo(@"
<html>
<body>
<header>
layout num = 1
pageMetaTitle = page meta title
inlinePageTitle = 
pageResultTitle = page result title
</header>
<h3>partial num = 110</h3> 
<section>
<h3>partial num = 210</h3> 
<h2>page num = 2</h2>
<h3>partial num = 310</h3> 
</section>
<h3>partial num = 410</h3> 
<footer>
layout num = 2
inlinePageTitle = PAGE INLINE TITLE
</footer>
</body>
</html>
".RemoveNewLines()));
        }

        [Test]
        public void Does_not_select_template_with_null_target()
        {
            var context = new ScriptContext().Init();

            var result = context.EvaluateScript("{{ null |> select: was called }}");
            
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Can_parseKeyValueText()
        {
            var context = new ScriptContext
            {
                ScriptMethods = { new ProtectedScripts() }
            }.Init();
            
            context.VirtualFiles.WriteFile("expenses.txt", @"
Rent      1000
TV        50
Internet  50
Mobile    50
Food      400
");

            var output = context.EvaluateScript(@"
{{ 'expenses.txt' |> includeFile |> assignTo: expensesText }}
{{ expensesText |> parseKeyValueText |> assignTo: expenses }}
Expenses:
{{ expenses |> toList |> select: { it.Key |> padRight(10) }{ it.Value }\n }}
{{ '-' |> repeat(15) }}
Total    {{ expenses |> values |> sum }}
");
            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
Expenses:
Rent      1000
TV        50
Internet  50
Mobile    50
Food      400

---------------
Total    1550
".NormalizeNewLines()));

        }

        public class ModelValues
        {
            public int Id { get; set; }
            public TimeSpan TimeSpan { get; set; }
            public DateTime DateTime { get; set; }
        }

        [Test]
        public void Can_order_by_different_data_types()
        {
            var items = new[]
            {
                new ModelValues { Id = 1, DateTime = new DateTime(2001,01,01), TimeSpan = TimeSpan.FromSeconds(1) }, 
                new ModelValues { Id = 2, DateTime = new DateTime(2001,01,02), TimeSpan = TimeSpan.FromSeconds(2) }
            };

            var context = new ScriptContext
            {
                Args =
                {
                    ["items"] = items
                }
            }.Init();

            Assert.That(context.EvaluateScript(@"{{ items 
                |> orderByDescending: it.DateTime 
                |> first |> property: Id }}"), Is.EqualTo("2"));
            
            Assert.That(context.EvaluateScript(@"{{ items 
                |> orderByDescending: it.TimeSpan 
                |> first |> property: Id }}"), Is.EqualTo("2"));
        }

        [Test]
        public void Can_use_not_operator_in_boolean_expression()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript(@"
{{ var items = ['A','B','C'] }}
{{ !items.contains('A') |> iif('Y','N') }}").Trim(), Is.EqualTo("N"));

            Assert.That(context.EvaluateScript(@"
{{ var items = ['A','B','C'] }}
{{ 'Y' |> ifElse(!items.contains('D'), 'N') }}").Trim(), Is.EqualTo("Y"));

            Assert.That(context.EvaluateScript(@"
{{ var items = ['A','B','C'] }}
{{ 'Y' |> ifElse(not(items.contains('D')),'N') }}").Trim(), Is.EqualTo("Y"));

            Assert.That(context.EvaluateScript(@"
{{ var items = ['A','B','C'] }}
{{ ['B','C','D'] |> where => !items.contains(it) |> first }}").Trim(), Is.EqualTo("D"));
        }

        [Test]
        [Ignore("Needs review - MONOREPO")]
        public void Does_fmt()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{ 'in {0} middle' |> fmt('the') }}"), 
                Is.EqualTo("in the middle"));
            Assert.That(context.EvaluateScript("{{ 'in {0} middle of the {1}' |> fmt('the', 'night') }}"), 
                Is.EqualTo("in the middle of the night"));
            Assert.That(context.EvaluateScript("{{ 'in {0} middle of the {1} I go {2}' |> fmt('the', 'night', 'walking') }}"), 
                Is.EqualTo("in the middle of the night I go walking"));
            Assert.That(context.EvaluateScript("{{ 'in {0} middle of the {1} I go {2} in my {3}' |> fmt(['the', 'night', 'walking', 'sleep']) }}"), 
                Is.EqualTo("in the middle of the night I go walking in my sleep"));
            
            Assert.That(context.EvaluateScript("{{ 'I owe {0:c}' |> fmt(123.45) }}"), 
                Is.EqualTo("I owe $123.45"));
        }

        [Test]
        [Ignore("Needs review - MONOREPO")]
        public void Does_appendFmt()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{ 'in ' |> appendFmt('{0} middle','the') }}"), 
                Is.EqualTo("in the middle"));
            Assert.That(context.EvaluateScript("{{ 'in ' |> appendFmt('{0} middle of the {1}', 'the', 'night') }}"), 
                Is.EqualTo("in the middle of the night"));
            Assert.That(context.EvaluateScript("{{ 'in ' |> appendFmt('{0} middle of the {1} I go {2}', 'the', 'night', 'walking') }}"), 
                Is.EqualTo("in the middle of the night I go walking"));
            Assert.That(context.EvaluateScript("{{ 'in ' |> appendFmt('{0} middle of the {1} I go {2} in my {3}', ['the', 'night', 'walking', 'sleep']) }}"), 
                Is.EqualTo("in the middle of the night I go walking in my sleep"));
            
            Assert.That(context.EvaluateScript("{{ 'I ' |> appendFmt('owe {0:c}', 123.45) }}"), 
                Is.EqualTo("I owe $123.45"));
        }

        [Test]
        public void Can_use_exist_tests_on_non_existing_arguments()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["arg"] = "value",
                    ["list"] = new[]{ 1, 2, 3 },
                    ["emptyList"] = new int[0],
                    ["map"] = new Dictionary<string, object> { {"a", 1}, {"b", 2} },
                    ["emptyMap"] = new Dictionary<string, object>()
                }
            }.Init();
            
            context.VirtualFiles.WriteFile("h1.html", "<h1>{{ it }}</h1>");
            
            
            Assert.That(context.EvaluateScript("{{ arg |> ifExists }}"), Is.EqualTo("value"));
            Assert.That(context.EvaluateScript("{{ noArg |> ifExists }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateScript("{{ 1 |> ifExists(arg) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 1 |> ifExists(noArg) }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateScript("{{ 1 |> ifNotExists(arg) }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ 1 |> ifNotExists(noArg) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 1 |> ifNo(arg) }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ 1 |> ifNo(noArg) }}"), Is.EqualTo("1"));

            Assert.That(context.EvaluateScript("{{ arg |> selectPartial: h1 }}"), Is.EqualTo("<h1>value</h1>"));
            Assert.That(context.EvaluateScript("{{ noArg |> selectPartial: h1 }}"), Is.EqualTo(""));
            
            Assert.That(context.EvaluateScript("{{ list |> ifNotEmpty |> join }}"), Is.EqualTo("1,2,3"));
            Assert.That(context.EvaluateScript("{{ noList |> ifNotEmpty }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateScript("{{ 1 |> ifNotEmpty(list) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 1 |> ifNotEmpty(emptyList) }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ 1 |> ifNotEmpty(noList) }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateScript("{{ 1 |> ifEmpty(list) }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ 1 |> ifEmpty(emptyList) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 1 |> ifEmpty(noList) }}"), Is.EqualTo("1"));
        }

        [Test]
        public void Does_not_emit_binding_on_empty_Key_Value()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["row"] = new List<KeyValuePair<string,object>>
                    {
                        new KeyValuePair<string, object>("arg", "value"),
                        new KeyValuePair<string, object>("enmptyArg", ""),
                        new KeyValuePair<string, object>("nullArg", null)
                    } 
                }
            }.Init();

            var output = context.EvaluateScript("{{ row |> select: <i>{ it.Key }</i><b>{ it.Value }</b> }}");
            Assert.That(output, Is.EqualTo("<i>arg</i><b>value</b><i>enmptyArg</i><b></b><i>nullArg</i><b></b>"));
        }

        [Test]
        public void Does_resolve_partials_and_files_using_cascading_resolution()
        {
            var context = new ScriptContext
            {
                ScriptMethods = { new ProtectedScripts() }
            }.Init();

            context.VirtualFiles.WriteFile("root-partial.html", @"root-partial.html");
            context.VirtualFiles.WriteFile("root-file.txt", @"root-file.txt");
            context.VirtualFiles.WriteFile("partial.html", @"partial.html");
            context.VirtualFiles.WriteFile("file.txt", @"file.txt");

            context.VirtualFiles.WriteFile("dir/partial.html", @"dir/partial.html");
            context.VirtualFiles.WriteFile("dir/file.txt", @"dir/file.txt");

            context.VirtualFiles.WriteFile("dir/dir-partial.html", @"dir/dir-partial.html");
            context.VirtualFiles.WriteFile("dir/dir-file.txt", @"dir/dir-file.txt");

            context.VirtualFiles.WriteFile("dir/sub/partial.html", @"dir/sub/partial.html");
            context.VirtualFiles.WriteFile("dir/sub/file.txt", @"dir/sub/file.txt");
            
            context.VirtualFiles.WriteFile("page.html", @"partial: {{ 'partial' |> partial }}
file: {{ 'file.txt' |> includeFile }}
root-partial: {{ 'root-partial' |> partial }}
root-file: {{ 'root-file.txt' |> includeFile }}");

            context.VirtualFiles.WriteFile("dir/page.html", @"partial: {{ 'partial' |> partial }}
file: {{ 'file.txt' |> includeFile }}
root-partial: {{ 'root-partial' |> partial }}
root-file: {{ 'root-file.txt' |> includeFile }}");

            context.VirtualFiles.WriteFile("dir/sub/page.html", @"partial: {{ 'partial' |> partial }}
file: {{ 'file.txt' |> includeFile }}
root-partial: {{ 'root-partial' |> partial }}
root-file: {{ 'root-file.txt' |> includeFile }}
dir-partial: {{ 'dir-partial' |> partial }}
dir-file: {{ 'dir-file.txt' |> includeFile }}");
            
            Assert.That(new PageResult(context.GetPage("page")).Result.NormalizeNewLines(),
                Is.EqualTo(@"
partial: partial.html
file: file.txt
root-partial: root-partial.html
root-file: root-file.txt".NormalizeNewLines()));
            
            Assert.That(new PageResult(context.GetPage("dir/page")).Result.NormalizeNewLines(),
                Is.EqualTo(@"
partial: dir/partial.html
file: dir/file.txt
root-partial: root-partial.html
root-file: root-file.txt".NormalizeNewLines()));
            
            Assert.That(new PageResult(context.GetPage("dir/sub/page")).Result.NormalizeNewLines(),
                Is.EqualTo(@"
partial: dir/sub/partial.html
file: dir/sub/file.txt
root-partial: root-partial.html
root-file: root-file.txt
dir-partial: dir/dir-partial.html
dir-file: dir/dir-file.txt
".NormalizeNewLines()));
        }

        [Test]
        public void Can_use_end_to_discard_return_value()
        {
            var context = new ScriptContext().Init();
            
            context.VirtualFiles.WriteFile("partial.html", "partial");

            Assert.That(context.EvaluateScript("{{ 1 |> end }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ add(1,1) |> end }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ 'partial' |> partial |> end }}"), Is.EqualTo(""));
        }

        [Test]
        public void Can_use_split_with_different_delimiters()
        {
            var context = new ScriptContext().Init();

            Assert.That(context.EvaluateScript("{{ 'a,b,c' |> split |> join('|') }}"), Is.EqualTo("a|b|c"));
            Assert.That(context.EvaluateScript("{{ 'a:b:c' |> split(':') |> join('|') }}"), Is.EqualTo("a|b|c"));
            Assert.That(context.EvaluateScript("{{ 'a::b::c' |> split('::') |> join('|') }}"), Is.EqualTo("a|b|c"));
            Assert.That(context.EvaluateScript("{{ 'a:b/c' |> split([':','/']) |> join('|') }}"), Is.EqualTo("a|b|c"));
            Assert.That(context.EvaluateScript("{{ 'a::b//c' |> split(['::','//']) |> join('|') }}"), Is.EqualTo("a|b|c"));
        }

        [Test]
        public void Can_use_length_filters()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["items"] = new[]{1,2,3}
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{ items |> length }}"), Is.EqualTo("3"));
            Assert.That(context.EvaluateScript("{{ items |> hasMinCount(0) |> iif(1,0) }}:{{ items |> hasMinCount(3) |> iif(1,0) }}:{{ items |> hasMinCount(4) |> iif(1,0) }}"), Is.EqualTo("1:1:0"));
            Assert.That(context.EvaluateScript("{{ items |> hasMaxCount(0) |> iif(1,0) }}:{{ items |> hasMaxCount(3) |> iif(1,0) }}:{{ items |> hasMaxCount(4) |> iif(1,0) }}"), Is.EqualTo("0:1:1"));

            Assert.That(context.EvaluateScript("{{ null |> hasMinCount(0) |> iif(1,0) }}:{{ 1 |> hasMinCount(1) |> iif(1,0) }}:{{ 'a' |> hasMinCount(0) |> iif(1,0) }}"), Is.EqualTo("0:0:1"));
            Assert.That(context.EvaluateScript("{{ null |> length }}:{{ 1 |> length }}:{{ 'a' |> length }}"), Is.EqualTo("0:0:1"));

            Assert.That(context.EvaluateScript("{{ [1,2] |> hasMinCount(0) |> iif(1,0) }}:{{ 1 |> hasMinCount(1) |> iif(1,0) }}:{{ 'a' |> hasMinCount(0) |> iif(1,0) }}"), Is.EqualTo("1:0:1"));
            Assert.That(context.EvaluateScript("{{ noArg |> hasMinCount(0) |> iif(1,0) }}:{{ items |> hasMinCount(1) |> ifUse(length(items)) }}"), Is.EqualTo("0:3"));
        }

        [Test]
        public void Can_use_test_isTest_filters()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["string"] = "foo",
                    ["int"] = 1,
                    ["long"] = (long)1,
                    ["byte"] = (byte)1,
                    ["double"] = 1.1,
                    ["float"] = (float)1.1,
                    ["decimal"] = (decimal)1.1,
                    ["bool"] = true,
                    ["char"] = 'c',
                    ["chars"] = new[]{ 'a','b','c' },
                    ["bytes"] = new byte[]{ 1, 2, 3 },
                    ["intDictionary"] = new Dictionary<int, int>(),
                    ["stringDictionary"] = new Dictionary<string, string>(),
                    ["objectDictionary"] = new Dictionary<string, object>(),
                    ["objectList"] = new List<object>(),
                    ["objectArray"] = new object[]{ 1, "a" },
                    ["anonObject"] = new { id = 1 },
                    ["context"] = new ScriptContext(),
                    ["tuple"] = Tuple.Create(1, "a"),
                    ["keyValuePair"] = new KeyValuePair<int,string>(1,"a")
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{ 'a' |> isString |> iif(1,0) }}:{{ 1 |> isString |> iif(1,0) }}"), Is.EqualTo("1:0"));
            Assert.That(context.EvaluateScript("{{ 'a' |> isInt |> iif(1,0) }}:{{ 1 |> isInt |> iif(1,0) }}"), Is.EqualTo("0:1"));
            Assert.That(context.EvaluateScript("{{ 'a' |> isLong |> iif(1,0) }}:{{ 1 |> toLong |> isLong |> iif(1,0) }}"), Is.EqualTo("0:1"));
            Assert.That(context.EvaluateScript("{{ 'a' |> isDouble |> iif(1,0) }}:{{ 1.1 |> isDouble |> iif(1,0) }}"), Is.EqualTo("0:1"));
            Assert.That(context.EvaluateScript("{{ 'a' |> isFloat |> iif(1,0) }}:{{ 1.1 |> toFloat |> isFloat |> iif(1,0) }}"), Is.EqualTo("0:1"));
            Assert.That(context.EvaluateScript("{{ 'a' |> isDecimal |> iif(1,0) }}:{{ 1.1 |> toDecimal |> isDecimal |> iif(1,0) }}"), Is.EqualTo("0:1"));
            Assert.That(context.EvaluateScript("{{ 'a' |> isBool |> iif(1,0) }}:{{ false |> isBool |> iif(1,0) }}"), Is.EqualTo("0:1"));
            Assert.That(context.EvaluateScript("{{ 'a' |> isChar |> iif(1,0) }}:{{ 'a' |> toChar |> isChar |> iif(1,0) }}"), Is.EqualTo("0:1"));
            Assert.That(context.EvaluateScript("{{ 'a' |> isChars |> iif(1,0) }}:{{ 'a' |> toChars |> isChars |> iif(1,0) }}:{{ ['a','b'] |> toChars |> isChars |> iif(1,0) }}"), Is.EqualTo("0:1:1"));
            Assert.That(context.EvaluateScript("{{ 'a' |> isByte |> iif(1,0) }}:{{ 1 |> toByte |> isByte |> iif(1,0) }}"), Is.EqualTo("0:1"));
            Assert.That(context.EvaluateScript("{{ bytes |> isBytes |> iif(1,0) }}:{{ 'a' |> isBytes |> iif(1,0) }}:{{ 'a' |> toUtf8Bytes |> isBytes |> iif(1,0) }}"), Is.EqualTo("1:0:1"));
            Assert.That(context.EvaluateScript("{{ 'a' |> isList |> iif(1,0) }}:{{ {a:1} |> isList |> iif(1,0) }}:{{ ['a'] |> isList |> iif(1,0) }}"), Is.EqualTo("0:0:1"));
            Assert.That(context.EvaluateScript("{{ 'a' |> isEnumerable |> iif(1,0) }}:{{ 1 |> isEnumerable |> iif(1,0) }}:{{ ['a'] |> isEnumerable |> iif(1,0) }}:{{ {a:1} |> isEnumerable |> iif(1,0) }}"), Is.EqualTo("1:0:1:1"));
            Assert.That(context.EvaluateScript("{{ 'a' |> isDictionary |> iif(1,0) }}:{{ {a:1} |> isDictionary |> iif(1,0) }}:{{ ['a'] |> isDictionary |> iif(1,0) }}"), Is.EqualTo("0:1:0"));
            Assert.That(context.EvaluateScript("{{ {a:'a'} |> isStringDictionary |> iif(1,0) }}:{{ {a:1} |> isStringDictionary |> iif(1,0) }}:{{ stringDictionary |> isStringDictionary |> iif(1,0) }}"), Is.EqualTo("0:0:1"));
            Assert.That(context.EvaluateScript("{{ {a:'a'} |> isObjectDictionary |> iif(1,0) }}:{{ {a:1} |> isObjectDictionary |> iif(1,0) }}:{{ stringDictionary |> isObjectDictionary |> iif(1,0) }}"), Is.EqualTo("1:1:0"));
            Assert.That(context.EvaluateScript("{{ 'a' |> isNumber |> iif(1,0) }}:{{ 1 |> isNumber |> iif(1,0) }}:{{ 1.1 |> isNumber |> iif(1,0) }}"), Is.EqualTo("0:1:1"));
            Assert.That(context.EvaluateScript("{{ 'a' |> isRealNumber |> iif(1,0) }}:{{ 1 |> isRealNumber |> iif(1,0) }}:{{ 1.1 |> isRealNumber |> iif(1,0) }}"), Is.EqualTo("0:0:1"));
            Assert.That(context.EvaluateScript("{{ objectList |> isArray |> iif(1,0) }}:{{ objectArray |> isArray |> iif(1,0) }}:{{ [1,'a'] |> isArray |> iif(1,0) }}"), Is.EqualTo("0:1:0"));
            Assert.That(context.EvaluateScript("{{ anonObject |> isAnonObject |> iif(1,0) }}:{{ context |> isAnonObject |> iif(1,0) }}:{{ {a:1} |> isAnonObject |> iif(1,0) }}"), Is.EqualTo("1:0:0"));
            Assert.That(context.EvaluateScript("{{ context |> isClass |> iif(1,0) }}:{{ 1 |> isClass |> iif(1,0) }}"), Is.EqualTo("1:0"));
            Assert.That(context.EvaluateScript("{{ context |> isValueType |> iif(1,0) }}:{{ 1 |> isValueType |> iif(1,0) }}"), Is.EqualTo("0:1"));
            Assert.That(context.EvaluateScript("{{ {a:1} |> isKeyValuePair |> iif(1,0) }}:{{ keyValuePair |> isKeyValuePair |> iif(1,0) }}:{{ {a:1} |> toList |> get(0) |> isKeyValuePair |> iif(1,0) }}"), Is.EqualTo("0:1:1"));

            Assert.That(context.EvaluateScript("{{ 'a' |> isType('string') |> iif(1,0) }}:{{ string |> isType('String') |> iif(1,0) }}:{{ 1 |> isString |> iif(1,0) }}"), Is.EqualTo("1:1:0"));
        }

        [Test]
        public void Can_use_eval()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["arg"] = "value"
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{ '1' |> eval |> typeName }}"), Is.EqualTo("Int32"));
            Assert.That(context.EvaluateScript("{{ 'arg' |> eval }}"), Is.EqualTo("value"));
            Assert.That(context.EvaluateScript("{{ `'arg'` |> eval }}"), Is.EqualTo("arg"));
            Assert.That(context.EvaluateScript("{{ '{a:1}' |> eval |> get: a }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ '{a:arg}' |> eval |> get: a }}"), Is.EqualTo("value"));
            Assert.That(context.EvaluateScript("{{ '[1]' |> eval |> get(0) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ '[{a:arg}]' |> eval |> get(0) |> get:a }}"), Is.EqualTo("value"));

            Assert.That(context.EvaluateScript("{{ 'incr(1)' |> eval }}"), Is.EqualTo("2"));
            Assert.That(context.EvaluateScript("{{ '{a:incr(1)}' |> eval |> get: a }}"), Is.EqualTo("2"));
        }

        [Test]
        public void Can_parse_JSON()
        {
            var context = new ScriptContext().Init();

            Assert.That(context.EvaluateScript("{{ '1' |> parseJson |> typeName }}"), Is.EqualTo("Int32"));
            Assert.That(context.EvaluateScript("{{ 'arg' |> parseJson }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ `'arg'` |> parseJson }}"), Is.EqualTo("arg"));
            Assert.That(context.EvaluateScript("{{ '{a:1}' |> parseJson |> get: a }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ '{a:arg}' |> parseJson |> get: a }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ '[1]' |> parseJson |> get(0) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ '[{a:arg}]' |> parseJson |> get(0) |> get:a }}"), Is.EqualTo(""));
        }

        [Test]
        public void Can_stop_filter_execution_with_end()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["arg"] = "foo",
                    ["items"] = new[]{1,2,3}
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{ 1 |> end }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ 1 |> endIfNull }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ null  |> endIfNull     |> default('unreachable') }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ arg   |> endIfNull     |> useFmt('{0} + {1} = {2}',1,2,3) }}"), Is.EqualTo("1 + 2 = 3"));
            Assert.That(context.EvaluateScript("{{ arg   |> endIfNotNull  |> use('bar') |> assignTo: arg }}{{ arg }}"), Is.EqualTo("foo"));
            Assert.That(context.EvaluateScript("{{ noArg |> endIfExists   |> use('bar') |> assignTo: noArg }}{{ noArg }}"), Is.EqualTo("bar"));
            Assert.That(context.EvaluateScript("{{ []    |> endIfEmpty    |> default('unreachable') }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ items |> endIfNotEmpty |> use([4,5,6]) |> assignTo: items }}{{ items |> join }}"), Is.EqualTo("1,2,3"));
            Assert.That(context.EvaluateScript("{{ nums  |> endIfNotEmpty |> use([4,5,6]) |> assignTo: nums  }}{{ nums  |> join }}"), Is.EqualTo("4,5,6"));
            Assert.That(context.EvaluateScript("{{ 1 |> endIfFalsy |> default('unreachable') }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 0 |> endIfFalsy |> default('unreachable') }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ arg |> endIfTruthy |> use('bar') }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ one |> endIfTruthy |> use(1) |> assignTo: one }}{{ one }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 1 |> endIf(true) }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ 1 |> endIf(false) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 5 |> times |> endIfAny: it == 4\n |> join }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ 5 |> times |> endIfAny: it == 5\n |> join }}"), Is.EqualTo("0,1,2,3,4"));
            Assert.That(context.EvaluateScript("{{ 5 |> times |> endIfAll: lt(it,4)\n |> join }}"), Is.EqualTo("0,1,2,3,4"));
            Assert.That(context.EvaluateScript("{{ 5 |> times |> endIfAll: lt(it,5)\n |> join }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateScript("{{ 1   |> endWhere: isString(it) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 'a' |> endWhere: isString(it) }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateScript("{{ endIf(true)  |> use(1) }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ endIf(false) |> use(1) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ true  |> ifEnd |> use(1) }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ false |> ifEnd |> use(1) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ true  |> ifNotEnd |> use(1) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ false |> ifNotEnd |> use(1) }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateScript("{{ doIf(true)  |> use(1) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ doIf(false) |> use(1) }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ true  |> ifDo |> use(1) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ false |> ifDo |> use(1) }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ true  |> ifDo |> select: 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ false |> ifDo |> select: 1 }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateScript("{{ true  |> ifUse(1) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ false |> ifUse(1) }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ 1 |> useIf(true)  }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 1 |> useIf(false) }}"), Is.EqualTo(""));
        }

        [Test]
        public void Can_chain_end_filters_together()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["arg"] = "foo",
                    ["empty"] = "",
                    ["nil"] = null,
                    ["items"] = new[]{1,2,3},
                    ["none"] = new int[]{}
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{ arg   |> endIfNull     |> endIfNotNull(noArg) |> select: 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ noArg |> endIfExists   |> endIfExists(noArg2) |> select: 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ empty |> endIfNull     |> endIfNotNull(nil)   |> select: 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ empty |> endIfNull     |> endIfNull(nil) |> select: 1 }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateScript("{{ items |> endIfEmpty    |> endIfNotEmpty(none) |> select: 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ items |> endIfNotEmpty |> select: 1 }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ none  |> endIfEmpty    |> select: 1 }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateScript("{{ endIf(isEmpty(items)) |> endIf(!isEmpty(none)) |> select: 1 }}"), Is.EqualTo("1"));

            Assert.That(context.EvaluateScript("{{ noArg |> endIfExists |> endIfNull(none)   |> use(1) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ arg   |> endIfEmpty  |> endIfEmpty(items) |> join }}"), Is.EqualTo("1,2,3"));
        }

        [Test]
        public void Can_continue_filter_execution_with_only()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["arg"] = "foo",
                    ["items"] = new[]{1,2,3}
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{ 1 |> onlyIfNotNull }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ null  |> onlyIfNotNull  |> default('unreachable') }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ arg   |> onlyIfNotNull  |> useFmt('{0} + {1} = {2}',1,2,3) }}"), Is.EqualTo("1 + 2 = 3"));
            Assert.That(context.EvaluateScript("{{ arg   |> onlyIfNull     |> use('bar') |> assignTo: arg }}{{ arg }}"), Is.EqualTo("foo"));
            Assert.That(context.EvaluateScript("{{ noArg |> onlyIfNull     |> use('bar') |> assignTo: noArg }}{{ noArg }}"), Is.EqualTo("bar"));
            Assert.That(context.EvaluateScript("{{ []    |> onlyIfNotEmpty |> default('unreachable') }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ items |> onlyIfEmpty    |> use([4,5,6]) |> assignTo: items }}{{ items |> join }}"), Is.EqualTo("1,2,3"));
            Assert.That(context.EvaluateScript("{{ nums  |> onlyIfEmpty    |> use([4,5,6]) |> assignTo: nums  }}{{ nums  |> join }}"), Is.EqualTo("4,5,6"));
            Assert.That(context.EvaluateScript("{{ 1     |> onlyIfTruthy   |> default('unreachable') }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 0     |> onlyIfTruthy   |> default('unreachable') }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ arg   |> onlyIfFalsy    |> show: 1 }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ one   |> onlyIfFalsy    |> use(1) |> assignTo: one }}{{ one }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 1 |> onlyIf(false) }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ 1 |> onlyIf(true) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 5 |> times |> onlyIfAll: lt(it,4)\n |> join }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ 5 |> times |> onlyIfAll: lt(it,5)\n |> join }}"), Is.EqualTo("0,1,2,3,4"));
            Assert.That(context.EvaluateScript("{{ 5 |> times |> onlyIfAny: it == 4\n |> join }}"), Is.EqualTo("0,1,2,3,4"));
            Assert.That(context.EvaluateScript("{{ 5 |> times |> onlyIfAny: it == 5\n |> join }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateScript("{{ 1   |> onlyWhere: !isString(it) }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 'a' |> onlyWhere: !isString(it) }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateScript("{{ onlyIf(false)     |> show: 1 }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ onlyIf(true)      |> show: 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ true  |> ifOnly    |> show: 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ false |> ifOnly    |> show: 1 }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ true  |> ifNotOnly |> show: 1 }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ false |> ifNotOnly |> show: 1 }}"), Is.EqualTo("1"));

            Assert.That(context.EvaluateScript("{{ doIf(true)   |> show: 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ doIf(false)  |> show: 1 }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ true  |> ifDo |> show: 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ false |> ifDo |> show: 1 }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateScript("{{ true  |> ifShow: 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ false |> ifShow: 1 }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ 1 |> showIf(true)  }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 1 |> showIf(false) }}"), Is.EqualTo(""));
        }

        [Test]
        public void Can_chain_only_filters_together()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["arg"] = "foo",
                    ["empty"] = "",
                    ["nil"] = null,
                    ["items"] = new[]{1,2,3},
                    ["none"] = new int[]{}
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{ arg   |> onlyIfNotNull   |> onlyIfNull(noArg)   |> show: 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ noArg |> onlyIfNull      |> onlyIfNull(noArg2)  |> show: 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ empty |> onlyIfNotNull   |> onlyIfNull(nil)     |> show: 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ empty |> onlyIfNotNull   |> onlyIfNotNull(nil)  |> show: 1 }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateScript("{{ items |> onlyIfNotEmpty  |> onlyIfEmpty(none) |> show: 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ items |> onlyIfEmpty     |> show: 1 }}"), Is.EqualTo(""));
            Assert.That(context.EvaluateScript("{{ none  |> onlyIfNotEmpty  |> show: 1 }}"), Is.EqualTo(""));

            Assert.That(context.EvaluateScript("{{ onlyIf(!isEmpty(items)) |> onlyIf(isEmpty(none)) |> show: 1 }}"), Is.EqualTo("1"));

            Assert.That(context.EvaluateScript("{{ noArg |> onlyIfNull      |> onlyIfNotNull(none)   |> show: 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ arg   |> onlyIfNotEmpty  |> onlyIfNotEmpty(items) |> join }}"), Is.EqualTo("1,2,3"));
        }

        [Test]
        public void Can_flatten()
        {
            var context = new ScriptContext {
                Args = {
                    ["nestedInts"] = new [] { new[]{1,2,3},new[]{4,5,6} },
                    ["nestedInts2"] = new [] { new[]{new[]{1,2},new[]{3}},new[]{new[]{4},new[]{5,6}} },
                    ["nestedStrings"] = new [] { new[]{"A","B","C"},new[]{"D","E","F"} },
                    ["nestedStrings2"] = new [] { new[]{new[]{"A","B"},new[]{"C"}},new[]{new[]{"D"},new[]{"E","F"}} },
                }
            }.Init();

            Assert.That(context.Evaluate<List<object>>("{{ nestedInts |> flat |> return }}"), 
                Is.EquivalentTo(new[]{ 1,2,3,4,5,6 }));
            Assert.That(context.Evaluate<List<object>>("{{ nestedInts2 |> flatten |> return }}"), 
                Is.EquivalentTo(new[]{ 1,2,3,4,5,6 }));
            
            Assert.That(context.Evaluate<List<object>>("{{ nestedStrings |> flat |> return }}"), 
                Is.EquivalentTo(new[]{ "A","B","C","D","E","F" }));
            Assert.That(context.Evaluate<List<object>>("{{ nestedStrings2 |> flatten |> return }}"), 
                Is.EquivalentTo(new[]{ "A","B","C","D","E","F" }));

            Assert.That(context.Evaluate<List<object>>("{{ [ [1,2,[3], [4,[5,6]] ] ] |> flatten |> return }}"), 
                Is.EquivalentTo(new[]{ 1,2,3,4,5,6 }));
        }


        [Test]
        public void Does_show()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{ 1 }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 1 |> show: 2 }}"), Is.EqualTo("2"));
            Assert.That(context.EvaluateScript("{{ 1 |> use('({0})') |> fmt: 2 }}"), Is.EqualTo("(2)"));
            Assert.That(context.EvaluateScript("{{ 1 |> showFmt('({0})',2) }}"), Is.EqualTo("(2)"));
            Assert.That(context.EvaluateScript("{{ 1 |> use(2) |> format('0.00') }}"), Is.EqualTo("2.00"));
            Assert.That(context.EvaluateScript("{{ 1 |> showFormat(2,'0.00') }}"), Is.EqualTo("2.00"));

            Assert.That(context.EvaluateScript("{{ 1 |> show:    <h1>title</h1> }}"), Is.EqualTo("&lt;h1&gt;title&lt;/h1&gt;"));
            Assert.That(context.EvaluateScript("{{ 1 |> showRaw: <h1>title</h1> }}"), Is.EqualTo("<h1>title</h1>"));
            Assert.That(context.EvaluateScript("{{ 1 |> showFmtRaw('<h1>{0}</h1>',2) }}"), Is.EqualTo("<h1>2</h1>"));

            Assert.That(context.EvaluateScript("{{ 2 |> formatRaw: <h1>{0}</h1> }}"), Is.EqualTo("<h1>2</h1>"));
        }

        [Test]
        public void Does_conditional_error_handling()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["invalid"] = true
                }
            }.Init();
            
            Assert.That(context.EvaluateScript(
                    @"{{ invalid |> ifDo |> select: <div class=""alert alert-danger"">Argument is invalid.</div> }}"),
                Is.EqualTo(@"<div class=""alert alert-danger"">Argument is invalid.</div>"));
            
        }

        [Test]
        public void Does_match_pathInfo()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["PathInfo"] = "/dir/sub"
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{ '/dir/sub' |> matchesPathInfo }}"), Is.EqualTo("True"));
            Assert.That(context.EvaluateScript("{{ 1 |> ifMatchesPathInfo('/dir/sub/') }}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{ 1 |> ifMatchesPathInfo('/dir/su') |> otherwise: 0 }}"), Is.EqualTo("0"));
        }

        [Test]
        public void Can_addTo_existing_collection()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript(@"
{{ 1  |> addTo: nums }}
{{ 2  |> addTo: nums }}
{{ 3  |> addTo: nums }}
{{ nums |> join }}
".NormalizeNewLines()), Is.EqualTo("1,2,3"));
            
            Assert.That(context.EvaluateScript(@"
{{ [1]  |> addTo: nums }}
{{ [2]  |> addTo: nums }}
{{ [3]  |> addTo: nums }}
{{ nums |> join }}
".NormalizeNewLines()), Is.EqualTo("1,2,3"));
        }

        [Test]
        public void Can_addToGlobal_existing_collection()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript(@"
{{ 1  |> addToGlobal: nums }}
{{ 2  |> addToGlobal: nums }}
{{ 3  |> addToGlobal: nums }}
{{ nums |> join }}
".NormalizeNewLines()), Is.EqualTo("1,2,3"));
            
            Assert.That(context.EvaluateScript(@"
{{ [1]  |> addToGlobal: nums }}
{{ [2]  |> addToGlobal: nums }}
{{ [3]  |> addToGlobal: nums }}
{{ nums |> join }}
".NormalizeNewLines()), Is.EqualTo("1,2,3"));
        }

        [Test]
        public void Can_addToStart_of_an_existing_collection()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript(@"
{{ 1  |> addToStart: nums }}
{{ 2  |> addToStart: nums }}
{{ 3  |> addToStart: nums }}
{{ nums |> join }}
".NormalizeNewLines()), Is.EqualTo("3,2,1"));
        }

        [Test]
        public void Can_addToStartGlobal_of_an_existing_collection()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript(@"
{{ 1  |> addToStartGlobal: nums }}
{{ 2  |> addToStartGlobal: nums }}
{{ 3  |> addToStartGlobal: nums }}
{{ nums |> join }}
".NormalizeNewLines()), Is.EqualTo("3,2,1"));
        }

        [Test]
        public void Can_appendTo_existing_string()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript(@"
{{ 'a' |> appendTo: string }}
{{ 'b' |> appendTo: string }}
{{ 'c' |> appendTo: string }}
{{ string }}
".NormalizeNewLines()), Is.EqualTo("abc"));
        }

        [Test]
        public void Can_appendToGlobal_existing_string()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript(@"
{{ 'a' |> appendToGlobal: string }}
{{ 'b' |> appendToGlobal: string }}
{{ 'c' |> appendToGlobal: string }}
{{ string }}
".NormalizeNewLines()), Is.EqualTo("abc"));
        }

        [Test]
        public void Can_prependTo_existing_string()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript(@"
{{ 'a' |> prependTo: string }}
{{ 'b' |> prependTo: string }}
{{ 'c' |> prependTo: string }}
{{ string }}
".NormalizeNewLines()), Is.EqualTo("cba"));
        }

        [Test]
        public void Can_prependToGlobal_existing_string()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript(@"
{{ 'a' |> prependToGlobal: string }}
{{ 'b' |> prependToGlobal: string }}
{{ 'c' |> prependToGlobal: string }}
{{ string }}
".NormalizeNewLines()), Is.EqualTo("cba"));
        }

        [Test]
        public void Can_addItem_and_toQueryString()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["nvc"] = new NameValueCollection {["a"] = "1"},
                    ["obj"] = new Dictionary<string, object> { ["a"] = "1" },
                    ["str"] = new Dictionary<string, string> { ["a"] = "1" }
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{ nvc |> addItem({b:2}) |> toQueryString |> raw }}"),
                Is.EqualTo("?a=1&b=2"));

            Assert.That(context.EvaluateScript("{{ obj |> addItem({b:2}) |> toQueryString |> raw }}"),
                Is.EqualTo("?a=1&b=2"));
            Assert.That(context.EvaluateScript("{{ obj |> addItem(pair('b',2)) |> toQueryString |> raw }}"),
                Is.EqualTo("?a=1&b=2"));

            Assert.That(context.EvaluateScript("{{ str |> addItem({b:'2'}) |> toQueryString |> raw }}"),
                Is.EqualTo("?a=1&b=2"));
            Assert.That(context.EvaluateScript("{{ str |> addItem(pair('b','2')) |> toQueryString |> raw }}"),
                Is.EqualTo("?a=1&b=2"));
        }

        [Test]
        public void Return_does_stop_all_execution()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript(@"A{{ return }}B"), Is.EqualTo("A"));
            
            var pageResult = new PageResult(context.OneTimePage("A{{ 1 |> return }}B"));
            Assert.That(pageResult.Result, Is.EqualTo("A"));
            Assert.That(pageResult.ReturnValue.Result, Is.EqualTo(1));
            
            pageResult = new PageResult(context.OneTimePage("A{{ 1 |> return({ a: 1 }) }}B"));
            Assert.That(pageResult.Result, Is.EqualTo("A"));
            Assert.That(pageResult.ReturnValue.Result, Is.EqualTo(1));
            Assert.That(pageResult.ReturnValue.Args, Is.EquivalentTo(new Dictionary<string,object>
            {
                { "a", 1 }
            }));
        }

        [Test]
        public void Can_use_resolveAsset_to_resolve_external_paths()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    [ScriptConstants.AssetsBase] = "http://example.com/assets/"
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{ 'img/logo.png'  |> resolveAsset }}"), Is.EqualTo("http://example.com/assets/img/logo.png"));
            Assert.That(context.EvaluateScript("{{ '/img/logo.png' |> resolveAsset }}"), Is.EqualTo("http://example.com/assets/img/logo.png"));
        }

        [Test]
        public void Returns_path_when_no_assetsBase_exists()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{ 'img/logo.png'  |> resolveAsset }}"), Is.EqualTo("img/logo.png"));
            Assert.That(context.EvaluateScript("{{ '/img/logo.png' |> resolveAsset }}"), Is.EqualTo("/img/logo.png"));
        }

        [Test]
        public void Can_use_isNull_on_nested_properties()
        {
            var sampleModel = new
            {
                StringProperty = "Hello",
                NullStringProperty = (string)null
            };

            var context = new ScriptContext().Init();

            var args = new Dictionary<string, object> { { "sampleArg", sampleModel } };
            Assert.That(context.EvaluateScript("{{ sampleArg |> isNull }}", args), Is.EqualTo("False"));
            Assert.That(context.EvaluateScript("{{ sampleArg.StringProperty |> isNull }}", args), Is.EqualTo("False"));
            Assert.That(context.EvaluateScript("{{ sampleArg.NullStringProperty |> isNull }}", args), Is.EqualTo("True"));
        }

        [Test]
        public void Can_detect_empty_values()
        {
            var context = new ScriptContext {
                Args = {
                    ["nullArg"] = null,
                    ["emptyArg"] = "",
                    ["whitespace"] = " ",
                    ["foo"] = "foo"
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{ unknown |> isNull }}"), Is.EqualTo("True"));
            Assert.That(context.EvaluateScript("{{ nullArg |> isNull }}"), Is.EqualTo("True"));
            Assert.That(context.EvaluateScript("{{ '' |> isNull }}"), Is.EqualTo("False"));
            Assert.That(context.EvaluateScript("{{ `` |> isNull }}"), Is.EqualTo("False"));
            Assert.That(context.EvaluateScript("{{ emptyArg |> isNull }}"), Is.EqualTo("False"));
            Assert.That(context.EvaluateScript("{{ null |> isEmpty }}"), Is.EqualTo("True"));
            Assert.That(context.EvaluateScript("{{ '' |> isEmpty }}"), Is.EqualTo("True"));
            Assert.That(context.EvaluateScript("{{ `` |> isEmpty }}"), Is.EqualTo("True"));
            Assert.That(context.EvaluateScript("{{ emptyArg |> isEmpty }}"), Is.EqualTo("True"));
            Assert.That(context.EvaluateScript("{{ ' ' |> isEmpty }}"), Is.EqualTo("False"));
            Assert.That(context.EvaluateScript("{{ ` ` |> isEmpty }}"), Is.EqualTo("False"));
            Assert.That(context.EvaluateScript("{{ whitespace |> isEmpty }}"), Is.EqualTo("False"));
            Assert.That(context.EvaluateScript("{{ ' ' |> IsNullOrWhiteSpace }}"), Is.EqualTo("True"));
            Assert.That(context.EvaluateScript("{{ ` ` |> IsNullOrWhiteSpace }}"), Is.EqualTo("True"));
            Assert.That(context.EvaluateScript("{{ whitespace |> IsNullOrWhiteSpace }}"), Is.EqualTo("True"));
            Assert.That(context.EvaluateScript("{{ 'foo' |> IsNullOrWhiteSpace }}"), Is.EqualTo("False"));
            Assert.That(context.EvaluateScript("{{ foo |> IsNullOrWhiteSpace }}"), Is.EqualTo("False"));
        }

        public class ShadowScripts : ScriptMethods
        {
            public int add(int x, int y) => x * y;
        }

        [Test]
        public void Can_shadow_default_ScriptMethods_with_InsertScriptMethods()
        {
            var context = new ScriptContext {
                InsertScriptMethods = { new ShadowScripts() }
            }.Init();

            var result = context.Evaluate<int>("{{ add(4,4) |> return }}");
            Assert.That(result, Is.EqualTo(16));
        }

        [Test]
        public void Arguments_can_shadow_existing_filters()
        {
            var context = new ScriptContext {
                Args = {
                    ["min"] = -1
                }
            }.Init();

            var output = context.EvaluateScript("{{ 1 |> assignTo: max}}{{min}}:{{max}}", new ObjectDictionary {
                ["max"] = 1
            });
            Assert.That(output, Is.EqualTo("-1:1"));
        }

        class A
        {
            public int a { get; set; }
            public A(int a) => this.a = a;
        }

        class B 
        {
            public int a { get; set; }
            public string b { get; set; }
            public B(int a, string b)
            {
                this.a = a;
                this.b = b;
            }
        }

        [Test]
        public void Can_use_textDump()
        {
            var context = new ScriptContext().Init();

            var kvpA1 = new KeyValuePair<string,object>("a",1);
            var kvpA2 = new KeyValuePair<string,object>("a",2);
            var kvpBx = new KeyValuePair<string,object>("b","x");
            var kvpBy = new KeyValuePair<string,object>("b","y");
            
            
            Assert.That(context.EvaluateScript("{{ {a:1} |> textDump }}").NormalizeNewLines(), 
                Is.EqualTo("|||\n|-|-|\n| a | 1 |".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> textDump }}", new ObjectDictionary { ["o"] = new A(1) }).NormalizeNewLines(), 
                Is.EqualTo("|||\n|-|-|\n| a | 1 |".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> textDump }}", new ObjectDictionary { ["o"] = kvpA1 }).NormalizeNewLines(), 
                Is.EqualTo("|||\n|-|-|\n| a | 1 |".NormalizeNewLines()));
            
            Assert.That(context.EvaluateScript("{{ {a:1,b:'x'} |> textDump }}").NormalizeNewLines(), 
                Is.EqualTo("|||\n|-|-|\n| a | 1 |\n| b | x |".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> textDump }}", new ObjectDictionary { ["o"] = new B(1, "x") }).NormalizeNewLines(), 
                Is.EqualTo("|||\n|-|-|\n| a | 1 |\n| b | x |".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> textDump }}", new ObjectDictionary { ["o"] = new[]{ kvpA1, kvpBx } }).NormalizeNewLines(), 
                Is.EqualTo("|||\n|-|-|\n| a | 1 |\n| b | x |".NormalizeNewLines()));


            Assert.That(context.EvaluateScript("{{ {a:1} |> textDump({ caption: 'C' }) }}").NormalizeNewLines(), 
                Is.EqualTo("| C    ||\n|---|---|\n| a | 1 |".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> textDump({ caption: 'C' }) }}", new ObjectDictionary { ["o"] = new A(1) }).NormalizeNewLines(), 
                Is.EqualTo("| C    ||\n|---|---|\n| a | 1 |".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> textDump({ caption: 'C' }) }}", new ObjectDictionary { ["o"] = kvpA1 }).NormalizeNewLines(), 
                Is.EqualTo("| C    ||\n|---|---|\n| a | 1 |".NormalizeNewLines()));
            
            
            Assert.That(context.EvaluateScript("{{ [{a:1},{a:2}] |> textDump({ caption: 'C', rowNumbers:false }) }}").NormalizeNewLines(), 
                Is.EqualTo("C\n\n| a |\n|---|\n| 1 |\n| 2 |".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> textDump({ caption: 'C', rowNumbers:false }) }}", new ObjectDictionary { ["o"] = new[]{ new A(1), new A(2) } }).NormalizeNewLines(), 
                Is.EqualTo("C\n\n| a |\n|---|\n| 1 |\n| 2 |".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> textDump({ caption: 'C', rowNumbers:false }) }}", new ObjectDictionary { ["o"] = new[]{ kvpA1, kvpA2 } }).NormalizeNewLines(), 
                Is.EqualTo("C\n\n| a |\n|---|\n| 1 |\n| 2 |".NormalizeNewLines()));

            Assert.That(context.EvaluateScript("{{ [{a:1,b:'x'},{a:2,b:'y'}] |> textDump({ caption: 'C', rowNumbers:false }) }}").NormalizeNewLines(), 
                Is.EqualTo("C\n\n| a | b |\n|---|---|\n| 1 | x |\n| 2 | y |".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> textDump({ caption: 'C', rowNumbers:false }) }}", new ObjectDictionary { ["o"] = new[]{ new B(1, "x"), new B(2, "y") } }).NormalizeNewLines(), 
                Is.EqualTo("C\n\n| a | b |\n|---|---|\n| 1 | x |\n| 2 | y |".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> textDump({ caption: 'C', rowNumbers:false }) }}", new ObjectDictionary { ["o"] = new[]{ new[]{ kvpA1, kvpBx }, new[]{ kvpA2, kvpBy }  } }).NormalizeNewLines(), 
                Is.EqualTo("C\n\n| a | b |\n|---|---|\n| 1 | x |\n| 2 | y |".NormalizeNewLines()));
            

            Assert.That(context.EvaluateScript("{{ [{a:1},{a:2}] |> textDump({ caption: 'C' }) }}").NormalizeNewLines(), 
                Is.EqualTo("C\n\n| # | a |\n|---|---|\n| 1 | 1 |\n| 2 | 2 |".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> textDump({ caption: 'C' }) }}", new ObjectDictionary { ["o"] = new[]{ new A(1), new A(2) } }).NormalizeNewLines(), 
                Is.EqualTo("C\n\n| # | a |\n|---|---|\n| 1 | 1 |\n| 2 | 2 |".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> textDump({ caption: 'C' }) }}", new ObjectDictionary { ["o"] = new[]{ kvpA1, kvpA2 } }).NormalizeNewLines(), 
                Is.EqualTo("C\n\n| # | a |\n|---|---|\n| 1 | 1 |\n| 2 | 2 |".NormalizeNewLines()));

            Assert.That(context.EvaluateScript("{{ [{a:1,b:'x'},{a:2,b:'y'}] |> textDump({ caption: 'C' }) }}").NormalizeNewLines(), 
                Is.EqualTo("C\n\n| # | a | b |\n|---|---|---|\n| 1 | 1 | x |\n| 2 | 2 | y |".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> textDump({ caption: 'C' }) }}", new ObjectDictionary { ["o"] = new[]{ new B(1, "x"), new B(2, "y") } }).NormalizeNewLines(), 
                Is.EqualTo("C\n\n| # | a | b |\n|---|---|---|\n| 1 | 1 | x |\n| 2 | 2 | y |".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> textDump({ caption: 'C' }) }}", new ObjectDictionary { ["o"] = new[]{ new[]{ kvpA1, kvpBx }, new[]{ kvpA2, kvpBy } } }).NormalizeNewLines(), 
                Is.EqualTo("C\n\n| # | a | b |\n|---|---|---|\n| 1 | 1 | x |\n| 2 | 2 | y |".NormalizeNewLines()));

            
            Assert.That(context.EvaluateScript("{{ [1,2] |> textDump({ caption: 'C' }) }}").NormalizeNewLines(), 
                Is.EqualTo("| C |\n|---|\n| 1 |\n| 2 |".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ ['a','b'] |> textDump({ caption: 'C' }) }}").NormalizeNewLines(), 
                Is.EqualTo("| C |\n|---|\n| a |\n| b |".NormalizeNewLines()));
            
            Assert.That(context.EvaluateScript("{{ [1,2] |> textDump }}").NormalizeNewLines(), 
                Is.EqualTo("||\n|-|\n| 1 |\n| 2 |".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ ['a','b'] |> textDump }}").NormalizeNewLines(), 
                Is.EqualTo("||\n|-|\n| a |\n| b |".NormalizeNewLines()));
            
            Assert.That(context.EvaluateScript("{{ [] |> textDump({ caption: 'C', captionIfEmpty: 'E' }) }}").NormalizeNewLines(), 
                Is.EqualTo("E".NormalizeNewLines()));
        }

        [Test]
        public void Can_use_htmlDump()
        {
            var context = new ScriptContext().Init();

            var kvpA1 = new KeyValuePair<string,object>("a",1);
            var kvpA2 = new KeyValuePair<string,object>("a",2);
            var kvpBx = new KeyValuePair<string,object>("b","x");
            var kvpBy = new KeyValuePair<string,object>("b","y");

            
            Assert.That(context.EvaluateScript("{{ {a:1} |> htmlDump }}").NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><tbody><tr><th>a</th><td>1</td></tr></tbody></table>".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> htmlDump }}", new ObjectDictionary { ["o"] = new A(1) }).NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><tbody><tr><th>a</th><td>1</td></tr></tbody></table>".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> htmlDump }}", new ObjectDictionary { ["o"] = kvpA1 }).NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><tbody><tr><th>a</th><td>1</td></tr></tbody></table>".NormalizeNewLines()));
            
            Assert.That(context.EvaluateScript("{{ {a:1,b:'x'} |> htmlDump }}").NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><tbody><tr><th>a</th><td>1</td></tr><tr><th>b</th><td>x</td></tr></tbody></table>".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ {a:1,b:'x'} |> htmlDump }}", new ObjectDictionary { ["o"] = new B(1, "x") }).NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><tbody><tr><th>a</th><td>1</td></tr><tr><th>b</th><td>x</td></tr></tbody></table>".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ {a:1,b:'x'} |> htmlDump }}", new ObjectDictionary { ["o"] = new[]{ kvpA1, kvpBx } }).NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><tbody><tr><th>a</th><td>1</td></tr><tr><th>b</th><td>x</td></tr></tbody></table>".NormalizeNewLines()));


            Assert.That(context.EvaluateScript("{{ {a:1} |> htmlDump({ caption: 'C' }) }}").NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><caption>C</caption><tbody><tr><th>a</th><td>1</td></tr></tbody></table>".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> htmlDump({ caption: 'C' }) }}", new ObjectDictionary { ["o"] = new A(1) }).NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><caption>C</caption><tbody><tr><th>a</th><td>1</td></tr></tbody></table>".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> htmlDump({ caption: 'C' }) }}", new ObjectDictionary { ["o"] = kvpA1 }).NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><caption>C</caption><tbody><tr><th>a</th><td>1</td></tr></tbody></table>".NormalizeNewLines()));
            
            
            Assert.That(context.EvaluateScript("{{ [{a:1},{a:2}] |> htmlDump({ caption: 'C' }) }}").NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><caption>C</caption><thead><tr><th>a</th></tr></thead><tbody><tr><td>1</td></tr><tr><td>2</td></tr></tbody></table>".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> htmlDump({ caption: 'C' }) }}", new ObjectDictionary { ["o"] = new[]{ new A(1), new A(2) } }).NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><caption>C</caption><thead><tr><th>a</th></tr></thead><tbody><tr><td>1</td></tr><tr><td>2</td></tr></tbody></table>".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> htmlDump({ caption: 'C' }) }}", new ObjectDictionary { ["o"] = new[]{ kvpA1, kvpA2 } }).NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><caption>C</caption><thead><tr><th>a</th></tr></thead><tbody><tr><td>1</td></tr><tr><td>2</td></tr></tbody></table>".NormalizeNewLines()));

            Assert.That(context.EvaluateScript("{{ [{a:1,b:'x'},{a:2,b:'y'}] |> htmlDump({ caption: 'C' }) }}").NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><caption>C</caption><thead><tr><th>a</th><th>b</th></tr></thead><tbody><tr><td>1</td><td>x</td></tr><tr><td>2</td><td>y</td></tr></tbody></table>".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> htmlDump({ caption: 'C' }) }}", new ObjectDictionary { ["o"] = new[]{ new B(1, "x"), new B(2, "y") } }).NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><caption>C</caption><thead><tr><th>a</th><th>b</th></tr></thead><tbody><tr><td>1</td><td>x</td></tr><tr><td>2</td><td>y</td></tr></tbody></table>".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ o |> htmlDump({ caption: 'C' }) }}", new ObjectDictionary { ["o"] = new[]{ new[]{ kvpA1, kvpBx }, new[]{ kvpA2, kvpBy } } }).NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><caption>C</caption><thead><tr><th>a</th><th>b</th></tr></thead><tbody><tr><td>1</td><td>x</td></tr><tr><td>2</td><td>y</td></tr></tbody></table>".NormalizeNewLines()));
            
            
            Assert.That(context.EvaluateScript("{{ [1,2] |> htmlDump({ caption: 'C' }) }}").NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><caption>C</caption><tbody><tr><td>1</td></tr><tr><td>2</td></tr></tbody></table>".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ ['a','b'] |> htmlDump({ caption: 'C' }) }}").NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><caption>C</caption><tbody><tr><td>a</td></tr><tr><td>b</td></tr></tbody></table>".NormalizeNewLines()));
            
            Assert.That(context.EvaluateScript("{{ [1,2] |> htmlDump }}").NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><tbody><tr><td>1</td></tr><tr><td>2</td></tr></tbody></table>".NormalizeNewLines()));
            Assert.That(context.EvaluateScript("{{ ['a','b'] |> htmlDump }}").NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table\"><tbody><tr><td>a</td></tr><tr><td>b</td></tr></tbody></table>".NormalizeNewLines()));
            
            
            Assert.That(context.EvaluateScript("{{ [] |> htmlDump({ caption: 'C', captionIfEmpty: 'E', className:'table-bordered' }) }}").NormalizeNewLines(), 
                Is.EqualTo("<table class=\"table-bordered\"><caption>E</caption></table>".NormalizeNewLines()));
        }

        [Test]
        public void Can_use_array_methods()
        {
            // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Array
            var context = new ScriptContext {
                Args = {
                    ["fruits"] = new List<object> { "Apple", "Banana" }
                }
            }.Init();

            Assert.That(context.EvaluateScript("{{fruits[0]}}"), Is.EqualTo("Apple"));
            Assert.That(context.EvaluateScript("{{fruits[fruits.Count - 1]}}"), Is.EqualTo("Banana"));
            Assert.That(context.EvaluateScript("{{ [] |> to => result}}{{fruits.forEach((item,index) => result.push(`${item} ${index}`))}}{{result |> join}}"), 
                Is.EqualTo("Apple 0,Banana 1"));
            Assert.That(context.EvaluateScript("{{#each fruits}}{{`${it} ${index},`}}{{/each}}"), 
                Is.EqualTo("Apple 0,Banana 1,"));
            Assert.That(context.EvaluateScript("{{fruits.push('Orange')}} => {{fruits |> join}}"), 
                Is.EqualTo("3 => Apple,Banana,Orange"));
            Assert.That(context.EvaluateScript("{{fruits.pop()}}"), 
                Is.EqualTo("Orange"));
            Assert.That(context.EvaluateScript("{{fruits.shift()}}"), 
                Is.EqualTo("Apple"));
            Assert.That(context.EvaluateScript("{{fruits.unshift('Strawberry')}} => {{fruits |> join}}"), 
                Is.EqualTo("2 => Strawberry,Banana"));
            Assert.That(context.EvaluateScript("{{fruits.push('Mango')}} : {{fruits.indexOf('Banana')}}"), 
                Is.EqualTo("3 : 1"));
            Assert.That(context.EvaluateScript("{{fruits.indexOf('Banana')}}"), 
                Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{fruits.splice(fruits.indexOf('Banana'),1) |> join}} : {{fruits |> join}}"), 
                Is.EqualTo("Banana : Strawberry,Mango"));
            Assert.That(context.EvaluateScript("{{fruits.Count}} : {{fruits.slice().push('Pear')}} : {{fruits.Count}}"), 
                Is.EqualTo("2 : 3 : 2"));
        }

        [Test]
        public void Can_use_array_splice()
        {
            var context = new ScriptContext {
                Args = {
                    ["vegetables"] = new List<object> { "Cabbage", "Turnip", "Radish", "Carrot" }
                }
            }.Init();

            
            Assert.That(context.EvaluateScript("{{vegetables.splice(1,2) |> join}} : {{ vegetables |> join }}"), 
                Is.EqualTo("Turnip,Radish : Cabbage,Carrot"));
        }

        [Test]
        public void Can_use_other_array_methods()
        {
            var context = new ScriptContext().Init();

            Assert.That(context.EvaluateScript("{{['a', 'b', 'c'].concat(['d', 'e', 'f']) |> join}}"),
                Is.EqualTo("a,b,c,d,e,f"));

            Assert.That(context.EvaluateScript("{{[1, 30, 39, 29, 10, 13].every(x => x < 40)}}"),
                Is.EqualTo("True"));

            Assert.That(context.EvaluateScript("{{['spray', 'limit', 'elite', 'exuberant', 'destruction', 'present'].filter(word => word.Length > 6) |> join}}"),
                Is.EqualTo("exuberant,destruction,present"));
            
            Assert.That(context.EvaluateScript("{{[5, 12, 8, 130, 44].find(x => x > 10) |> join}}"),
                Is.EqualTo("12"));
            
            Assert.That(context.EvaluateScript("{{[5, 12, 8, 130, 44].findIndex(x => x > 13) |> join}}"),
                Is.EqualTo("3"));
            
            Assert.That(context.EvaluateScript("{{[1, 2, [3, 4, [5, 6]]].flat(2) |> join}}"),
                Is.EqualTo("1,2,3,4,5,6"));
            
            Assert.That(context.EvaluateScript("{{[1, 2, 3, 4].flatMap(x => [x * 2]) |> join}}"),
                Is.EqualTo("2,4,6,8"));
            
            Assert.That(context.EvaluateScript("{{[1, 2, 3].includes(2)}}"), Is.EqualTo("True"));
            Assert.That(context.EvaluateScript("{{['cat', 'dog', 'bat'].includes('cat')}}"), Is.EqualTo("True"));
            Assert.That(context.EvaluateScript("{{['cat', 'dog', 'bat'].includes('at')}}"), Is.EqualTo("False"));
            
            Assert.That(context.EvaluateScript("{{['ant', 'bison', 'camel', 'duck', 'bison'].indexOf('bison')}}"), Is.EqualTo("1"));
            Assert.That(context.EvaluateScript("{{['ant', 'bison', 'camel', 'duck', 'bison'].indexOf('bison',2)}}"), Is.EqualTo("4"));
            Assert.That(context.EvaluateScript("{{['ant', 'bison', 'camel', 'duck', 'bison'].indexOf('giraffe')}}"), Is.EqualTo("-1"));
            
            Assert.That(context.EvaluateScript("{{['Fire', 'Air', 'Water'].join()}}"), Is.EqualTo("Fire,Air,Water"));
            Assert.That(context.EvaluateScript("{{['Fire', 'Air', 'Water'].join('')}}"), Is.EqualTo("FireAirWater"));
            Assert.That(context.EvaluateScript("{{['Fire', 'Air', 'Water'].join('-')}}"), Is.EqualTo("Fire-Air-Water"));

            Assert.That(context.EvaluateScript("{{['a', 'b', 'c'].keys() |> join}}"), Is.EqualTo("0,1,2"));
            
            Assert.That(context.EvaluateScript("{{['Dodo', 'Tiger', 'Penguin', 'Dodo'].lastIndexOf('Dodo')}}"), Is.EqualTo("3"));
            Assert.That(context.EvaluateScript("{{['Dodo', 'Tiger', 'Penguin', 'Dodo'].lastIndexOf('Tiger')}}"), Is.EqualTo("1"));

            Assert.That(context.EvaluateScript("{{[1, 4, 9, 16].map(x => x * 2) |> join}}"), Is.EqualTo("2,8,18,32"));
            
            Assert.That(context.EvaluateScript("{{['broccoli', 'cauliflower', 'cabbage', 'kale', 'tomato'].pop()}}"), Is.EqualTo("tomato"));
            
            Assert.That(context.EvaluateScript("{{['pigs', 'goats', 'sheep'].push('cows')}}"), Is.EqualTo("4"));
            
            Assert.That(context.EvaluateScript("{{[1, 2, 3, 4].reduce((accumulator, currentValue) => accumulator + currentValue)}}"), Is.EqualTo("10"));
            Assert.That(context.EvaluateScript("{{[1, 2, 3, 4].reduce((accumulator, currentValue) => accumulator + currentValue, 5)}}"), Is.EqualTo("15"));
            
            Assert.That(context.EvaluateScript("{{['one', 'two', 'three'].reverse() |> join}}"), Is.EqualTo("three,two,one"));
            
            Assert.That(context.EvaluateScript("{{[1, 2, 3].shift()}}"), Is.EqualTo("1"));
            
            Assert.That(context.EvaluateScript("{{['ant', 'bison', 'camel', 'duck', 'elephant'] |> to => animals}}{{animals.slice(2) |> join}}"), Is.EqualTo("camel,duck,elephant"));
            Assert.That(context.EvaluateScript("{{['ant', 'bison', 'camel', 'duck', 'elephant'] |> to => animals}}{{animals.slice(2,4) |> join}}"), Is.EqualTo("camel,duck"));
            Assert.That(context.EvaluateScript("{{['ant', 'bison', 'camel', 'duck', 'elephant'] |> to => animals}}{{animals.slice(1,5) |> join}}"), Is.EqualTo("bison,camel,duck,elephant"));

            Assert.That(context.EvaluateScript("{{[1, 2, 3, 4, 5].some(x => x % 2 == 0)}}"), Is.EqualTo("True"));

            Assert.That(context.EvaluateScript("{{['March', 'Jan', 'Feb', 'Dec'].sort() |> join}}"), Is.EqualTo("Dec,Feb,Jan,March"));
            Assert.That(context.EvaluateScript("{{[1, 30, 4, 21, 100000].sort() |> join}}"), Is.EqualTo("1,4,21,30,100000"));

            Assert.That(context.EvaluateScript(
                "{{['Jan', 'March', 'April', 'June'] |> to => months}}{{months.splice(1,0,['Feb']) |> end}}{{months |> join}}"), 
                Is.EqualTo("Jan,Feb,March,April,June"));
            
            Assert.That(context.EvaluateScript("{{[1, 2, 3] |> to => array}}{{array.unshift([4,5])}} : {{array |> join}}"), Is.EqualTo("5 : 4,5,1,2,3"));

            Assert.That(context.EvaluateScript("{{['a', 'b', 'c'].values() |> join}}"), Is.EqualTo("a,b,c"));
        }

        [Test]
        public void Can_use_forEach_on_dictionaries()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript(
                "{{ {a:1,b:2,c:3} |> to => d }}{{ []  |> to => values}}{{d.forEach((key,val) => values.push(val))}}{{values |> join}}"),
                Is.EqualTo("1,2,3"));
            
        }

        [Test]
        public void Can_flatMap()
        {
            var context = new ScriptContext().Init();
            Assert.That(context.Evaluate("{{ flatten([[1,2],[3,4]]) |> return }}"), Is.EqualTo(new[]{ 1, 2, 3, 4 }));
        }

        [Test]
        public void Can_removeKeyFromDictionary()
        {
            var context = new ScriptContext().Init();
            var output = context.RenderScript(@"```code|q
sample = {}
sample.myKey1 = 1
sample.myKey2 = 2
sample |> remove('myKey1')
sample |> removeKeyFromDictionary('myKey2')
```
{{ sample.myKey }}".NormalizeNewLines());
            
            Assert.That(output, Is.EqualTo(""));
        }

        [Test]
        public void Can_use_ownProps()
        {
            var context = new ScriptContext().Init();

            var output = context.RenderScript(@"
{{#partial test}}
{{ it |> ownProps |> map => it.Key |> jsv }}|{{ it.ownProps().map(x => x.Key).jsv() }}
{{/partial}}
{{ 'test' | partial({ A:1, B:2 }) }}".NormalizeNewLines());

            output.Print();
            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo("[A,B]|[A,B]"));
        }

    }
}