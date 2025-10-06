#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.Text;

namespace ServiceStack.Extensions.Tests;

[Ignore("TODO Fix AI Test in CI"), TestFixture]
public class AiChatTests
{
    public AiChatTests()
    {
        var app = AiChatIntegrationTests.ConfigureAppHost(configure: feature =>
        {
            feature.Variables = new()
            {
                ["OPENROUTER_FREE_API_KEY"] = "fake-key",
                ["GROQ_API_KEY"] = "fake-key",
                ["GOOGLE_FREE_API_KEY"] = "fake-key",
                ["CODESTRAL_API_KEY"] = "fake-key",
                ["ANTHROPIC_API_KEY"] = "fake-key",
                ["OPENAI_API_KEY"] = "fake-key",
                ["GROK_API_KEY"] = "fake-key",
                ["DASHSCOPE_API_KEY"] = "fake-key",
                ["ZAI_API_KEY"] = "fake-key",
                ["MISTRAL_API_KEY"] = "fake-key",
            };
        });
        app.StartAsync(TestsConfig.ListeningOn);
        WaitForLoadAsync(extraMs:1000).Wait();
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown() => AppHostBase.DisposeApp();

    public async Task WaitForLoadAsync(int extraMs = 0)
    {
        while (!ServiceStackHost.HasLoaded)
        {
            await Task.Delay(100);
        }

        if (extraMs > 0)
        {
            await Task.Delay(extraMs);
        }
    }

    private JsonApiClient CreateClient() => new(TestsConfig.ListeningOn)
    {
        BearerToken = ApiKeyTests.AdminKey
    };

    private async Task<JsonApiClient> CreateClientAsync()
    {
        await WaitForLoadAsync();
        return CreateClient();
    }

    [Test]
    public void All_Providers_uses_Variable_ApiKeys()
    {
        var feature = HostContext.AssertPlugin<ChatFeature>();
        foreach (var entry in feature.Providers)
        {
            var provider = entry.Value;
            if (entry.Key == "ollama")
                Assert.That(provider.ApiKey, Is.Null);
            else
                Assert.That(provider.ApiKey, Is.EqualTo("fake-key"));
        }
    }

    [Test]
    public void Can_get_all_Chat_clients()
    {
        var feature = HostContext.AssertPlugin<ChatFeature>();
        var services = HostContext.AppHost.GetApplicationServices();
        var clients = services.GetRequiredService<IChatClients>();
        
        foreach (var provider in feature.Providers.Keys)
        {
            Assert.That(clients.GetClient(provider), Is.Not.Null);
            Assert.That(clients.GetRequiredClient(provider), Is.Not.Null);
        }
        Assert.That(clients.GetClient("non-existent"), Is.Null);
        Assert.Throws<Exception>(() => clients.GetRequiredClient("non-existent"));
        
        Assert.That(clients.GetOpenAiProvider("openrouter_free"), Is.Not.Null);
        Assert.That(clients.GetGoogleProvider("google_free"), Is.Not.Null);
        Assert.That(clients.GetOllamaProvider("ollama"), Is.Not.Null);
    }
    
    [Test]
    public async Task Can_get_models()
    {
        var client = await CreateClientAsync();
        var response = await client.SendAsync(new ChatModels());
        ClientConfig.PrintSystemJson(response);
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Length, Is.GreaterThan(0));
    }

    [Test]
    public async Task Can_get_config()
    {
        var client = await CreateClientAsync();
        var response = await client.SendAsync(new ChatConfig());
        ClientConfig.IndentJson(response).Print();
    }

    [Test]
    public async Task Can_get_status()
    {
        var client = await CreateClientAsync();
        var response = await client.SendAsync(new ChatStatus());
        ClientConfig.PrintSystemJson(response);
        Assert.That(response, Is.Not.Null);
        Assert.That(response.All.Count, Is.GreaterThan(0));
        Assert.That(response.Enabled.Count, Is.GreaterThan(0));
        Assert.That(response.Disabled.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task Can_enable_and_disable_provider()
    {
        var client = await CreateClientAsync();
        var feature = HostContext.AssertPlugin<ChatFeature>();
        
        Assert.That(feature.Providers.ContainsKey("mistral"), Is.True);
        var response = await client.SendAsync(new UpdateChatProvider {
            Id = "mistral",
            Disable = true,
        });
        ClientConfig.PrintSystemJson(response);
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Enabled, Does.Not.Contain("mistral"));
        Assert.That(response.Disabled, Does.Contain("mistral"));

        Assert.That(feature.Providers.ContainsKey("mistral"), Is.False);
        response = await client.SendAsync(new UpdateChatProvider {
            Id = "mistral",
            Enable = true,
        });
        Assert.That(response.Enabled, Does.Contain("mistral"));
        Assert.That(response.Disabled, Does.Not.Contain("mistral"));
        Assert.That(feature.Providers.ContainsKey("mistral"), Is.True);
    }

    [Test]
    public async Task Can_get_static_files()
    {
        var client = await CreateClientAsync();
        string[] files =
        [
            "",
            "llms.json",
            "ui.json",
        ];
        
        foreach (var file in files)
        {
            var bytes = await client.SendAsync(new ChatStaticFile { Path = file });
            Assert.That(bytes, Is.Not.Null);
            Assert.That(bytes.Length, Is.GreaterThan(0));
            var txt = bytes.FromUtf8Bytes();
            if (file.EndsWith(".json"))
            {
                Assert.That(txt.Trim(), Does.StartWith("{"));
            }
            else if (file.Length == 0 || file.EndsWith(".html"))
            {
                Assert.That(txt.Trim(), Does.StartWith("<"));
            }
        }
    }
}