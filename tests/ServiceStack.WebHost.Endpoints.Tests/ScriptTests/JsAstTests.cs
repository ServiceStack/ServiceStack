using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class JsAstTests
    {
        [Test]
        public void Can_use_ToJsAst_to_generate_Esprima_AST()
        {
            JsToken token = JS.expression("{ key: a.prop == 1 ? b < 2 : c > 3 }");

//            "{ key: a.prop == 1 ? b < 2 : c > 3 }".ParseJsExpression(out token);            

            Dictionary<string, object> ast = token.ToJsAst();

            ast.ToJson().IndentJson().Print();

            var expected = new Dictionary<string, object> {
                ["type"] = "ObjectExpression",
                ["properties"] = new List<object> {
                    new Dictionary<string, object> {
                        ["type"] = "Property",
                        ["key"] = new Dictionary<string, object> {
                            ["type"] = "Identifier",
                            ["name"] = "key",
                        },
                        ["computed"] = false,
                        ["value"] = new Dictionary<string, object> {
                            ["type"] = "ConditionalExpression",
                            ["test"] = new Dictionary<string, object> {
                                ["type"] = "BinaryExpression",
                                ["operator"] = "==",
                                ["left"] = new Dictionary<string, object> {
                                    ["type"] = "MemberExpression",
                                    ["computed"] = false,
                                    ["object"] = new Dictionary<string, object> {
                                        ["type"] = "Identifier",
                                        ["name"] = "a",
                                    },
                                    ["property"] = new Dictionary<string, object> {
                                        ["type"] = "Identifier",
                                        ["name"] = "prop",
                                    }
                                },
                                ["right"] = new Dictionary<string, object> {
                                    ["type"] = "Literal",
                                    ["value"] = 1,
                                    ["raw"] = "1",
                                },
                            },
                            ["consequent"] = new Dictionary<string, object> {
                                ["type"] = "BinaryExpression",
                                ["operator"] = "<",
                                ["left"] = new Dictionary<string, object> {
                                    ["type"] = "Identifier",
                                    ["name"] = "b",
                                },
                                ["right"] = new Dictionary<string, object> {
                                    ["type"] = "Literal",
                                    ["value"] = 2,
                                    ["raw"] = "2",
                                },
                            },
                            ["alternate"] = new Dictionary<string, object> {
                                ["type"] = "BinaryExpression",
                                ["operator"] = ">",
                                ["left"] = new Dictionary<string, object> {
                                    ["type"] = "Identifier",
                                    ["name"] = "c",
                                },
                                ["right"] = new Dictionary<string, object> {
                                    ["type"] = "Literal",
                                    ["value"] = 3,
                                    ["raw"] = "3",
                                },
                            },
                        },
                        ["kind"] = "init",
                        ["method"] = false,
                        ["shorthand"] = false,
                    }
                }
            };

            "Expected: ".Print();
            expected.ToJson().IndentJson().Print();

            Assert.That(ast, Is.EqualTo(expected));
        }

        [Test]
        public void Does_support_ast_with_null()
        {
            JsToken token;
            
            "a > b ? a : null".ParseJsExpression(out token);
            
            var ast = token.ToJsAst();

            token.ToJsAstString().Print();

            var expected = new Dictionary<string, object> {
                ["type"] = "ConditionalExpression",
                ["test"] = new Dictionary<string, object> {
                    ["type"] = "BinaryExpression",
                    ["operator"] = ">",
                    ["left"] = new Dictionary<string, object> {
                        ["type"] = "Identifier",
                        ["name"] = "a",
                    },
                    ["right"] = new Dictionary<string, object> {
                        ["type"] = "Identifier",
                        ["name"] = "b",
                    },
                },
                ["consequent"] = new Dictionary<string, object> {
                    ["type"] = "Identifier",
                    ["name"] = "a",
                },
                ["alternate"] = new Dictionary<string, object> {
                    ["type"] = "Literal",
                    ["value"] = null,
                    ["raw"] = "null",
                },
            };

            Assert.That(ast, Is.EqualTo(expected));
        }
    }
}