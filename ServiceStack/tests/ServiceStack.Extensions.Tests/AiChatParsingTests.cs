#nullable enable
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.Extensions.Tests;

public class AiChatParsingTests
{
    const string FullProviderJson = 
        """
        {
            "enabled": false,
            "type": "OpenAiProvider",
            "base_url": "https://api.z.ai/api/paas/v4",
            "api_key": "$ZAI_API_KEY",
            "models": {
                "glm-4.6": "glm-4.6",
                "glm-4.5": "glm-4.5",
                "glm-4.5-air": "glm-4.5-air",
                "glm-4.5-x": "glm-4.5-x",
                "glm-4.5-airx": "glm-4.5-airx",
                "glm-4.5-flash": "glm-4.5-flash",
                "glm-4:32b": "glm-4-32b-0414-128k"
            },
            "temperature": 0.7,
            "headers": {
                "Content-Type": "application/json",
                "User-Agent": "llms.py/1.0"
            },
            "frequency_penalty": 1,
            "logprobs": true,
            "max_completion_tokens": 1024,
            "n": 1,
            "parallel_tool_calls": true,
            "presence_penalty": 1,
            "prompt_cache_key": "prompt-cache-key",
            "reasoning_effort": "reasoning-effort",
            "safety_identifier": "safety-identifier",
            "seed": 1,
            "service_tier": "service-tier",
            "stop": [
                "stop1",
                "stop2"
            ],
            "store": true,
            "top_logprobs": 1,
            "top_p": 1,
            "verbosity": "verbosity",
            "enable_thinking": true
        }
        """;

    [Test]
    public void Can_create_full_OpenAiProvider_Json()
    {
        var obj = (Dictionary<string, object?>) JSON.parse(FullProviderJson);
        var provider = OpenAiProvider.Create(NullLogger.Instance, null!, obj)!;
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider.BaseUrl, Is.EqualTo("https://api.z.ai/api/paas/v4"));
        Assert.That(provider.ApiKey, Is.EqualTo("$ZAI_API_KEY"));
        Assert.That(provider.Temperature, Is.EqualTo(0.7));
        Assert.That(provider.Models, Is.Not.Null);
        Assert.That(provider.Models.Count, Is.GreaterThan(0));
        Assert.That(provider.Headers, Is.Not.Null);
        Assert.That(provider.Headers.Keys, Is.EquivalentTo(new[] { "Content-Type", "User-Agent", "Authorization" })); 
        Assert.That(provider.FrequencyPenalty, Is.EqualTo(1));
        Assert.That(provider.LogProbs, Is.True);
        Assert.That(provider.MaxCompletionTokens, Is.EqualTo(1024));
        Assert.That(provider.N, Is.EqualTo(1));
        Assert.That(provider.ParallelToolCalls, Is.True);
        Assert.That(provider.PresencePenalty, Is.EqualTo(1));
        Assert.That(provider.PromptCacheKey, Is.EqualTo("prompt-cache-key"));
        Assert.That(provider.ReasoningEffort, Is.EqualTo("reasoning-effort"));
        Assert.That(provider.SafetyIdentifier, Is.EqualTo("safety-identifier"));
        Assert.That(provider.Seed, Is.EqualTo(1));
        Assert.That(provider.ServiceTier, Is.EqualTo("service-tier"));
        Assert.That(provider.Stop, Is.Not.Null);
        Assert.That(provider.Stop.Count, Is.EqualTo(2));
        Assert.That(provider.Store, Is.True);
        Assert.That(provider.TopLogprobs, Is.EqualTo(1));
        Assert.That(provider.TopP, Is.EqualTo(1));
        Assert.That(provider.Verbosity, Is.EqualTo("verbosity"));
        Assert.That(provider.EnableThinking, Is.True);
    }

    [Test]
    public void Can_get_different_numeric_values()
    {
        var obj = (Dictionary<string, object?>) JSON.parse(FullProviderJson);
        if (obj.TryGetValue("temperature", out int i))
        {
            Assert.That(i, Is.EqualTo(1));
        } 
        if (obj.TryGetValue("temperature", out int l))
        {
            Assert.That(l, Is.EqualTo(1));
        } 
        if (obj.TryGetValue("seed", out double d))
        {
            Assert.That(d, Is.EqualTo(1));
        } 
        if (obj.TryGetValue("seed", out int lSeed))
        {
            Assert.That(lSeed, Is.EqualTo(1));
        } 
    }

    [Test]
    public void Can_use_TryGetValue_on_Object_and_Lists()
    {
        var obj = (Dictionary<string, object?>) JSON.parse(FullProviderJson);
        if (obj.TryGetValue("headers", out Dictionary<string, object?> headers))
        {
            Assert.That(headers, Is.Not.Null);
            Assert.That(headers.Count, Is.EqualTo(2));
        }
        else Assert.Fail("Failed to get headers");
        if (obj.TryGetValue("stop", out List<object> stop))
        {
            Assert.That(stop, Is.Not.Null);
            Assert.That(stop.Count, Is.EqualTo(2));
        }
        else Assert.Fail("Failed to get stops");
    }
    
    const string GoogleProviderJson = 
        """
        {
            "enabled": false,
            "type": "GoogleProvider",
            "api_key": "$GOOGLE_API_KEY",
            "models": {
                "gemini-flash-latest": "gemini-flash-latest",
                "gemini-flash-lite-latest": "gemini-flash-lite-latest",
                "gemini-2.5-pro": "gemini-2.5-pro",
                "gemini-2.5-flash": "gemini-2.5-flash",
                "gemini-2.5-flash-lite": "gemini-2.5-flash-lite"
            },
            "safety_settings": [
                {
                    "category": "HARM_CATEGORY_DANGEROUS_CONTENT",
                    "threshold": "BLOCK_ONLY_HIGH"
                }
            ],
            "thinking_config": {
                "thinkingBudget": 1024,
                "includeThoughts": true
            }
        }
        """;

    [Test]
    public void Can_create_GoogleProvider_Json()
    {
        var obj = (Dictionary<string, object?>) JSON.parse(GoogleProviderJson);
        var provider = (GoogleProvider)GoogleProvider.Create(NullLogger.Instance, null!, obj)!;
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider.ApiKey, Is.EqualTo("$GOOGLE_API_KEY"));
        Assert.That(provider.Models, Is.Not.Null);
        Assert.That(provider.Models.Count, Is.GreaterThan(0));
        Assert.That(provider.SafetySettings, Is.Not.Null);
        Assert.That(provider.SafetySettings!.Count, Is.EqualTo(1));
        Assert.That(provider.ThinkingConfig, Is.Not.Null);
        Assert.That(provider.ThinkingConfig!.Count, Is.EqualTo(2));
    }
    
}