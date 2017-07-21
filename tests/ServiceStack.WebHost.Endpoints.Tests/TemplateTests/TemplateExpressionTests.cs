using NUnit.Framework;
using ServiceStack.Templates;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateExpressionTests
    {
        [Test]
        public void Can_assign_list_numbers()
        {
            var context = new TemplateContext().Init();

            Assert.That(context.EvaluateTemplate(@"
{{ [1,2,3] | assignTo: numArray }}
{{ do: assign('numArray[1]', 4) }}
{{ numArray[1] }}
").Trim(), Is.EqualTo("4"));
        }
        
        [Test]
        public void Can_assign_array_numbers()
        {
            var context = new TemplateContext().Init();

            Assert.That(context.EvaluateTemplate(@"
{{ [1,2,3] | toArray | assignTo: numArray }}
{{ do: assign('numArray[1]', 4) }}
{{ numArray[1] }}
").Trim(), Is.EqualTo("4"));
        }
        
        [Test]
        public void Can_assign_list_strings()
        {
            var context = new TemplateContext().Init();

            Assert.That(context.EvaluateTemplate(@"
{{ ['a','b','c'] | assignTo: stringArray }}
{{ do: assign('stringArray[1]', 'd') }}
{{ stringArray[1] }}
").Trim(), Is.EqualTo("d"));
        }
        
        [Test]
        public void Can_assign_array_strings()
        {
            var context = new TemplateContext().Init();

            Assert.That(context.EvaluateTemplate(@"
{{ ['a','b','c'] | toArray | assignTo: stringArray }}
{{ do: assign('stringArray[1]', 'd') }}
{{ stringArray[1] }}
").Trim(), Is.EqualTo("d"));
        }
         
        [Test]
        public void Can_assign_dictionary_strings()
        {
            var context = new TemplateContext().Init();

            Assert.That(context.EvaluateTemplate(@"
{{ { a: 'foo', b: 'bar' } | assignTo: map }}
{{ do: assign('map[`b`]', 'qux') }}
{{ map['b'] }}
").Trim(), Is.EqualTo("qux"));
        }
    }
}