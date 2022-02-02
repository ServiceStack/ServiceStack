using System;
using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class SharpPageUtilsTests
    {
        [Test]
        public void Can_parse_template_with_no_vars()
        {
            Assert.That(ScriptTemplateUtils.ParseTemplate("").Count, Is.EqualTo(0));
            var fragments = ScriptTemplateUtils.ParseTemplate("<h1>title</h1>");
            Assert.That(fragments.Count, Is.EqualTo(1));

            var strFragment = fragments[0] as PageStringFragment;
            Assert.That(strFragment.Value.ToString(), Is.EqualTo("<h1>title</h1>"));
        }

        [Test]
        public void Can_parse_template_with_variable()
        {
            var fragments = ScriptTemplateUtils.ParseTemplate("<h1>{{ title }}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as PageStringFragment;
            var varFragment2 = fragments[1] as PageVariableFragment;
            var strFragment3 = fragments[2] as PageStringFragment;

            Assert.That(strFragment1.Value.ToString(), Is.EqualTo("<h1>"));
            Assert.That(varFragment2.OriginalText.ToString(), Is.EqualTo("{{ title }}"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(0));
            Assert.That(strFragment3.Value.ToString(), Is.EqualTo("</h1>"));
        }

        [Test]
        public void Can_parse_template_with_filter()
        {
            var fragments = ScriptTemplateUtils.ParseTemplate("<h1>{{ title |> filter }}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as PageStringFragment;
            var varFragment2 = fragments[1] as PageVariableFragment;
            var strFragment3 = fragments[2] as PageStringFragment;

            Assert.That(strFragment1.Value.ToString(), Is.EqualTo("<h1>"));
            Assert.That(varFragment2.OriginalText.ToString(), Is.EqualTo("{{ title |> filter }}"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterExpressions[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterExpressions[0].Arguments.Length, Is.EqualTo(0));
            Assert.That(strFragment3.Value.ToString(), Is.EqualTo("</h1>"));

            fragments = ScriptTemplateUtils.ParseTemplate("<h1>{{ title |> filter() }}</h1>");

            varFragment2 = fragments[1] as PageVariableFragment;
            Assert.That(varFragment2.OriginalText.ToString(), Is.EqualTo("{{ title |> filter() }}"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterExpressions[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterExpressions[0].Arguments.Length, Is.EqualTo(0));
        }

        [Test]
        public void Can_parse_template_with_filter_without_whitespace()
        {
            var fragments = ScriptTemplateUtils.ParseTemplate("<h1>{{title}}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as PageStringFragment;
            var varFragment2 = fragments[1] as PageVariableFragment;
            var strFragment3 = fragments[2] as PageStringFragment;

            Assert.That(strFragment1.Value.ToString(), Is.EqualTo("<h1>"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(0));

            fragments = ScriptTemplateUtils.ParseTemplate("<h1>{{title|filter}}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            strFragment1 = fragments[0] as PageStringFragment;
            varFragment2 = fragments[1] as PageVariableFragment;
            strFragment3 = fragments[2] as PageStringFragment;

            Assert.That(strFragment1.Value.ToString(), Is.EqualTo("<h1>"));
            Assert.That(varFragment2.OriginalText.ToString(), Is.EqualTo("{{title|filter}}"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterExpressions[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterExpressions[0].Arguments.Length, Is.EqualTo(0));
            Assert.That(strFragment3.Value.ToString(), Is.EqualTo("</h1>"));
        }

        [Test]
        public void Can_parse_template_with_filter_with_arg()
        {
            var fragments = ScriptTemplateUtils.ParseTemplate("<h1>{{ title |> filter(1) }}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as PageStringFragment;
            var varFragment2 = fragments[1] as PageVariableFragment;
            var strFragment3 = fragments[2] as PageStringFragment;

            Assert.That(strFragment1.Value.ToString(), Is.EqualTo("<h1>"));
            Assert.That(varFragment2.OriginalText.ToString(), Is.EqualTo("{{ title |> filter(1) }}"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterExpressions[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterExpressions[0].Arguments.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterExpressions[0].Arguments[0], Is.EqualTo(new JsLiteral(1)));
            Assert.That(strFragment3.Value.ToString(), Is.EqualTo("</h1>"));
        }

        [Test]
        public void Can_parse_template_with_filter_with_multiple_args()
        {
            var fragments = ScriptTemplateUtils.ParseTemplate("<h1>{{ title |> filter(1,2.2,'a',\"b\",true) }}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as PageStringFragment;
            var varFragment2 = fragments[1] as PageVariableFragment;
            var strFragment3 = fragments[2] as PageStringFragment;

            Assert.That(strFragment1.Value.ToString(), Is.EqualTo("<h1>"));
            Assert.That(varFragment2.OriginalText.ToString(), Is.EqualTo("{{ title |> filter(1,2.2,'a',\"b\",true) }}"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterExpressions[0].Name, Is.EqualTo("filter"));
            Assert.That(varFragment2.FilterExpressions[0].Arguments.Length, Is.EqualTo(5));
            Assert.That(varFragment2.FilterExpressions[0].Arguments[0], Is.EqualTo(new JsLiteral(1)));
            Assert.That(varFragment2.FilterExpressions[0].Arguments[1], Is.EqualTo(new JsLiteral(2.2)));
            Assert.That(varFragment2.FilterExpressions[0].Arguments[2], Is.EqualTo(new JsLiteral("a")));
            Assert.That(varFragment2.FilterExpressions[0].Arguments[3], Is.EqualTo(new JsLiteral("b")));
            Assert.That(varFragment2.FilterExpressions[0].Arguments[4], Is.EqualTo(JsLiteral.True));
            Assert.That(strFragment3.Value.ToString(), Is.EqualTo("</h1>"));
        }

        [Test]
        public void Can_parse_template_with_multiple_filters_and_multiple_args()
        {
            var fragments = ScriptTemplateUtils.ParseTemplate("<h1>{{ title |> filter1 |> filter2(1) |> filter3(1,2.2,'a',\"b\",true) }}</h1>");
            Assert.That(fragments.Count, Is.EqualTo(3));

            var strFragment1 = fragments[0] as PageStringFragment;
            var varFragment2 = fragments[1] as PageVariableFragment;
            var strFragment3 = fragments[2] as PageStringFragment;

            Assert.That(strFragment1.Value.ToString(), Is.EqualTo("<h1>"));
            Assert.That(varFragment2.OriginalText.ToString(), Is.EqualTo("{{ title |> filter1 |> filter2(1) |> filter3(1,2.2,'a',\"b\",true) }}"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(3));

            Assert.That(varFragment2.FilterExpressions[0].Name, Is.EqualTo("filter1"));
            Assert.That(varFragment2.FilterExpressions[0].Arguments.Length, Is.EqualTo(0));

            Assert.That(varFragment2.FilterExpressions[1].Name, Is.EqualTo("filter2"));
            Assert.That(varFragment2.FilterExpressions[1].Arguments.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterExpressions[1].Arguments[0], Is.EqualTo(new JsLiteral(1)));

            Assert.That(varFragment2.FilterExpressions[2].Name, Is.EqualTo("filter3"));
            Assert.That(varFragment2.FilterExpressions[2].Arguments.Length, Is.EqualTo(5));
            Assert.That(varFragment2.FilterExpressions[2].Arguments[0], Is.EqualTo(new JsLiteral(1)));
            Assert.That(varFragment2.FilterExpressions[2].Arguments[1], Is.EqualTo(new JsLiteral(2.2)));
            Assert.That(varFragment2.FilterExpressions[2].Arguments[2], Is.EqualTo(new JsLiteral("a")));
            Assert.That(varFragment2.FilterExpressions[2].Arguments[3], Is.EqualTo(new JsLiteral("b")));
            Assert.That(varFragment2.FilterExpressions[2].Arguments[4], Is.EqualTo(JsLiteral.True));

            Assert.That(strFragment3.Value.ToString(), Is.EqualTo("</h1>"));
        }

        [Test]
        public void Can_parse_template_with_multiple_variables_and_filters()
        {
            var fragments = ScriptTemplateUtils.ParseTemplate("<h1>{{ title |> filter1 }}</h1>\n<p>{{ content |> filter2(a) }}</p>");
            Assert.That(fragments.Count, Is.EqualTo(5));

            var strFragment1 = fragments[0] as PageStringFragment;
            var varFragment2 = fragments[1] as PageVariableFragment;
            var strFragment3 = fragments[2] as PageStringFragment;
            var varFragment4 = fragments[3] as PageVariableFragment;
            var strFragment5 = fragments[4] as PageStringFragment;

            Assert.That(strFragment1.Value.ToString(), Is.EqualTo("<h1>"));

            Assert.That(varFragment2.OriginalText.ToString(), Is.EqualTo("{{ title |> filter1 }}"));
            Assert.That(varFragment2.Binding, Is.EqualTo("title"));
            Assert.That(varFragment2.FilterExpressions.Length, Is.EqualTo(1));
            Assert.That(varFragment2.FilterExpressions[0].Name, Is.EqualTo("filter1"));
            Assert.That(varFragment2.FilterExpressions[0].Arguments.Length, Is.EqualTo(0));

            Assert.That(strFragment3.Value.ToString(), Is.EqualTo("</h1>\n<p>"));

            Assert.That(varFragment4.OriginalText.ToString(), Is.EqualTo("{{ content |> filter2(a) }}"));
            Assert.That(varFragment4.Binding, Is.EqualTo("content"));
            Assert.That(varFragment4.FilterExpressions.Length, Is.EqualTo(1));
            Assert.That(varFragment4.FilterExpressions[0].Name, Is.EqualTo("filter2"));
            Assert.That(varFragment4.FilterExpressions[0].Arguments.Length, Is.EqualTo(1));
            Assert.That(varFragment4.FilterExpressions[0].Arguments[0], Is.EqualTo(new JsIdentifier("a")));

            Assert.That(strFragment5.Value.ToString(), Is.EqualTo("</p>"));
        }

        [Test]
        public void Can_parse_template_with_only_variable()
        {
            var fragments = ScriptTemplateUtils.ParseTemplate("{{ filter }}");
            Assert.That(fragments.Count, Is.EqualTo(1));
            Assert.That(((PageVariableFragment)fragments[0]).Binding, Is.EqualTo("filter"));
        }

        [Test]
        public void Can_parse_template_with_arg_and_multiple_filters()
        {
            var fragments = ScriptTemplateUtils.ParseTemplate("{{ ' - {{it}}' |> forEach(items) |> markdown }}");
            var varFragment = fragments[0] as PageVariableFragment;
            
            Assert.That(varFragment.OriginalText.ToString(), Is.EqualTo("{{ ' - {{it}}' |> forEach(items) |> markdown }}"));
            Assert.That(varFragment.FilterExpressions.Length, Is.EqualTo(2));
            Assert.That(varFragment.FilterExpressions[0].Name, Is.EqualTo("forEach"));
            Assert.That(varFragment.FilterExpressions[0].Arguments.Length, Is.EqualTo(1));
            Assert.That(varFragment.FilterExpressions[0].Arguments[0], Is.EqualTo(new JsIdentifier("items")));
            Assert.That(varFragment.FilterExpressions[1].Name, Is.EqualTo("markdown"));
        }

        [Test]
        public void Can_parse_filter_with_different_arg_types()
        {
            var fragments = ScriptTemplateUtils.ParseTemplate("{{ array(['a',1,'c']) }}");
            var varFragment = (PageVariableFragment)fragments[0];
            
            Assert.That(varFragment.OriginalText.ToString(), Is.EqualTo("{{ array(['a',1,'c']) }}"));
            Assert.That(varFragment.InitialExpression.Name, Is.EqualTo("array"));
            Assert.That(varFragment.InitialExpression.Arguments.Length, Is.EqualTo(1));
        }

        [Test]
        public void Can_parse_next_token()
        {
            JsToken token;

            "a".ParseJsExpression(out token);
            Assert.That(((JsIdentifier)token).Name, Is.EqualTo("a"));
            "a2".ParseJsExpression(out token);
            Assert.That(((JsIdentifier)token).Name, Is.EqualTo("a2"));
            " a2 ".ParseJsExpression(out token);
            Assert.That(((JsIdentifier)token).Name, Is.EqualTo("a2"));
            "'a'".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLiteral("a")));
            "\"a\"".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLiteral("a")));
            "`a`".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsTemplateLiteral("a")));
            "1".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLiteral(1)));
            "100".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLiteral(100)));
            "100.0".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLiteral(100d)));
            "1.0E+2".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLiteral(100d)));
            "1e+2".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLiteral(100d)));
            "true".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(JsLiteral.True));
            "false".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(JsLiteral.False));
            "null".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(JsNull.Value));
            "{foo:1}".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsObjectExpression(new JsProperty(new JsIdentifier("foo"), new JsLiteral(1)))
            ));
            "{ foo : 1 , bar: 'qux', d: 1.1, b:false, n:null }".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsObjectExpression(
                    new JsProperty(new JsIdentifier("foo"), new JsLiteral(1)),
                    new JsProperty(new JsIdentifier("bar"), new JsLiteral("qux")),
                    new JsProperty(new JsIdentifier("d"), new JsLiteral(1.1)),
                    new JsProperty(new JsIdentifier("b"), new JsLiteral(false)),
                    new JsProperty(new JsIdentifier("n"), JsNull.Value)
                )
            ));
            "{ map : { bar: 'qux', b: true } }".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsObjectExpression(
                    new JsProperty(
                        new JsIdentifier("map"), 
                        new JsObjectExpression(
                            new JsProperty(new JsIdentifier("bar"), new JsLiteral("qux")),
                            new JsProperty(new JsIdentifier("b"), new JsLiteral(true))
                        )
                    )
                )
            ));
            "{varRef:foo}".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsObjectExpression(new JsProperty(new JsIdentifier("varRef"), new JsIdentifier("foo")))
            ));
            "{ \"foo\" : 1 , \"bar\": 'qux', \"d\": 1.1, \"b\":false, \"n\":null }".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsObjectExpression(
                    new JsProperty(new JsLiteral("foo"), new JsLiteral(1)),
                    new JsProperty(new JsLiteral("bar"), new JsLiteral("qux")),
                    new JsProperty(new JsLiteral("d"), new JsLiteral(1.1)),
                    new JsProperty(new JsLiteral("b"), new JsLiteral(false)),
                    new JsProperty(new JsLiteral("n"), JsNull.Value)
                )
            ));
            "{ `foo` : 1 , `bar`: 'qux', `d`: 1.1, `b`:false, `n`:null }".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsObjectExpression(
                    new JsProperty(new JsTemplateLiteral("foo"), new JsLiteral(1)),
                    new JsProperty(new JsTemplateLiteral("bar"), new JsLiteral("qux")),
                    new JsProperty(new JsTemplateLiteral("d"), new JsLiteral(1.1)),
                    new JsProperty(new JsTemplateLiteral("b"), new JsLiteral(false)),
                    new JsProperty(new JsTemplateLiteral("n"), JsNull.Value)
                )
            ));

            "[1,2,3]".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsArrayExpression(new JsLiteral(1),new JsLiteral(2),new JsLiteral(3))));
            "[a,b,c]".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsArrayExpression(new JsIdentifier("a"),new JsIdentifier("b"),new JsIdentifier("c"))));
            "[a.Id,b.Name]".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsArrayExpression(
                new JsMemberExpression(new JsIdentifier("a"), new JsIdentifier("Id")),
                new JsMemberExpression(new JsIdentifier("b"), new JsIdentifier("Name"))
            )));
            "{ x: a.Id, y: b.Name }".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsObjectExpression(
                    new JsProperty(new JsIdentifier("x"), new JsMemberExpression(new JsIdentifier("a"), new JsIdentifier("Id"))),
                    new JsProperty(new JsIdentifier("y"), new JsMemberExpression(new JsIdentifier("b"), new JsIdentifier("Name")))
                )
            ));
            "['a',\"b\",`c`]".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsArrayExpression(new JsLiteral("a"),new JsLiteral("b"),new JsTemplateLiteral("c"))));
            " [ 'a' , \"b\"  , 'c' ] ".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsArrayExpression(new JsLiteral("a"),new JsLiteral("b"),new JsLiteral("c"))));
            "[ {a: 1}, {b: 2} ]".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsArrayExpression(
                    new JsObjectExpression(
                        new JsProperty(new JsIdentifier("a"), new JsLiteral(1))
                    ),
                    new JsObjectExpression(
                        new JsProperty(new JsIdentifier("b"), new JsLiteral(2))
                    )
                )
            ));
            "[ {a: { 'aa': [1,2,3] } }, { b: [a,b,c] }, 3, true, null ]".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsArrayExpression(
                    new JsObjectExpression(
                        new JsProperty(
                            new JsIdentifier("a"), 
                            new JsObjectExpression(
                                new JsProperty(
                                    new JsLiteral("aa"), 
                                    new JsArrayExpression(new JsLiteral(1),new JsLiteral(2),new JsLiteral(3))
                                )
                            )
                        )                        
                    ),
                    new JsObjectExpression(
                        new JsProperty(
                            new JsIdentifier("b"), 
                            new JsArrayExpression(new JsIdentifier("a"),new JsIdentifier("b"),new JsIdentifier("c"))
                        )                        
                    ),
                    new JsLiteral(3),
                    new JsLiteral(true),
                    JsNull.Value
                )
            ));
            "{ k:'v', data: { id: 1, name: 'foo' }, k2: 'v2', k3: 'v3' }".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsObjectExpression(
                    new JsProperty(new JsIdentifier("k"), new JsLiteral("v")),
                    new JsProperty(
                        new JsIdentifier("data"), 
                        new JsObjectExpression(
                            new JsProperty(new JsIdentifier("id"), new JsLiteral(1)),
                            new JsProperty(new JsIdentifier("name"), new JsLiteral("foo"))
                        )
                    ),
                    new JsProperty(new JsIdentifier("k2"), new JsLiteral("v2")),
                    new JsProperty(new JsIdentifier("k3"), new JsLiteral("v3"))
                )
            ));
            "[{name:'Alice', score:50}, {name: 'Bob', score:40}, {name:'Cathy', score:45}]".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsArrayExpression(
                    new JsObjectExpression(
                        new JsProperty(new JsIdentifier("name"), new JsLiteral("Alice")),
                        new JsProperty(new JsIdentifier("score"), new JsLiteral(50))
                    ),
                    new JsObjectExpression(
                        new JsProperty(new JsIdentifier("name"), new JsLiteral("Bob")),
                        new JsProperty(new JsIdentifier("score"), new JsLiteral(40))
                    ),
                    new JsObjectExpression(
                        new JsProperty(new JsIdentifier("name"), new JsLiteral("Cathy")),
                        new JsProperty(new JsIdentifier("score"), new JsLiteral(45))
                    )
                )
            ));
        }

        [Test]
        public void Can_parse_templates_within_literals()
        {
            JsToken token;

            "'<li>{{it}}</li>'".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLiteral("<li>{{it}}</li>")));

            var fragments = ScriptTemplateUtils.ParseTemplate("<ul>{{ '<li>{{it}}</li>' }}</ul>");
            Assert.That(fragments.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_parse_method_binding_expressions()
        {
            JsToken token;

            "if(OR(gt(1,2),lt(3,4)))".ParseJsExpression(out token);
            
            Assert.That(token, Is.EqualTo(
                new JsCallExpression(
                    new JsIdentifier("if"),
                    new JsCallExpression(
                        new JsIdentifier("OR"),
                        new JsCallExpression(
                            new JsIdentifier("gt"),
                            new JsLiteral(1),
                            new JsLiteral(2)
                        ),
                        new JsCallExpression(
                            new JsIdentifier("lt"),
                            new JsLiteral(3),
                            new JsLiteral(4)
                        )
                    )
                )
            ));
            

            @"
            if (
                OR (
                    gt ( 1 , 2 ) ,
                    lt ( 3 , 4 )
                )
            )".ParseJsExpression(out token);

            Assert.That(token, Is.EqualTo(
                new JsCallExpression(
                    new JsIdentifier("if"),
                    new JsCallExpression(
                        new JsIdentifier("OR"),
                        new JsCallExpression(
                            new JsIdentifier("gt"),
                            new JsLiteral(1),
                            new JsLiteral(2)
                        ),
                        new JsCallExpression(
                            new JsIdentifier("lt"),
                            new JsLiteral(3),
                            new JsLiteral(4)
                        )
                    )
                )
            ));
        }

        [Test]
        public void Does_support_shorthand_object_initializers()
        {
            "{key}".ParseJsExpression(out var token);
            Assert.That(token, Is.EqualTo(
                new JsObjectExpression(
                    new JsProperty(new JsIdentifier("key"), new JsIdentifier("key"), shorthand:true)
                )
            ));
            "{ key }".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsObjectExpression(
                    new JsProperty(new JsIdentifier("key"), new JsIdentifier("key"), shorthand:true)
                )
            ));
            "{ map : { key , foo: 'bar' , qux } }".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsObjectExpression(
                    new JsProperty(
                        new JsIdentifier("map"), 
                        new JsObjectExpression(
                            new JsProperty(new JsIdentifier("key"), new JsIdentifier("key"), shorthand:true),
                            new JsProperty(new JsIdentifier("foo"), new JsLiteral("bar")),
                            new JsProperty(new JsIdentifier("qux"), new JsIdentifier("qux"), shorthand:true)
                        )
                    )
                )
            ));
        }

        [Test]
        public void Does_preserve_new_lines()
        {
            JsToken token;

            "'a\n'".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(new JsLiteral("a\n")));
        }

        [Test]
        public void Can_parse_boolean_logic_expressions()
        {
            JsToken token;

            "it.Id == 0".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsBinaryExpression(
                    new JsMemberExpression(new JsIdentifier("it"), new JsIdentifier("Id")),
                    JsEquals.Operator,
                    new JsLiteral(0)
                )
            ));

            var hold = ScriptConfig.AllowAssignmentExpressions;
            ScriptConfig.AllowAssignmentExpressions = false;

            "it.Id = 0".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsBinaryExpression(
                    new JsMemberExpression(new JsIdentifier("it"), new JsIdentifier("Id")),
                    JsEquals.Operator,
                    new JsLiteral(0)
                )
            ));
            
            ScriptConfig.AllowAssignmentExpressions = hold;
        }

        [Test]
        public void Can_parse_assignment_expression()
        {
            JsToken token;

            "id = 0".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsAssignmentExpression(
                    new JsIdentifier("id"),
                    JsAssignment.Operator,
                    new JsLiteral(0)
                )
            ));

            "global.id = 0".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsAssignmentExpression(
                    new JsMemberExpression(new JsIdentifier("global"), new JsIdentifier("id")),
                    JsAssignment.Operator,
                    new JsLiteral(0)
                )
            ));
        }

        [Test]
        public void Can_parse_variable_declarations()
        {
            JsToken token;

            "var id = 0".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsVariableDeclaration(
                    JsVariableDeclarationKind.Var,
                    new JsDeclaration(new JsIdentifier("id"), new JsLiteral(0))
                )
            ));

            "var id = 0;".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsVariableDeclaration(
                    JsVariableDeclarationKind.Var,
                    new JsDeclaration(new JsIdentifier("id"), new JsLiteral(0))
                )
            ));

            "let a = 1 + 2, b = 3 * 4, c, d = 'D'".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsVariableDeclaration(
                    JsVariableDeclarationKind.Let,
                    new JsDeclaration(new JsIdentifier("a"), 
                        new JsBinaryExpression(new JsLiteral(1), JsAddition.Operator, new JsLiteral(2) ))
                    ,new JsDeclaration(new JsIdentifier("b"), 
                        new JsBinaryExpression(new JsLiteral(3), JsMultiplication.Operator, new JsLiteral(4) ))
                    ,new JsDeclaration(new JsIdentifier("c"), null) 
                    ,new JsDeclaration(new JsIdentifier("d"), new JsLiteral("D") ))
                )
            );

            "const c = [1]".ParseJsExpression(out token);
            Assert.That(token, Is.EqualTo(
                new JsVariableDeclaration(
                    JsVariableDeclarationKind.Const,
                    new JsDeclaration(new JsIdentifier("c"), 
                        new JsArrayExpression(new JsLiteral(1)))
                )
            ));
        }

        [Test]
        public void Can_execute_variable_declarations()
        {
            var context = new ScriptContext().Init();
            Assert.That(context.RenderScript("{{var a = 1}}{{a}}"), Is.EqualTo("1"));
            Assert.That(context.RenderScript("{{let a = 1, b = 1 + 2}}{{b}}"), Is.EqualTo("3"));
            Assert.That(context.RenderScript("{{const a = 1, b = 1 + 2, c}}{{b}}"), Is.EqualTo("3"));
            Assert.That(context.RenderScript("{{var a = 1, b = 1 + 2, c}}{{c}}"), Is.EqualTo(""));
            Assert.That(context.RenderScript("{{let a = 1, b = 1 + 2,c,d='A'}}{{d}}"), Is.EqualTo("A"));
            Assert.That(context.RenderScript("{{var a=1, b=1+2, c, d='A'}}{{d}}"), Is.EqualTo("A"));
            
            var expr = JS.expression("var a=1, b = 1 + 2, c, d='A'");
            var str = expr.ToJsAstString();
        }

        [Test]
        public void Can_use_cleaner_whitespace_sensitive_syntax_for_string_arguments()
        {
            var fragments1 = ScriptTemplateUtils.ParseTemplate(
                @"{{ 
products 
  |> where: it.UnitsInStock = 0 
  |> select: { it.productName |> raw } is sold out!\n 
}}");
            
            var fragments2 = ScriptTemplateUtils.ParseTemplate(
            @"{{ products 
                 |> where: it.UnitsInStock = 0 
                 |> select: { it.productName |> raw } is sold out!\n }}");
            
            // i.e. is rewritten and is equivalent to:
            var fragments3 = ScriptTemplateUtils.ParseTemplate(
                @"{{ products |> where(′it.UnitsInStock = 0′) |> select(′{{ it.productName |> raw }} is sold out!\n′)}}");
            Assert.That(fragments3.Count, Is.EqualTo(1));
            
            Assert.That(fragments1.Count, Is.EqualTo(1));
            var varFragment1 = fragments1[0] as PageVariableFragment;
            Assert.That(varFragment1.FilterExpressions[0].Name, Is.EqualTo("where"));
            Assert.That(varFragment1.FilterExpressions[0].Arguments.Length, Is.EqualTo(1));
            Assert.That(varFragment1.FilterExpressions[0].Arguments[0], Is.EqualTo(
                new JsLiteral("it.UnitsInStock = 0")
            ));
            Assert.That(varFragment1.FilterExpressions[1].Name, Is.EqualTo("select"));
            Assert.That(varFragment1.FilterExpressions[1].Arguments.Length, Is.EqualTo(1));
            Assert.That(varFragment1.FilterExpressions[1].Arguments[0], Is.EqualTo(
                new JsLiteral("{{ it.productName |> raw }} is sold out!\\n")
            ));

            foreach (var fragments in new[]{ fragments2, fragments3 })
            {
                var varFragment = fragments[0] as PageVariableFragment;
                Assert.That(varFragment.FilterExpressions[0].Name, Is.EqualTo(varFragment1.FilterExpressions[0].Name));
                Assert.That(varFragment.FilterExpressions[0].Arguments.Length, Is.EqualTo(varFragment1.FilterExpressions[0].Arguments.Length));
                Assert.That(varFragment.FilterExpressions[0].Arguments[0], Is.EqualTo(varFragment1.FilterExpressions[0].Arguments[0]));
                Assert.That(varFragment.FilterExpressions[1].Name, Is.EqualTo(varFragment1.FilterExpressions[1].Name));
                Assert.That(varFragment.FilterExpressions[1].Arguments.Length, Is.EqualTo(varFragment1.FilterExpressions[1].Arguments.Length));
                Assert.That(varFragment.FilterExpressions[1].Arguments[0], Is.EqualTo(varFragment1.FilterExpressions[1].Arguments[0]));
            }
        }

        [Test]
        public void Can_parse_pages_starting_with_values()
        {
            var fragments = ScriptTemplateUtils.ParseTemplate(
                @"{{ [c.CustomerId, o.OrderId, o.OrderDate] |> jsv }}\n");

            var varFragment = (PageVariableFragment) fragments[0];
            Assert.That(varFragment.Expression, Is.EqualTo(new JsArrayExpression(
                new JsMemberExpression(new JsIdentifier("c"), new JsIdentifier("CustomerId")),
                new JsMemberExpression(new JsIdentifier("o"), new JsIdentifier("OrderId")),
                new JsMemberExpression(new JsIdentifier("o"), new JsIdentifier("OrderDate"))
            )));
            
            Assert.That(varFragment.OriginalText.ToString(), Is.EqualTo("{{ [c.CustomerId, o.OrderId, o.OrderDate] |> jsv }}"));
            
            var newLine = (PageStringFragment) fragments[1];
            Assert.That(newLine.Value.ToString(), Is.EqualTo("\\n"));
        }

        [Test]
        public void Can_parse_pages_starting_with_values_newLine()
        {
            var context = new ScriptContext().Init();
            var page = context.OneTimePage("{{ [c.CustomerId, o.OrderId, o.OrderDate] |> jsv }}\n");
            var fragments = page.PageFragments;
            
//            var fragments = TemplatePageUtils.ParseTemplatePage(
//                "{{ [c.CustomerId, o.OrderId, o.OrderDate] |> jsv }}\n");

            var varFragment = (PageVariableFragment) fragments[0];
            Assert.That(varFragment.Expression, Is.EqualTo(new JsArrayExpression(
                new JsMemberExpression(new JsIdentifier("c"), new JsIdentifier("CustomerId")),
                new JsMemberExpression(new JsIdentifier("o"), new JsIdentifier("OrderId")),
                new JsMemberExpression(new JsIdentifier("o"), new JsIdentifier("OrderDate"))
            )));
            
            var newLine = (PageStringFragment) fragments[1];
            Assert.That(newLine.Value.ToString(), Is.EqualTo("\n"));
        }

        [Test]
        public void Can_detect_invalid_syntax()
        {
            try
            {
                var fragments = ScriptTemplateUtils.ParseTemplate("{{ arg |> filter(' 1) }}");
                Assert.Fail("should throw");
            }
            catch (ArgumentException e)
            {
                e.Message.Print();
            }

            try
            {
                var fragments = ScriptTemplateUtils.ParseTemplate("square = {{ 'square-partial |> partial({ ten }) }}");
                Assert.Fail("should throw");
            }
            catch (ArgumentException e)
            {
                e.Message.Print();
            }

            try
            {
                var fragments = ScriptTemplateUtils.ParseTemplate("{{ arg |> filter({ unterminated:1) }}");
                Assert.Fail("should throw");
            }
            catch (ArgumentException e)
            {
                e.Message.Print();
            }

            try
            {
                var fragments = ScriptTemplateUtils.ParseTemplate("{{ arg |> filter([ 1) }}");
                Assert.Fail("should throw");
            }
            catch (ArgumentException e)
            {
                e.Message.Print();
            }
            
        }

        [Test]
        public void Does_remove_new_line_between_var_literals()
        {
            var fragments = ScriptTemplateUtils.ParseTemplate("{{ 'foo' |> assignTo: bar }}\n{{ bar }}");
            Assert.That(fragments.Count, Is.EqualTo(2));
            fragments = ScriptTemplateUtils.ParseTemplate("{{ 'foo' |> assignTo: bar }}\r\n{{ bar }}");
            Assert.That(fragments.Count, Is.EqualTo(2));

            fragments = ScriptTemplateUtils.ParseTemplate("{{ ['foo'] |> do: assign('bar', it) }}\n{{ bar }}");
            Assert.That(fragments.Count, Is.EqualTo(2));
            fragments = ScriptTemplateUtils.ParseTemplate("{{ do: assign('bar', 'foo') }}\n{{ bar }}");
            Assert.That(fragments.Count, Is.EqualTo(2));
            fragments = ScriptTemplateUtils.ParseTemplate("{{ 10 |> times |> do: assign('bar', 'foo') }}\n{{ bar }}");
            Assert.That(fragments.Count, Is.EqualTo(2));
            fragments = ScriptTemplateUtils.ParseTemplate("{{ 10 |> times |> do: assign('bar', 'foo') }}\nbar");
            Assert.That(fragments.Count, Is.EqualTo(2));
            var stringFragment = (PageStringFragment) fragments[1];
            Assert.That(stringFragment.Value.ToString(), Is.EqualTo("bar"));
        }

        [Test]
        public void Can_parse_empty_arguments()
        {
            JsToken token;
            
            "fn()".ParseJsExpression(out token);
            Assert.That(((JsCallExpression)token).Name, Is.EqualTo("fn"));
            "fn({})".ParseJsExpression(out token);
            Assert.That(((JsCallExpression)token).Arguments.Length, Is.EqualTo(1));
            "fn({ })".ParseJsExpression(out token);
            Assert.That(((JsCallExpression)token).Arguments.Length, Is.EqualTo(1));
            "fn({  })".ParseJsExpression(out token);
            Assert.That(((JsCallExpression)token).Arguments.Length, Is.EqualTo(1));
        }


    }
}