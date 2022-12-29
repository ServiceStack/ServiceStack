using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.IO;
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
                textFiles: new Dictionary<string, string> {
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
 
        [Test]
        public async Task Does_FetchAllTruncatedFilesAsync()
        {
            var gistWithTruncatedFiles = "6f3484ef287c85b118ee6ca3262c1534";
            var vfs = new GistVirtualFiles(gistWithTruncatedFiles);
            var gist = await vfs.GetGistAsync();
            Assert.That(gist.Files.Values.Any(x => x.Truncated && string.IsNullOrEmpty(x.Content)));

            await vfs.LoadAllTruncatedFilesAsync();

            Assert.That(!gist.Files.Values.Any(x => x.Truncated && string.IsNullOrEmpty(x.Content)));
        }

        [Test]
        public void Can_GetSourceTagZipUrl()
        {
            var user = "NetCoreTemplates";
            var repo = "web";
            var tag = "v28";
            
            var gateway = new GitHubGateway(AccessToken);

            var zipUrlForTag = gateway.GetSourceZipUrl(user, repo, tag);
            
            Assert.That(zipUrlForTag, Is.EqualTo("https://github.com/NetCoreTemplates/web/archive/refs/tags/v28.zip"));
        }
        
        [Test]
        public void Can_GetSourceTagZipUrl_InvalidTag()
        {
            var user = "NetCoreTemplates";
            var repo = "web";
            var tag = "invalid-tag";
            
            var gateway = new GitHubGateway(AccessToken);

            var exception = Assert.Throws<WebException>(() =>
            {
                var zipBytes = gateway.GetSourceZipUrl(user, repo, tag).GetBytesFromUrl();
            });
            
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Message, Does.Contain("(404) Not Found"));
        }
    }
}