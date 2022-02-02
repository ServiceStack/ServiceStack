using NUnit.Framework;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ExpressionTests
    {
        [Test]
        public void Can_assign_list_numbers()
        {
            var context = new ScriptContext().Init();

            Assert.That(context.EvaluateScript(@"
{{ [1,2,3] |> assignTo: numArray }}
{{ do: assign('numArray[1]', 4) }}
{{ numArray[1] }}
").Trim(), Is.EqualTo("4"));
        }

        [Test]
        public void Does_not_execute_do_on_null_or_none_existing_value()
        {
            var context = new ScriptContext().Init();

            Assert.That(context.EvaluateScript(@"
{{ 1 |> assignTo: arg }}
{{ arg |> do: assign('doArg', incr(it)) }}
{{ doArg }}
").Trim(), Is.EqualTo("2"));
            
            Assert.That(context.EvaluateScript(@"
{{ null |> assignTo: arg }}
{{ arg |> do: assign('doArg', 2) }}
{{ doArg }}
").Trim(), Is.EqualTo(""));
            
            Assert.That(context.EvaluateScript(@"
{{ noArg |> do: assign('doArg', 2) }}
{{ doArg }}
").Trim(), Is.EqualTo(""));
        }
        
        [Test]
        public void Can_assign_array_numbers()
        {
            var context = new ScriptContext().Init();

            Assert.That(context.EvaluateScript(@"
{{ [1,2,3] |> toArray |> assignTo: numArray }}
{{ do: assign('numArray[1]', 4) }}
{{ numArray[1] }}
").Trim(), Is.EqualTo("4"));
        }
        
        [Test]
        public void Can_assign_list_strings()
        {
            var context = new ScriptContext().Init();

            Assert.That(context.EvaluateScript(@"
{{ ['a','b','c'] |> assignTo: stringArray }}
{{ do: assign('stringArray[1]', 'd') }}
{{ stringArray[1] }}
").Trim(), Is.EqualTo("d"));
        }
        
        [Test]
        public void Can_assign_array_strings()
        {
            var context = new ScriptContext().Init();

            Assert.That(context.EvaluateScript(@"
{{ ['a','b','c'] |> toArray |> assignTo: stringArray }}
{{ do: assign('stringArray[1]', 'd') }}
{{ stringArray[1] }}
").Trim(), Is.EqualTo("d"));
        }
         
        [Test]
        public void Can_assign_dictionary_strings()
        {
            var context = new ScriptContext().Init();

            Assert.That(context.EvaluateScript(@"
{{ { a: 'foo', b: 'bar' } |> assignTo: map }}
{{ do: assign('map[`b`]', 'qux') }}
{{ map['b'] }}
").Trim(), Is.EqualTo("qux"));
        }
    }
}