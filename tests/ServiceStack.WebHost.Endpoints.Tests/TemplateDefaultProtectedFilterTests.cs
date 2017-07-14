using System;
using System.Reflection;
using Funq;
using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Testing;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class TemplateDefaultProtectedFilterTests
    {
        [Test]
        public void Does_not_include_protected_filters_by_default()
        {
            var context = new TemplatePagesContext().Init();
            context.VirtualFiles.WriteFile("index.txt", "file contents");

            Assert.That(new PageResult(context.OneTimePage("{{ 'index.txt' | includeFile }}")).Result, 
                Is.EqualTo("{{ 'index.txt' | includeFile }}"));

            using (new BasicAppHost().Init())
            {
                var feature = new TemplatePagesFeature().Init();
                feature.VirtualFiles.WriteFile("index.txt", "file contents");

                Assert.That(new PageResult(context.OneTimePage("{{ 'index.txt' | includeFile }}")).Result, 
                    Is.EqualTo("{{ 'index.txt' | includeFile }}"));
            }
        }

        [Test]
        public void Can_use_protected_includeFiles_in_context_or_PageResult()
        {
            var context = new TemplatePagesContext
            {
                TemplateFilters = { new TemplateProtectedFilters() }
            }.Init();
            context.VirtualFiles.WriteFile("index.txt", "file contents");
            
            Assert.That(new PageResult(context.OneTimePage("{{ 'index.txt' | includeFile }}")).Result, 
                Is.EqualTo("file contents"));
        }
    }
}