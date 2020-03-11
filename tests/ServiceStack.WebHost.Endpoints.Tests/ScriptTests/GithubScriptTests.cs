using System.IO;
using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    [Ignore("Integration Tests")]
    public class GithubScriptTests
    {
        public ScriptContext CreateScriptContext()
        {
            return new ScriptContext {
                Plugins = { new GitHubPlugin() },
                ScriptMethods = { new InfoScripts(), new ProtectedScripts(), },
            };
        }
        
        [Test]
        public void Can_write_and_read_gist_files()
        {
            var context = CreateScriptContext().Init();

            var output = context.EvaluateScript(@"
```code
githubGateway('GITHUB_GIST_TOKEN'.envVariable()) |> to => gateway

{{ gateway.githubCreateGist('Hello World Examples', {
     'hello_world_ruby.txt':   'Run `ruby hello_world.rb` to print Hello World',
     'hello_world_python.txt': 'Run `python hello_world.py` to print Hello World',
   })
   |> to => newGist }}

{ ...newGist, Files: null, Owner: null } |> textDump({ caption: 'new gist' })
newGist.Owner |> textDump({ caption: 'new gist owner' })
newGist.Files |> toList |> map(x => x.Value.textDump({ caption: x.Key })) |> join('\n')

gateway.githubGist(newGist.Id) |> to => gist
{ ...gist, Files: null, Owner: null } |> textDump({ caption: 'gist' })
gist.Files |> toList |> map(x => x.Value.textDump({ caption: x.Key })) |> join('\n')
```");
 
            output.Print();
        }

        [Test]
        public void Display_Gist()
        {
            var context = CreateScriptContext().Init();
            context.Args["gistId"] = "4c5d95ec4b2594b4cdd238987fe7a15a";
            
            var output = context.EvaluateScript(@"
```code
githubGateway('GITHUB_GIST_TOKEN'.envVariable()) |> to => gateway

gateway.githubGist(gistId) |> to => gist

{ ...gist, Files: null, Owner: null } |> textDump({ caption: 'gist' })

`### Gist Files`
#each file in gist.Files.Keys
    gist.Files[file] |> textDump({ caption: file })
/each
```");
 
            output.Print();
        }
    }
}