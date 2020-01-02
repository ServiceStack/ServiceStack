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
            const string text = @"C:\src\dotnet-app\src\WebApp\bin\Release\netcoreapp3.1\favicon.ico
C:\src\dotnet-app\src\WebApp\bin\Release\netcoreapp3.1\app.exe
C:\src\dotnet-app\src\WebApp\bin\Release\netcoreapp3.1\app.deps.json
C:\src\dotnet-app\src\WebApp\bin\Release\netcoreapp3.1\app.runtimeconfig.json
C:\src\dotnet-app\src\WebApp\bin\Release\netcoreapp3.1\app.runtimeconfig.dev.json
C:\src\dotnet-app\src\WebApp\bin\Release\netcoreapp3.1\app.dll
C:\src\dotnet-app\src\WebApp\bin\Release\netcoreapp3.1\app.pdb";
            
            var context = new ScriptContext {
                Args = {
                    ["file"] = text
                }
            }.Init();

            var output = context.RenderCode(@"
#each line in file.readLines() where line.contains('\\bin\\')
    line.substring(line.indexOf('\\bin\\') + 1) |> to => src
    line.lastRightPart('\\') |> to => target
    `<file src=""${src}"" target=""tools\\netcoreapp3.1\\any\\${target}"" />`.raw()
/each 
");
            // output.Print();
            Assert.That(output.NormalizeNewLines(), 
                Does.StartWith("<file src=\"bin\\Release\\netcoreapp3.1\\favicon.ico\" target=\"tools\\netcoreapp3.1\\any\\favicon.ico\" />"));
        }
    }
}