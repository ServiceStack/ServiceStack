using System.Collections.Generic;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class SharpApiTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(SharpPageTests), typeof(SharpPagesService).Assembly) { }

            public override void Configure(Container container)
            {
                Plugins.Add(new SharpPagesFeature());

                AfterInitCallbacks.Add(host => {
                    var memFs = VirtualFileSources.GetMemoryVirtualFiles();
                    foreach (var entry in HtmlFiles)
                    {
                        memFs.AppendFile(entry.Key, entry.Value);
                    }
                });
            }

            private static readonly Dictionary<string, string> HtmlFiles = new Dictionary<string, string> {
                {
                    "_layout.html",
                    "<html><head><title>{{ title }}</title></head><body id='layout'>{{ page }}</body></html>"
                }, {
                    "preview.html",
                    @"API /preview
* content : string - #Script to evaluate

{{ qs.content  |> evalTemplate({use:{plugins:'MarkdownScriptPlugin'}}) |> assignTo:response }}
{{ response |> return({ contentType:'text/plain' }) }}"
                },
            };
        }

        private readonly ServiceStackHost appHost;
        public SharpApiTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown] public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Does_evaluate_SharpAPi()
        {
            var output = Config.ListeningOn.CombineWith("preview.html")
                .AddQueryParam("content", "{{10|times|select:{pow(index,2)},}}")
                .GetStringFromUrl(accept: MimeTypes.Html);

            Assert.That(output.NormalizeNewLines(), Is.EqualTo("0,1,4,9,16,25,36,49,64,81,"));
        }

        [Test]
        public void Does_evaluateTemplate_with_no_content_returns_empty()
        {
            var output = Config.ListeningOn.CombineWith("preview.html")
                .GetStringFromUrl(accept: MimeTypes.Html);

            Assert.That(output.NormalizeNewLines(), Is.Empty);
        }

    }
}