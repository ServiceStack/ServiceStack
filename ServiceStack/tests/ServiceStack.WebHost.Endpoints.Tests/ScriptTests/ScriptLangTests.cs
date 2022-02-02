using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests 
{
    public class ScriptLangTests
    {
        [Test]
        public void Ignores_lang_blocks_that_are_not_languages()
        {
            var context = new ScriptContext {
                ScriptLanguages = { ScriptLisp.Language }
            }.Init();
            
            string render(string s) => context.RenderScript(s).NormalizeNewLines();
            
            Assert.That(render(@"
```code
`test`
```
{|lisp (+ 1 2) |}
"), Is.EqualTo(@"
test
3
".NormalizeNewLines()));
            
            Assert.That(render(@"
```<lang>
test
```
{|<lang> |}
"), Is.EqualTo(@"
```<lang>
test
```
{|<lang> |}
".NormalizeNewLines()));
            
        }
    }
}