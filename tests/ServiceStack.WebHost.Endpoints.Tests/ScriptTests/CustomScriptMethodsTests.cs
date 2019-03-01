using System;
using NUnit.Framework;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class CustomScriptMethodsTests
    {
        [Flags]
        public enum Options
        {
            None = 0,
            Option1 = 1,
            Option2 = 1 << 1,
            Option4 = 1 << 2,
        }

        public class EnumFilter : ScriptMethods
        {
            public bool hasOptionsFlag(Options source, Options value) => source.HasFlag(value);
        }

        [Test]
        public void Can_access_flag_enums()
        {
            var context = new ScriptContext
            {
                ScriptMethods = {new EnumFilter()},
                Args =
                {
                    ["options0"] = Options.None,
                    ["options1"] = Options.Option1,
                    ["options2"] = Options.Option2,
                    ["options4"] = Options.Option4,
                    ["options3"] = (Options.Option1 | Options.Option2)
                }
            }.Init();

            Assert.That(context.EvaluateScript(@"{{ hasOptionsFlag(options3, 'Option1') }}"),
                Is.EqualTo("True"));

            Assert.That(context.EvaluateScript(@"{{ hasFlag(options3, 'Option1') }},{{ hasFlag(options3, 1) }},{{ hasFlag(options3, options1) }}"),
                Is.EqualTo("True,True,True"));
            Assert.That(context.EvaluateScript(@"{{ hasFlag(options3, 'Option4') }},{{ hasFlag(options3, 4) }},{{ hasFlag(options3, options4) }}"),
                Is.EqualTo("False,False,False"));
            
            Assert.That(context.EvaluateScript(@"{{ isEnum(options1, 'Option1') }},{{ isEnum(options1, 1) }},{{ isEnum(options1, options1) }}"),
                Is.EqualTo("True,True,True"));
            Assert.That(context.EvaluateScript(@"{{ isEnum(options3, 'Option1') }},{{ isEnum(options3, 1) }},{{ isEnum(options3, options1) }}"),
                Is.EqualTo("False,False,False"));
        }
    }
}