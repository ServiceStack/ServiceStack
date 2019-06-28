using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Ignore("Integration Tests")]
    public class GithubGatewayTests
    {
        public static readonly string GistId = "67bc8f75273a29a1ba0609675b8ed1ae";
        public static readonly string AccessToken = Environment.GetEnvironmentVariable("GITHUB_GIST_TOKEN");

        [Test]
        public void Can_create_gist()
        {
            var gateway = new GitHubGateway(AccessToken);

            var gist = gateway.CreateGithubGist(
                description: "Hello World Examples",
                isPublic: true,
                files: new Dictionary<string, string> {
                    ["hello_world_ruby.txt"] = "Run `ruby hello_world.rb` to print Hello World",
                    ["hello_world_python.txt"] = "Run `python hello_world.py` to print Hello World",
                });
            
            gist.PrintDump();

            Assert.That(gist.Owner.Login, Is.EqualTo("gistlyn"));
            Assert.That(gist.Owner.Url, Is.EqualTo("https://api.github.com/users/gistlyn"));
            Assert.That(gist.Owner.Html_Url, Is.EqualTo("https://github.com/gistlyn"));
            
            var file = gist.Files["hello_world_ruby.txt"];
            Assert.That(file.Filename, Is.EqualTo("hello_world_ruby.txt"));
            Assert.That(file.Type, Is.EqualTo("text/plain"));
            Assert.That(file.Language, Is.EqualTo("Text"));
            Assert.That(file.Raw_Url, Does.EndWith("/hello_world_ruby.txt"));
            Assert.That(file.Size, Is.GreaterThan(0));
            Assert.That(file.Content, Does.Contain("Run `ruby hello_world.rb` to print Hello World"));
            
            file = gist.Files["hello_world_python.txt"];
            Assert.That(file.Filename, Is.EqualTo("hello_world_python.txt"));
            Assert.That(file.Type, Is.EqualTo("text/plain"));
            Assert.That(file.Language, Is.EqualTo("Text"));
            Assert.That(file.Raw_Url, Does.EndWith("/hello_world_python.txt"));
            Assert.That(file.Size, Is.GreaterThan(0));
            Assert.That(file.Content, Does.Contain("Run `python hello_world.py` to print Hello World"));
        }
        
        [Test]
        public void Can_download_public_gist()
        {
            var gateway = new GitHubGateway();
            var result = gateway.GetGist(GistId);
            var gist = (GithubGist)result;
            Assert.That(gist.Owner.Login, Is.EqualTo("gistlyn"));
            Assert.That(gist.Owner.Url, Is.EqualTo("https://api.github.com/users/gistlyn"));
            Assert.That(gist.Owner.Html_Url, Is.EqualTo("https://github.com/gistlyn"));

            var file = gist.Files["main.cs"];
            Assert.That(file.Filename, Is.EqualTo("main.cs"));
            Assert.That(file.Type, Is.EqualTo("text/plain"));
            Assert.That(file.Language, Is.EqualTo("C#"));
            Assert.That(file.Raw_Url, Does.EndWith("/main.cs"));
            Assert.That(file.Size, Is.GreaterThan(0));
            Assert.That(file.Content, Does.Contain("Hello, {name}!"));
        }

        [Test]
        public async Task Can_download_public_gist_Async()
        {
            var gateway = new GitHubGateway();
            var result = await gateway.GetGistAsync(GistId);
            var gist = (GithubGist)result;
            Assert.That(gist.Owner.Login, Is.EqualTo("gistlyn"));
            Assert.That(gist.Owner.Url, Is.EqualTo("https://api.github.com/users/gistlyn"));
            Assert.That(gist.Owner.Html_Url, Is.EqualTo("https://github.com/gistlyn"));

            var file = gist.Files["main.cs"];
            Assert.That(file.Filename, Is.EqualTo("main.cs"));
            Assert.That(file.Type, Is.EqualTo("text/plain"));
            Assert.That(file.Language, Is.EqualTo("C#"));
            Assert.That(file.Raw_Url, Does.EndWith("/main.cs"));
            Assert.That(file.Size, Is.GreaterThan(0));
            Assert.That(file.Content, Does.Contain("Hello, {name}!"));
        }

        [Test]
        public void Can_add_and_delete_gist_file()
        {
            var gateway = new GitHubGateway(AccessToken);

            var newFile = "new.txt";
            gateway.WriteGistFile(GistId, newFile, "this is a new file");
            
            var gist = gateway.GetGist(GistId);
            var file = gist.Files[newFile];
            Assert.That(file.Filename, Is.EqualTo(newFile));
            Assert.That(file.Type, Is.EqualTo("text/plain"));
            Assert.That(file.Content, Is.EqualTo("this is a new file"));

            gateway.DeleteGistFiles(GistId, newFile);
            
            gist = gateway.GetGist(GistId);
            Assert.That(gist.Files.TryGetValue(newFile, out file), Is.False);
        }
    }
}