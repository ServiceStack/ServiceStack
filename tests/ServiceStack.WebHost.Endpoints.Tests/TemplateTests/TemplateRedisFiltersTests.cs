﻿using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Redis;
using ServiceStack.Templates;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateRedisFiltersTests
    {
        [Test]
        public void Can_pass_filter_by_argument_to_partial() 
        {   
            var context = new TemplateContext
            {
                TemplateFilters =
                {
                    new TemplateRedisFilters { RedisManager = new RedisManagerPool() },
                }
            }.Init();
            
            context.VirtualFiles.WriteFile("page-argument.html", "{{ 'partial-argument' | partial({ redis: redisConnection }) }}");
            context.VirtualFiles.WriteFile("partial-argument.html", "{{ redis.host }}, {{ redis.port }}");
            
            var output = new PageResult(context.GetPage("page-argument")).Result;
            
            Assert.That(output, Is.EqualTo("localhost, 6379"));
        }
    }
}