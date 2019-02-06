using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateCustomFilterTests
    {
        [Flags]
        public enum Options
        {
            None = 0,
            Option1 = 1,
            Option2 = 1 << 1,
            Option4 = 1 << 2,
        }

        public class EnumFilter : TemplateFilter
        {
            public bool hasOptionsFlag(Options source, Options value) => source.HasFlag(value);
        }

        [Test]
        public void Can_access_flag_enums()
        {
            var context = new TemplateContext
            {
                TemplateFilters = {new EnumFilter()},
                Args =
                {
                    ["options0"] = Options.None,
                    ["options1"] = Options.Option1,
                    ["options2"] = Options.Option2,
                    ["options4"] = Options.Option4,
                    ["options3"] = (Options.Option1 | Options.Option2)
                }
            }.Init();

            Assert.That(context.EvaluateTemplate(@"{{ hasOptionsFlag(options3, 'Option1') }}"),
                Is.EqualTo("True"));

            Assert.That(context.EvaluateTemplate(@"{{ hasFlag(options3, 'Option1') }},{{ hasFlag(options3, 1) }},{{ hasFlag(options3, options1) }}"),
                Is.EqualTo("True,True,True"));
            Assert.That(context.EvaluateTemplate(@"{{ hasFlag(options3, 'Option4') }},{{ hasFlag(options3, 4) }},{{ hasFlag(options3, options4) }}"),
                Is.EqualTo("False,False,False"));
            
            Assert.That(context.EvaluateTemplate(@"{{ isEnum(options1, 'Option1') }},{{ isEnum(options1, 1) }},{{ isEnum(options1, options1) }}"),
                Is.EqualTo("True,True,True"));
            Assert.That(context.EvaluateTemplate(@"{{ isEnum(options3, 'Option1') }},{{ isEnum(options3, 1) }},{{ isEnum(options3, options1) }}"),
                Is.EqualTo("False,False,False"));
        }
    }
}