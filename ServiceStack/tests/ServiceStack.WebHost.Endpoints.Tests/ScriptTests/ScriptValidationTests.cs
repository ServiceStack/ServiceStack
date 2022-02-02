using System;
using System.IO;
using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    /*
    public class DynamicValidationServices : Service
    {
        public override void OnBeforeExecute(object requestDto)
        {
            try
            {
                // check if there's a script for this service
                var script = TryResolve<IAppSettings>().GetString($"script.{requestDto.GetType().Name}.validation");
                if (script == null)
                    return;
                
                var context = HostContext.GetPlugin<SharpPagesFeature>();
                
                var pageResult = new PageResult(context.OneTimePage(script)) {
                    Model = requestDto
                };
                pageResult.WriteToAsync(Stream.Null);
            }
            catch (ScriptException e)
            {
                throw e.InnerException ?? e;
            }
        }
    }
     * 
     */
    
    public class ScriptValidationTests
    {
        public class Person
        {
            public string Name { get; set; }
            public int? Age { get; set; }
        }
        
        [Test]
        public void Can_validate_person_in_code()
        {
            var context = new ScriptContext().Init();

            var code = @"
['Name','Age'] |> to => requiredProps
#each requiredProps
    #if !model[it]
        it.throwArgumentNullException()
    /if
/each

(Age < 13) |> ifThrowArgumentException('Must be 13 or over', 'Age')
";

            try
            {
                var pageResult = new PageResult(context.CodeBlock(code)) {
                    Model = new Person()
                };
                pageResult.RenderToStream(Stream.Null);
                Assert.Fail("Should throw");
            }
            catch (ScriptException e)
            {
                if (!(e.InnerException is ArgumentNullException ne))
                    throw;
                
                Assert.That(ne.ParamName, Is.EqualTo(nameof(Person.Name)));
            }

            try
            {
                var pageResult = new PageResult(context.CodeBlock(code)) {
                    Model = new Person { Name = "A" }
                };
                pageResult.RenderToStream(Stream.Null);
                Assert.Fail("Should throw");
            }
            catch (ScriptException e)
            {
                if (!(e.InnerException is ArgumentNullException ne))
                    throw;
                
                Assert.That(ne.ParamName, Is.EqualTo(nameof(Person.Age)));
            }

            try
            {
                var pageResult = new PageResult(context.CodeBlock(code)) {
                    Model = new Person { Name = "A", Age = 1 }
                };
                pageResult.RenderToStream(Stream.Null);
                Assert.Fail("Should throw");
            }
            catch (ScriptException e)
            {
                if (!(e.InnerException is ArgumentException ae))
                    throw;
                
                Assert.That(ae.Message.Replace("\r",""), 
                    Does.StartWith("Must be 13 or over"));
            }
        }
    }
}