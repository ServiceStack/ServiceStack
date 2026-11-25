#nullable enable
using System;
using System.Text.Json.Nodes;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.AiTests;

public class AiChatCliProviderTests
{
    sealed class TestGoogleCliProvider(string? executable) : GoogleCliProvider
    {
        protected override string? FindExecutable() => executable;
    }

    sealed class TestAnthropicCliProvider(string? executable) : AnthropicCliProvider
    {
        protected override string? FindExecutable() => executable;
    }

    [Test]
    public void ChatFeature_registers_google_and_anthropic_cli_providers()
    {
        var feature = new ChatFeature();
        Assert.That(feature.ProviderTypes["@ai-sdk/google-cli"](), Is.InstanceOf<GoogleCliProvider>());
        Assert.That(feature.ProviderTypes["@ai-sdk/anthropic-cli"](), Is.InstanceOf<AnthropicCliProvider>());
    }

    [Test]
    public void GoogleCliProvider_validate_accepts_cli_or_api_key_fallback()
    {
        var withCli = new TestGoogleCliProvider("/usr/local/bin/agy");
        withCli.Populate(new JsonObject { ["id"] = "google", ["name"] = "Google", ["env"] = new JsonArray("GOOGLE_GENERATIVE_AI_API_KEY") });
        Assert.That(withCli.Validate(), Is.Null);

        var withApiKey = new TestGoogleCliProvider(null);
        withApiKey.Populate(new JsonObject { ["id"] = "google", ["name"] = "Google", ["api_key"] = "test-key", ["env"] = new JsonArray("GOOGLE_GENERATIVE_AI_API_KEY") });
        Assert.That(withApiKey.Validate(), Is.Null);

        var withNeither = new TestGoogleCliProvider(null);
        withNeither.Populate(new JsonObject { ["id"] = "google", ["name"] = "Google", ["env"] = new JsonArray("GOOGLE_GENERATIVE_AI_API_KEY") });
        Assert.That(withNeither.Validate(), Does.Contain("agy"));
    }

    [Test]
    public void AnthropicCliProvider_validate_accepts_cli_or_api_key_fallback()
    {
        var withCli = new TestAnthropicCliProvider("/usr/local/bin/claude");
        withCli.Populate(new JsonObject { ["id"] = "anthropic", ["name"] = "Anthropic", ["env"] = new JsonArray("ANTHROPIC_API_KEY") });
        Assert.That(withCli.Validate(), Is.Null);

        var withApiKey = new TestAnthropicCliProvider(null);
        withApiKey.Populate(new JsonObject { ["id"] = "anthropic", ["name"] = "Anthropic", ["api_key"] = "test-key", ["env"] = new JsonArray("ANTHROPIC_API_KEY") });
        Assert.That(withApiKey.Validate(), Is.Null);

        var withNeither = new TestAnthropicCliProvider(null);
        withNeither.Populate(new JsonObject { ["id"] = "anthropic", ["name"] = "Anthropic", ["env"] = new JsonArray("ANTHROPIC_API_KEY") });
        Assert.That(withNeither.Validate(), Does.Contain("claude"));
    }

    [Test]
    public void GoogleCliProvider_parses_tab_and_space_models_output()
    {
        var raw = "\r\x1b[36m⠋ Fetching available models...\x1b[0m\r"
            + "gemini-4-neon-high\tGemini 4 Neon (High)\n"
            + "gemini-4-neon-medium           Gemini 4 Neon (Medium)\n"
            + "gemini-3.8-flash-low\tGemini 3.8 Flash (Low)\n";

        var models = GoogleCliProvider.ParseModelsOutput(raw);
        Assert.That(models, Has.Count.EqualTo(3));
        Assert.That(models[0], Is.EqualTo(("gemini-4-neon-high", "Gemini 4 Neon (High)")));
        Assert.That(models[1], Is.EqualTo(("gemini-4-neon-medium", "Gemini 4 Neon (Medium)")));
        Assert.That(models[2], Is.EqualTo(("gemini-3.8-flash-low", "Gemini 3.8 Flash (Low)")));
    }

    [Test]
    public void GoogleCliProvider_builds_prompt_and_resolves_effort()
    {
        var messages = new JsonArray(
            new JsonObject { ["role"] = "system", ["content"] = "Be concise." },
            new JsonObject { ["role"] = "user", ["content"] = "First" },
            new JsonObject { ["role"] = "assistant", ["content"] = "Ack" },
            new JsonObject { ["role"] = "user", ["content"] = "Hello" }
        );
        var prompt = GoogleCliProvider.BuildPrompt(messages);
        Assert.That(prompt, Does.Contain("System instructions:\nBe concise."));
        Assert.That(prompt, Does.Contain("Previous conversation:\n[user]: First\n\n[assistant]: Ack"));
        Assert.That(prompt, Does.EndWith("Hello"));

        var chat = new JsonObject { ["reasoning"] = new JsonObject { ["reasoning_effort"] = "low" } };
        Assert.That(GoogleCliProvider.ResolveEffort(chat, "gemini-4-neon"), Is.EqualTo("low"));
        Assert.That(GoogleCliProvider.ResolveEffort(chat, "gemini-4-neon-high"), Is.Null);
    }

    [Test]
    public void GoogleCliProvider_maps_cli_response_and_usage()
    {
        var cliResponse = new JsonObject
        {
            ["conversation_id"] = "conv-1",
            ["status"] = "SUCCESS",
            ["response"] = "Hello!\n",
            ["duration_seconds"] = 1.5,
            ["usage"] = new JsonObject
            {
                ["input_tokens"] = 100,
                ["output_tokens"] = 10,
                ["thinking_tokens"] = 4,
                ["cache_read_tokens"] = 50,
                ["total_tokens"] = 110,
            }
        };
        var res = GoogleCliProvider.ToCliResponse(cliResponse, new JsonObject { ["model"] = "gemini-4-neon" }, DateTimeOffset.UtcNow);
        Assert.That(res.GetString("id"), Is.EqualTo("conv-1"));
        Assert.That(res.GetArray("choices")![0]!.AsObject().GetObject("message")!.GetString("content"), Is.EqualTo("Hello!"));
        Assert.That(res.GetObject("usage")!.GetLong("prompt_tokens"), Is.EqualTo(100));
        Assert.That(res.GetObject("usage")!.GetLong("completion_tokens"), Is.EqualTo(10));
        Assert.That(res.GetObject("usage")!.GetObject("prompt_tokens_details")!.GetLong("cached_tokens"), Is.EqualTo(50));
        Assert.That(res.GetObject("usage")!.GetObject("completion_tokens_details")!.GetLong("reasoning_tokens"), Is.EqualTo(4));
        Assert.That(res.GetObject("metadata")!.GetLong("duration"), Is.EqualTo(1500));
        Assert.That(res.GetObject("metadata")!.GetString("pricing"), Is.EqualTo("0/0"));
    }

    [Test]
    public void GoogleCliProvider_materializes_image_and_audio_attachments()
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test-agy-media-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(dir);
        try
        {
            var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFBQIAX8jx0gAAAABJRU5ErkJggg==");
            var wav = System.Text.Encoding.ASCII.GetBytes("RIFF\x24\0\0\0WAVEfmt ");
            var messages = new JsonArray(
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = new JsonArray(
                        new JsonObject { ["type"] = "text", ["text"] = "Check media" },
                        new JsonObject { ["type"] = "image_url", ["image_url"] = new JsonObject { ["url"] = "data:image/png;base64," + Convert.ToBase64String(png) } },
                        new JsonObject { ["type"] = "input_audio", ["input_audio"] = new JsonObject { ["data"] = Convert.ToBase64String(wav), ["format"] = "wav" } }
                    )
                }
            );

            var prompt = GoogleCliProvider.BuildPrompt(messages, dir);
            Assert.That(prompt, Does.Contain("[Attached image: attachment_1.png (image/png)]"));
            Assert.That(prompt, Does.Contain("[Attached audio: attachment_2.wav (audio/wav)]"));
            Assert.That(prompt, Does.Contain("read_file"));
            Assert.That(System.IO.File.ReadAllBytes(System.IO.Path.Combine(dir, "attachment_1.png")), Is.EqualTo(png));
            Assert.That(System.IO.File.ReadAllBytes(System.IO.Path.Combine(dir, "attachment_2.wav")), Is.EqualTo(wav));
        }
        finally
        {
            System.IO.Directory.Delete(dir, true);
        }
    }

    [Test]
    public void AnthropicCliProvider_advertises_media_modalities_and_materializes_audio()
    {
        var provider = new TestAnthropicCliProvider("/usr/local/bin/claude");
        provider.Populate(new JsonObject
        {
            ["id"] = "anthropic",
            ["name"] = "Anthropic",
            ["models"] = new JsonObject
            {
                ["claude-sonnet"] = new JsonObject
                {
                    ["id"] = "claude-sonnet",
                    ["name"] = "Claude Sonnet",
                    ["modalities"] = new JsonObject { ["input"] = new JsonArray("text") }
                }
            }
        });
        var inputs = provider.Models["claude-sonnet"].GetObject("modalities")!.GetArray("input")!;
        Assert.That(inputs.ToJsonString(), Does.Contain("\"image\"").And.Contain("\"audio\""));

        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test-claude-audio-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(dir);
        try
        {
            var wav = System.Text.Encoding.ASCII.GetBytes("RIFF\x24\0\0\0WAVEfmt ");
            var messages = new JsonArray(
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = new JsonArray(
                        new JsonObject { ["type"] = "input_audio", ["input_audio"] = new JsonObject { ["data"] = Convert.ToBase64String(wav), ["format"] = "wav" } }
                    )
                }
            );
            var files = AnthropicCliProvider.MaterializeAudioAttachments(messages, dir);
            Assert.That(files, Has.Count.EqualTo(1));
            Assert.That(files[0], Is.EqualTo(("attachment_1.wav", "audio/wav")));
            Assert.That(System.IO.File.ReadAllBytes(System.IO.Path.Combine(dir, "attachment_1.wav")), Is.EqualTo(wav));
        }
        finally
        {
            System.IO.Directory.Delete(dir, true);
        }
    }
}
