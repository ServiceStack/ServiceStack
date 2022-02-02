using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class TemplateLiteralsTests
    {
        [Test]
        public void Can_embed_escaped_strings_in_template_literals()
        {
            const string text = @"C:\src\dotnet-app\src\WebApp\bin\Release\net6.0\favicon.ico
C:\src\dotnet-app\src\WebApp\bin\Release\net6.0\app.exe
C:\src\dotnet-app\src\WebApp\bin\Release\net6.0\app.deps.json
C:\src\dotnet-app\src\WebApp\bin\Release\net6.0\app.runtimeconfig.json
C:\src\dotnet-app\src\WebApp\bin\Release\net6.0\app.runtimeconfig.dev.json
C:\src\dotnet-app\src\WebApp\bin\Release\net6.0\app.dll
C:\src\dotnet-app\src\WebApp\bin\Release\net6.0\app.pdb";
            
            var context = new ScriptContext {
                Args = {
                    ["file"] = text
                }
            }.Init();

            var output = context.RenderCode(@"
#each line in file.readLines() where line.contains('\\bin\\')
    line.substring(line.indexOf('\\bin\\') + 1) |> to => src
    line.lastRightPart('\\') |> to => target
    `<file src=""${src}"" target=""tools\\net6.0\\any\\${target}"" />`.raw()
/each 
");
            // output.Print();
            Assert.That(output.NormalizeNewLines(), 
                Does.StartWith("<file src=\"bin\\Release\\net6.0\\favicon.ico\" target=\"tools\\net6.0\\any\\favicon.ico\" />"));
        }

        [Test]
        public void Does_UnRaw_RawStrings_in_Template_Literals()
        {
            var context = new ScriptContext().Init();
            var output = context.RenderCode("`type: ${1.typeName()}`");
            Assert.That(output.Trim(), Is.EqualTo("type: Int32"));
        }

    }
}