#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.Extensions.Tests;

[Ignore("Requires API Provider Keys"), TestFixture]
public class AiChatIntegrationTests
{
    internal class AppHost() : AppHostBase(nameof(AiChatIntegrationTests))
    {
        public override void Configure()
        {
            var services = this.GetApplicationServices();
            var apiKeysFeature = GetPlugin<ApiKeysFeature>();
            using var db = apiKeysFeature.OpenDb();
            apiKeysFeature.InitSchema(db);
            
            var dbFactory = services.GetRequiredService<IDbConnectionFactory>();
            apiKeysFeature.InsertAll(db, [
                new() { Key = ApiKeyTests.AnonKey },
                new() { Key = ApiKeyTests.UserKey, UserId = "89C1698D-9FD1-43B1-8C8B-C76EFA65E99B", UserName = "apiuser" },
                new() { Key = ApiKeyTests.AdminKey, UserId = "40E566F2-DD08-4432-9D9C-528B3B0CCBEE", UserName = "admin", Scopes = [Roles.Admin] },
                new() { Key = ApiKeyTests.RestrictedKey, UserId = "9FDC4B8B-04AA-42AD-80AD-803FF8530EFB", UserName = "restricted", RestrictTo = [nameof(RestrictedApiKey)] },
            ]);
            
            SetConfig(new() {
                AdminAuthSecret = ApiKeyTests.AuthSecret,
                DebugMode = true,
            });
        }
    }

    internal static WebApplication ConfigureAppHost(Action<ChatFeature>? configure = null)
    {
        var contentRootPath = "~/../../../".MapServerPath();
        
        var appDataDir = contentRootPath.CombineWith("App_Data");
        var imgPath = appDataDir.CombineWith("ubixar.webp");
        if (!File.Exists(imgPath))
            File.WriteAllBytes(imgPath, "https://ubixar.com/avatars/ub/ubixar.webp".GetBytesFromUrl());
        var audioPath = appDataDir.CombineWith("speaker.mp3");
        if (!File.Exists(audioPath))
            File.WriteAllBytes(audioPath, "https://media.servicestack.com/audio/speaker.mp3".GetBytesFromUrl());
        var pdfPath = appDataDir.CombineWith("Q1556_NASA.pdf");
        if (!File.Exists(pdfPath))
            File.WriteAllBytes(pdfPath, "https://media.servicestack.com/documents/Q1556_NASA.pdf".GetBytesFromUrl());
        
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = contentRootPath,
            WebRootPath = contentRootPath,
        });
        var services = builder.Services;
        var config = builder.Configuration;

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(new NUnitLoggerProvider());
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        services.AddPlugin(new ApiKeysFeature());
        var chatFeature = new ChatFeature
        {
            EnableProviders =
            [
                "openrouter_free",
                "ollama",
                "groq",
                "codestral",
                "google_free",
                "anthropic",
                "openai",
                "grok",
                "qwen",
                "z.ai",
                "mistral",
            ],
            VirtualFiles = new FileSystemVirtualFiles(appDataDir),
            Variables =
            {
                ["OPENROUTER_API_KEY"] = Environment.GetEnvironmentVariable("OPENROUTER_FREE_API_KEY")
                                         ?? throw new Exception("OPENROUTER_FREE_API_KEY not set"),
            }
        };
        configure?.Invoke(chatFeature);
        services.AddPlugin(chatFeature);
        
        var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        services.AddSingleton<IDbConnectionFactory>(dbFactory);
        services.AddServiceStack(typeof(ApiKeyServices).Assembly);

        var app = builder.Build();
        app.UseServiceStack(new AppHost(), options => {
            options.MapEndpoints();
        });
        return app;
    }

    public AiChatIntegrationTests()
    {
        var app = ConfigureAppHost();
        app.StartAsync(TestsConfig.ListeningOn);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown() => AppHostBase.DisposeApp();

    public async Task WaitForLoadAsync()
    {
        while (!ServiceStackHost.HasLoaded)
        {
            await Task.Delay(100);
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
    
    public IChatClients GetChatClients() => HostContext.AppHost.GetApplicationServices()
        .GetRequiredService<IChatClients>();
    
    void AssertValidResponse(ChatResponse response, string? model = null)
    {
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Id, Is.Not.Null);
        Assert.That(response.Object, Is.EqualTo("chat.completion"));
        Assert.That(response.Created, Is.GreaterThan(0));
        Assert.That(response.Choices.Count, Is.GreaterThan(0));
        Assert.That(response.Choices[0].Message.Content, Is.Not.Null);
        Assert.That(response.Choices[0].Message.Role, Is.EqualTo("assistant"));
        Assert.That(response.Usage, Is.Not.Null);
        Assert.That(response.Usage.CompletionTokens, Is.GreaterThan(0));
        Assert.That(response.Usage.PromptTokens, Is.GreaterThan(0));
        Assert.That(response.Usage.TotalTokens, Is.GreaterThan(0));
        if (model != null)
            Assert.That(response.Model, Is.EqualTo(model));
    }

    private static async Task<OllamaProvider> GetOllama()
    {
        var feature = HostContext.GetPlugin<ChatFeature>();
        await feature.LoadAsync();
        var provider = feature.GetOllamaProvider("ollama");
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider.Models.Count, Is.GreaterThan(0));
        return provider;
    }
    
    private static async Task<OpenAiProvider> GetOpenRouter()
    {
        var feature = HostContext.GetPlugin<ChatFeature>();
        var provider = feature.GetOpenAiProvider("openrouter_free");
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider.Models.Count, Is.GreaterThan(0));
        return provider;
    }
    
    private static async Task<OpenAiProvider> GetOpenRouterPaid()
    {
        var feature = HostContext.GetPlugin<ChatFeature>();
        var provider = feature.GetOpenAiProvider("openrouter");
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider.Models.Count, Is.GreaterThan(0));
        return provider;
    }
    
    private static async Task<GoogleProvider> GetGoogle()
    {
        var feature = HostContext.GetPlugin<ChatFeature>();
        var provider = feature.GetGoogleProvider("google_free");
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider.Models.Count, Is.GreaterThan(0));
        return provider;
    }
    
    private static async Task<OpenAiProvider> GetOpenAi()
    {
        var feature = HostContext.GetPlugin<ChatFeature>();
        var provider = feature.GetOpenAiProvider("openai");
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider.Models.Count, Is.GreaterThan(0));
        return provider;
    }
    
    [Test]
    public async Task Can_send_request_to_Groq_using_IChatClients_factory()
    {
        var chatClients = GetChatClients();
        
        var request = new ChatCompletion
        {
            Model = "llama4:109b",
            Messages = [
                Message.Text("Capital of France?"),
            ]
        };
        
        var response = await chatClients.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("Paris"));
    }
    
    [Test]
    public async Task Can_send_request_to_Groq_using_IChatClients_groq()
    {
        var chatClients = GetChatClients();
        
        var request = new ChatCompletion
        {
            Model = "llama4:109b",
            Messages = [
                Message.Text("Capital of France?"),
            ]
        };
        
        var client = chatClients.GetRequiredClient("groq");
        var response = await client.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("Paris"));
    }
    
    [Test]
    public async Task Can_send_request_to_Ollama()
    {
        var ollama = await GetOllama();
        
        var request = new ChatCompletion
        {
            Model = "mistral-small3.2:24b",
            Messages = [
                Message.Text("Capital of France?"),
            ]
        };
        
        var response = await ollama.ChatAsync(request);
        // response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("Paris"));
    }
    
    [Test]
    public async Task Can_send_request_to_OpenRouter_Free()
    {
        var feature = HostContext.GetPlugin<ChatFeature>();
        var provider = feature.GetRequiredProvider<OpenAiProvider>("openrouter_free");
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider!.Models.Count, Is.GreaterThan(0));
        
        var request = new ChatCompletion
        {
            Model = "mistral-small3.2:24b",
            Messages = [
                Message.Text("Capital of France?"),
            ]
        };
        
        var response = await provider.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("Paris"));
    }
    
    [Test]
    public async Task Can_send_request_to_Groq()
    {
        var feature = HostContext.GetPlugin<ChatFeature>();
        var provider = feature.GetRequiredProvider<OpenAiProvider>("groq");
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider!.Models.Count, Is.GreaterThan(0));
        
        var request = new ChatCompletion
        {
            Model = "llama4:109b",
            Messages = [
                Message.Text("Capital of France?"),
            ]
        };
        
        var response = await provider.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("Paris"));
    }

    [Test]
    public async Task Can_send_request_to_Codestral()
    {
        var feature = HostContext.GetPlugin<ChatFeature>();
        var provider = feature.GetRequiredProvider<OpenAiProvider>("codestral");
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider!.Models.Count, Is.GreaterThan(0));
        
        var request = new ChatCompletion
        {
            Model = "codestral:22b",
            Messages = [
                Message.Text("Capital of France?"),
            ]
        };
        
        var response = await provider.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("Paris"));
    }

    [Test]
    public async Task Can_send_request_to_Google_Free()
    {
        var feature = HostContext.GetPlugin<ChatFeature>();
        var provider = feature.GetRequiredProvider<GoogleProvider>("google_free");
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider!.Models.Count, Is.GreaterThan(0));
        
        var request = new ChatCompletion
        {
            Model = "gemini-flash-latest",
            Messages = [
                Message.Text("Capital of France?"),
            ]
        };
        
        var response = await provider.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("Paris"));
    }

    [Test]
    public async Task Can_send_request_to_Anthropic()
    {
        var feature = HostContext.GetPlugin<ChatFeature>();
        var provider = feature.GetRequiredProvider<OpenAiProvider>("anthropic");
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider!.Models.Count, Is.GreaterThan(0));
        
        var request = new ChatCompletion
        {
            Model = "claude-sonnet-4-5",
            Messages = [
                Message.Text("Capital of France?"),
            ]
        };
        
        var response = await provider.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("Paris"));
    }

    [Test]
    public async Task Can_send_request_to_OpenAi()
    {
        var feature = HostContext.GetPlugin<ChatFeature>();
        var provider = feature.GetRequiredProvider<OpenAiProvider>("openai");
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider!.Models.Count, Is.GreaterThan(0));
        
        var request = new ChatCompletion
        {
            Model = "gpt-5",
            Messages = [
                Message.Text("Capital of France?"),
            ]
        };
        
        var response = await provider.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("Paris"));
    }

    [Test]
    public async Task Can_send_request_to_Grok()
    {
        var feature = HostContext.GetPlugin<ChatFeature>();
        var provider = feature.GetRequiredProvider<OpenAiProvider>("grok");
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider!.Models.Count, Is.GreaterThan(0));
        
        var request = new ChatCompletion
        {
            Model = "grok-4",
            Messages = [
                Message.Text("Capital of France?"),
            ]
        };
        
        var response = await provider.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("Paris"));
    }

    [Test]
    public async Task Can_send_request_to_Qwen()
    {
        var feature = HostContext.GetPlugin<ChatFeature>();
        var provider = feature.GetRequiredProvider<OpenAiProvider>("qwen");
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider!.Models.Count, Is.GreaterThan(0));
        
        var request = new ChatCompletion
        {
            Model = "qwen3:32b",
            Messages = [
                Message.Text("Capital of France?"),
            ]
        };
        
        var response = await provider.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("Paris"));
    }

    [Test]
    public async Task Can_send_request_to_Zai()
    {
        var feature = HostContext.GetPlugin<ChatFeature>();
        var provider = feature.GetRequiredProvider<OpenAiProvider>("z.ai");
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider!.Models.Count, Is.GreaterThan(0));
        
        var request = new ChatCompletion
        {
            Model = "glm-4.6",
            Messages = [
                Message.Text("Capital of France?"),
            ]
        };
        
        var response = await provider.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("Paris"));
    }

    [Test]
    public async Task Can_send_request_to_Mistral()
    {
        var feature = HostContext.GetPlugin<ChatFeature>();
        var provider = feature.GetRequiredProvider<OpenAiProvider>("mistral");
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider!.Models.Count, Is.GreaterThan(0));
        
        var request = new ChatCompletion
        {
            Model = "mistral-nemo:12b",
            Messages = [
                Message.Text("Capital of France?"),
            ]
        };
        
        var response = await provider.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("Paris"));
    }

    [Test]
    public async Task Can_send_Ollama_image_request_with_url()
    {
        var ollama = await GetOllama();
        
        var request = new ChatCompletion
        {
            Model = "qwen2.5vl:7b",
            Messages = [
                Message.Image(imageUrl:"https://ubixar.com/avatars/ub/ubixar.webp",
                    text:"Describe the key features of the input image"),
            ]
        };
        
        var response = await ollama.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("female").Or.Contain("woman").Or.Contain("character"));
    }

    [Test]
    public async Task Can_send_Ollama_image_request_with_file_path()
    {
        var ollama = await GetOllama();
        
        var request = new ChatCompletion
        {
            Model = "qwen2.5vl:7b",
            Messages = [
                Message.Image(imageUrl:"/ubixar.webp",
                    text:"Describe the key features of the input image"),
            ]
        };
        
        var response = await ollama.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("female").Or.Contain("woman").Or.Contain("character"));
    }

    [Test]
    public async Task Can_send_Ollama_image_request_with_data_uri()
    {
        var ollama = await GetOllama();
        
        var feature = HostContext.GetPlugin<ChatFeature>();
        var bytes = await feature.VirtualFiles!.GetFile("/ubixar.webp").ReadAllBytesAsync();
        var dataUri = $"data:image/webp;base64,{Convert.ToBase64String(bytes)}";
        
        var request = new ChatCompletion
        {
            Model = "qwen2.5vl:7b",
            Messages = [
                Message.Image(imageUrl:dataUri,
                    text:"Describe the key features of the input image"),
            ]
        };
        
        var response = await ollama.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("female").Or.Contain("woman").Or.Contain("character"));
    }
    
    [Test]
    public async Task Can_send_OpenRouter_image_request_with_url()
    {
        var provider = await GetOpenRouter();
        
        var request = new ChatCompletion
        {
            Model = "qwen2.5vl",
            Messages = [
                Message.Image(imageUrl:"https://ubixar.com/avatars/ub/ubixar.webp",
                    text:"Describe the key features of the input image"),
            ]
        };
        
        var response = await provider.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("female").Or.Contain("woman").Or.Contain("character"));
    }
    
    [Test]
    public async Task Can_send_Google_image_request_with_url()
    {
        var provider = await GetGoogle();
        
        var request = new ChatCompletion
        {
            Model = "gemini-flash-latest",
            Messages = [
                Message.Image(imageUrl:"https://ubixar.com/avatars/ub/ubixar.webp",
                    text:"Describe the key features of the input image"),
            ]
        };
        
        var response = await  provider.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("female").Or.Contain("woman").Or.Contain("character"));
    }

    [Test]
    public async Task Can_send_Google_audio_request_with_url()
    {
        var provider = await GetGoogle();
        
        var request = new ChatCompletion
        {
            Model = "gemini-flash-latest",
            Messages = [
                Message.Audio(data:"https://media.servicestack.com/audio/speaker.mp3",
                    text:"Please transcribe and summarize this audio file"),
            ]
        };
        
        var response = await provider.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("rainbow"));
    }

    [Test]
    public async Task Can_send_OpenAi_audio_request_with_url()
    {
        var provider = await GetOpenAi();
        
        var request = new ChatCompletion
        {
            Model = "gpt-4o-audio-preview",
            Messages = [
                Message.Audio(data:"https://media.servicestack.com/audio/speaker.mp3",
                    text:"Please transcribe and summarize this audio file"),
            ]
        };
        
        var response = await provider.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("rainbow"));
    }

    [Test]
    public async Task Can_send_OpenRouter_pdf_request_with_url()
    {
        var provider = await GetOpenRouter();
        
        var request = new ChatCompletion
        {
            Model = "qwen2.5vl",
            Messages = [
                Message.File(
                    fileData:"https://media.servicestack.com/documents/Q1556_NASA.pdf",
                    text:"Please summarize this document"),
            ]
        };
        
        var response = await provider.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("2,997"));
    }

    [Test]
    public async Task Can_send_Google_pdf_request_with_url()
    {
        var provider = await GetGoogle();
        
        var request = new ChatCompletion
        {
            Model = "gemini-flash-latest",
            Messages = [
                Message.File(
                    fileData:"https://media.servicestack.com/documents/Q1556_NASA.pdf",
                    text:"Please summarize this document"),
            ]
        };
        
        var response = await provider.ChatAsync(request);
        response.PrintDump();
        AssertValidResponse(response);
        var answer = response.GetAnswer();
        Assert.That(answer, Does.Contain("2,997"));
    }

    [Test]
    public async Task Can_send_ChatCompletion_request()
    {
        var client = await CreateClientAsync();
        var response = await client.SendAsync(new ChatCompletion
        {
            Model = "gpt-oss:20b",
            Messages = [
                Message.Text("Capital of France?"),
            ]
        });
        ClientConfig.PrintDump(response);
        AssertValidResponse(response);
    }
}
