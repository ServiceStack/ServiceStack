using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ScriptBlockHtmlTests
    {
        [Test]
        public void Does_evaluate_void_img_html_block()
        {
            var context = new ScriptContext().Init();
            
            Assert.That(context.EvaluateScript("{{#img {alt:'image',src:'image.png'} }}{{/img}}"), 
                Is.EqualTo("<img alt=\"image\" src=\"image.png\">"));
        }

        [Test]
        public void Does_evaluate_ul_html_block()
        {
            var context = new ScriptContext {
                Args = {
                    ["numbers"] = new int[]{1, 2, 3},
                    ["letters"] = new[]{ "A", "B", "C" },
                }
            }.Init();
            
            Assert.That(context.EvaluateScript("{{#ul}}{{/ul}}").RemoveNewLines(), Is.EqualTo("<ul></ul>"));
            
            Assert.That(context.EvaluateScript("{{#ul {class:'nav'} }} <li>item</li> {{/ul}}").RemoveNewLines(), 
                Is.EqualTo(@"<ul class=""nav""> <li>item</li> </ul>"));
            
            Assert.That(context.EvaluateScript("{{#ul {each:letters, class:'nav', id:'menu'} }}<li>{{it}}</li>{{/ul}}").RemoveNewLines(), 
                Is.EqualTo(@"<ul class=""nav"" id=""menu""><li>A</li><li>B</li><li>C</li></ul>"));
            
            Assert.That(context.EvaluateScript("{{#ul {each:numbers, it:'num'} }}<li>{{num}}</li>{{/ul}}").RemoveNewLines(), 
                Is.EqualTo(@"<ul><li>1</li><li>2</li><li>3</li></ul>"));
            
            Assert.That(context.EvaluateScript("{{#ul {each:none} }}<li>{{it}}</li>{{/ul}}").RemoveNewLines(), Is.EqualTo(@""));
            
            Assert.That(context.EvaluateScript("{{#ul {each:none} }}<li>{{it}}</li>{{else}}no items{{/ul}}").RemoveNewLines(), 
                Is.EqualTo(@"no items"));
        }

        private static ScriptContext CreateContext()
        {
            var context = new ScriptContext {
                Args = {
                    ["items"] = new[] {new Person("foo", 1), new Person("bar", 2), new Person("baz", 3)},
                    ["id"] = "menu",
                    ["disclaimerAccepted"] = false,
                    ["hasAccess"] = true,
                    ["highlight"] = "baz",
                    ["digits"] = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" },
                }
            }.Init();
            return context;
        }

        [Test]
        public void Does_evaluate_ul_with_nested_html_blocks()
        {
            var context = CreateContext();

            var template = @"
{{#ul {each:items, class:['nav', !disclaimerAccepted?'blur':''], id:`ul-${id}`} }}
    {{#li {class: {alt:isOdd(index), active:Name==highlight} }}
        {{Name}}
    {{/li}}
{{else}}
    <div>no items</div>
{{/ul}}";

            var result = context.EvaluateScript(template);
            
            Assert.That(result.NormalizeNewLines(), Is.EqualTo(@"
<ul class=""nav blur"" id=""ul-menu"">
    <li>
        foo
    </li>
    <li class=""alt"">
        bar
    </li>
    <li class=""active"">
        baz
    </li>
</ul>".NormalizeNewLines()));

            var withoutHtmlBlock = @"
{{#if !isEmpty(items)}}
<ul{{ ['nav', !disclaimerAccepted?'blur':''] |> htmlClass }} id=""ul-{{id}}"">
{{#each items}}
    <li{{ {alt:isOdd(index), active:Name==highlight} |> htmlClass }}>
        {{Name}}
    </li>
{{/each}}
</ul>
{{else}}
   <div>no items</div>
{{/if}}";

            var withoutBlockResult = context.EvaluateScript(withoutHtmlBlock);
            
            Assert.That(withoutBlockResult.RemoveNewLines(), Is.EqualTo(result.RemoveNewLines()));
        }

        [Test]
        public void Does_evaluate_if_and_where_in_html_blocks()
        {
            var context = CreateContext();

            var template = @"
{{#ul {if:hasAccess, each:items, where:'Age >= 2', class:['nav', !disclaimerAccepted?'blur':''], id:`ul-${id}`} }}
    {{#li {class: {alt:isOdd(index), active:Name==highlight} }}
        {{Name}}
    {{/li}}
{{else}}
    <div>no items</div>
{{/ul}}";

            var result = context.EvaluateScript(template);
            Assert.That(result.NormalizeNewLines(), Is.EqualTo(@"
<ul class=""nav blur"" id=""ul-menu"">
    <li>
        bar
    </li>
    <li class=""alt active"">
        baz
    </li>
</ul>".NormalizeNewLines()));

            var withoutHtmlBlock = @"
{{ items |> where: it.Age >= 2  
   |> assignTo: items }}
{{#if !isEmpty(items)}}
{{#if hasAccess}}
<ul{{ ['nav', !disclaimerAccepted?'blur':''] |> htmlClass }} id=""ul-{{id}}"">
{{#each items}}
    <li{{ {alt:isOdd(index), active:Name==highlight} |> htmlClass }}>
        {{Name}}
    </li>
{{/each}}
</ul>
{{/if}}
 {{else}}
     <div>no items</div>
 {{/if}}".NormalizeNewLines();

            var withoutBlockResult = context.EvaluateScript(withoutHtmlBlock);
            withoutBlockResult.Print();
            Assert.That(withoutBlockResult.RemoveNewLines(), Is.EqualTo(result.RemoveNewLines()));

            result = context.EvaluateScript(template.Replace("hasAccess","!hasAccess"));
            Assert.That(result.NormalizeNewLines(), Is.EqualTo(@""));
        }

        [Test]
        public void Does_evaluate_where_expression_on_strings()
        {
            var context = CreateContext();

            var template = @"
{{#each d in digits where d.Length < index}}
The word {{d}} is shorter than its value.
{{/each}}";

            var result = context.EvaluateScript(template);
            Assert.That(result.NormalizeNewLines(), Is.EqualTo(@"
The word five is shorter than its value.
The word six is shorter than its value.
The word seven is shorter than its value.
The word eight is shorter than its value.
The word nine is shorter than its value.".NormalizeNewLines()));
        }
        
    }

}