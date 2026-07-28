#nullable enable
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.Extensions.Tests;

public class AiChatParsingTests
{
    // llms.json v3-format merged provider definition (models.dev entry + overrides)
    const string FullProviderJson =
        """
        {
            "id": "zai",
            "npm": "@ai-sdk/openai-compatible",
            "api": "https://api.z.ai/api/paas/v4/",
            "api_key": "$ZAI_API_KEY",
            "env": ["ZAI_API_KEY"],
            "models": {
                "glm-4.6": {
                    "id": "glm-4.6",
                    "name": "GLM-4.6",
                    "cost": { "input": 0.6, "output": 2.2 }
                },
                "glm-4.5-air": {
                    "id": "glm-4.5-air",
                    "name": "GLM-4.5-Air",
                    "cost": { "input": 0.2, "output": 1.1 }
                }
            },
            "temperature": 0.7,
            "headers": {
                "Content-Type": "application/json",
                "User-Agent": "llms.py/1.0"
            },
            "frequency_penalty": 1,
            "max_completion_tokens": 1024,
            "n": 1,
            "parallel_tool_calls": true,
            "presence_penalty": 1,
            "prompt_cache_key": "prompt-cache-key",
            "reasoning_effort": "reasoning-effort",
            "safety_identifier": "safety-identifier",
            "seed": 1,
            "service_tier": "service-tier",
            "stop": ["stop1", "stop2"],
            "store": true,
            "top_logprobs": 1,
            "top_p": 1,
            "verbosity": "verbosity",
            "enable_thinking": true,
            "stream": false,
            "server_tools": ["web_search"]
        }
        """;

    [Test]
    public void Can_populate_full_provider_definition()
    {
        var provider = new OpenAiCompatibleProvider();
        provider.Populate(ChatJson.ParseObject(FullProviderJson));

        Assert.That(provider.Id, Is.EqualTo("zai"));
        Assert.That(provider.Api, Is.EqualTo("https://api.z.ai/api/paas/v4"));
        Assert.That(provider.ChatUrl, Is.EqualTo("https://api.z.ai/api/paas/v4/chat/completions"));
        Assert.That(provider.ApiKey, Is.EqualTo("$ZAI_API_KEY"));
        Assert.That(provider.Env, Is.EquivalentTo(new[] { "ZAI_API_KEY" }));
        Assert.That(provider.Temperature, Is.EqualTo(0.7));
        Assert.That(provider.Models.Count, Is.EqualTo(2));
        Assert.That(provider.Headers.Keys, Is.EquivalentTo(new[] { "Content-Type", "User-Agent", "Authorization" }));
        Assert.That(provider.FrequencyPenalty, Is.EqualTo(1));
        Assert.That(provider.MaxCompletionTokens, Is.EqualTo(1024));
        Assert.That(provider.N, Is.EqualTo(1));
        Assert.That(provider.ParallelToolCalls, Is.True);
        Assert.That(provider.PresencePenalty, Is.EqualTo(1));
        Assert.That(provider.PromptCacheKey, Is.EqualTo("prompt-cache-key"));
        Assert.That(provider.ReasoningEffort, Is.EqualTo("reasoning-effort"));
        Assert.That(provider.SafetyIdentifier, Is.EqualTo("safety-identifier"));
        Assert.That(provider.Seed, Is.EqualTo(1));
        Assert.That(provider.ServiceTier, Is.EqualTo("service-tier"));
        Assert.That(provider.Stop!.AsArray().Count, Is.EqualTo(2));
        Assert.That(provider.Store, Is.True);
        Assert.That(provider.TopLogprobs, Is.EqualTo(1));
        Assert.That(provider.TopP, Is.EqualTo(1));
        Assert.That(provider.Verbosity, Is.EqualTo("verbosity"));
        Assert.That(provider.EnableThinking, Is.True);
        Assert.That(provider.Stream, Is.False);
        Assert.That(provider.ServerTools, Is.EquivalentTo(new[] { "web_search" }));
    }

    [Test]
    public void Stream_defaults_to_true()
    {
        var provider = new OpenAiCompatibleProvider();
        provider.Populate(ChatJson.ParseObject(
            """{"id":"test","api":"https://example.org/v1"}"""));
        Assert.That(provider.Stream, Is.True);
        Assert.That(provider.Name, Is.EqualTo("Test"));
    }

    [Test]
    public void Can_resolve_provider_models_case_insensitively()
    {
        var provider = new OpenAiCompatibleProvider();
        provider.Populate(ChatJson.ParseObject(FullProviderJson));

        Assert.That(provider.ProviderModel("glm-4.6"), Is.EqualTo("glm-4.6"));
        Assert.That(provider.ProviderModel("GLM-4.6"), Is.EqualTo("glm-4.6"));
        Assert.That(provider.ProviderModel("GLM-4.5-Air"), Is.EqualTo("glm-4.5-air"));
        Assert.That(provider.ProviderModel("zai/glm-4.6"), Is.EqualTo("glm-4.6"));
        Assert.That(provider.ProviderModel("unknown-model"), Is.Null);

        Assert.That(provider.ModelInfo("glm-4.6")!["name"]!.GetValue<string>(), Is.EqualTo("GLM-4.6"));
        Assert.That(provider.ModelCost("glm-4.6")!["input"]!.GetValue<double>(), Is.EqualTo(0.6));
    }

    [Test]
    public void Map_models_filters_and_maps()
    {
        var provider = new OpenAiCompatibleProvider();
        provider.Populate(ChatJson.ParseObject(
            """
            {
                "id": "test",
                "api": "https://example.org/v1",
                "models": {
                    "model-a": { "id": "model-a", "name": "Model A" },
                    "model-b": { "id": "model-b", "name": "Model B" }
                },
                "map_models": { "my-model": "model-a" }
            }
            """));

        Assert.That(provider.Models.Keys, Is.EquivalentTo(new[] { "model-a" }));
        Assert.That(provider.ProviderModel("my-model"), Is.EqualTo("model-a"));
        Assert.That(provider.ProviderModel("model-a"), Is.EqualTo("model-a"));
    }

    [Test]
    public void Include_and_exclude_model_regex_filters()
    {
        const string json =
            """
            {
                "id": "test",
                "api": "https://example.org/v1",
                "models": {
                    "gpt-4o": { "id": "gpt-4o", "name": "GPT-4o" },
                    "gpt-4o-mini": { "id": "gpt-4o-mini", "name": "GPT-4o mini" },
                    "o1-preview": { "id": "o1-preview", "name": "o1 preview" }
                }
            }
            """;

        var include = new OpenAiCompatibleProvider();
        var includeDef = ChatJson.ParseObject(json);
        includeDef["include_models"] = "^gpt";
        include.Populate(includeDef);
        Assert.That(include.Models.Keys, Is.EquivalentTo(new[] { "gpt-4o", "gpt-4o-mini" }));

        var exclude = new OpenAiCompatibleProvider();
        var excludeDef = ChatJson.ParseObject(json);
        excludeDef["exclude_models"] = "mini";
        exclude.Populate(excludeDef);
        Assert.That(exclude.Models.Keys, Is.EquivalentTo(new[] { "gpt-4o", "o1-preview" }));
    }

    [Test]
    public void Validate_requires_api_key()
    {
        var provider = new OpenAiCompatibleProvider();
        provider.Populate(ChatJson.ParseObject(
            """{"id":"test","api":"https://example.org/v1","env":["TEST_API_KEY"]}"""));
        Assert.That(provider.Validate(), Does.Contain("TEST_API_KEY"));
        Assert.That(provider.Test(), Is.False);

        var withKey = new OpenAiCompatibleProvider();
        withKey.Populate(ChatJson.ParseObject(
            """{"id":"test","api":"https://example.org/v1","api_key":"sk-123"}"""));
        Assert.That(withKey.Validate(), Is.Null);
        Assert.That(withKey.Test(), Is.True);
        Assert.That(withKey.Headers["Authorization"], Is.EqualTo("Bearer sk-123"));
    }

    [Test]
    public void Ollama_provider_uses_v1_chat_endpoint_and_no_api_key()
    {
        var provider = new OllamaProvider();
        provider.Populate(ChatJson.ParseObject(
            """{"id":"ollama","api":"http://localhost:11434"}"""));
        Assert.That(provider.ChatUrl, Is.EqualTo("http://localhost:11434/v1/chat/completions"));
        Assert.That(provider.Validate(), Is.Null);
        Assert.That(provider.Test(), Is.True);
    }
}
