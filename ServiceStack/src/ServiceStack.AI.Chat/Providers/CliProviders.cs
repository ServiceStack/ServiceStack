using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

internal static class CliExecutable
{
    public static string? FindInPath(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return null;

        if (Path.IsPathRooted(command))
            return File.Exists(command) ? command : null;

        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return null;

        var extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT;.COM")
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
            : [""];

        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = dir.Trim().Trim('"');
            if (trimmed.Length == 0)
                continue;

            foreach (var ext in extensions)
            {
                var candidate = Path.Combine(trimmed, command.EndsWith(ext, StringComparison.OrdinalIgnoreCase) ? command : command + ext);
                if (File.Exists(candidate))
                    return candidate;
            }
        }
        return null;
    }

    public static void TryTerminate(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // best effort cleanup
        }
    }
}

/// <summary>
/// Google Provider using local `agy` (Antigravity) CLI to make use of an existing subscription
/// (port of GoogleProviderCli in llms/extensions/providers/google.py).
/// Falls back to <see cref="GoogleProvider"/> HTTP API when `agy` is not in PATH and an API key is configured.
/// </summary>
public class GoogleCliProvider : GoogleProvider
{
    static readonly Regex AnsiRegex = new(@"\x1b\[[0-9;?]*[a-zA-Z]", RegexOptions.Compiled);
    static readonly Regex ModelLineRegex = new(@"^([a-zA-Z0-9][a-zA-Z0-9._-]*)(?:\t+|\s{2,})(.+)$", RegexOptions.Compiled);
    static readonly Regex EffortSuffixRegex = new(@"-(low|medium|high|xhigh|max)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static readonly HashSet<string> EffortLevels = new(StringComparer.OrdinalIgnoreCase) { "low", "medium", "high", "xhigh", "max" };

    Dictionary<string, JsonObject> catalogModels = [];
    bool cliModelsLoaded;

    public override string Sdk => "@ai-sdk/google-cli";

    protected virtual string? FindExecutable() => CliExecutable.FindInPath("agy");
    bool UseCli => FindExecutable() != null;

    public override void Populate(JsonObject kwargs)
    {
        base.Populate(kwargs);
        catalogModels = Models.ToDictionary(x => x.Key, x => x.Value.Clone());
        if (UseCli)
        {
            foreach (var model in Models.Values)
                model["tool_call"] = false;
        }
    }

    public override string? Validate()
    {
        if (UseCli)
            return null;
        if (!string.IsNullOrEmpty(ApiKey))
            return null;
        return $"Provider '{Name}' requires 'agy' in PATH or API Key {string.Join(", ", Env)}";
    }

    public static List<(string Slug, string DisplayName)> ParseModelsOutput(string output)
    {
        var result = new List<(string, string)>();
        foreach (var rawLine in output.Split('\n'))
        {
            var lastSegment = rawLine.Contains('\r') ? rawLine[(rawLine.LastIndexOf('\r') + 1)..] : rawLine;
            var line = AnsiRegex.Replace(lastSegment, "").Trim();
            if (line.Length == 0)
                continue;
            var match = ModelLineRegex.Match(line);
            if (match.Success)
                result.Add((match.Groups[1].Value, match.Groups[2].Value.Trim()));
        }
        return result;
    }

    public override async Task LoadAsync(CancellationToken token = default)
    {
        if (cliModelsLoaded)
            return;
        var executable = FindExecutable();
        if (executable == null)
            return;

        cliModelsLoaded = true;
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.StartInfo.ArgumentList.Add("models");
            process.Start();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));
            var stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);
            try
            {
                await process.WaitForExitAsync(timeoutCts.Token).ConfigAwait();
            }
            catch
            {
                CliExecutable.TryTerminate(process);
                return;
            }

            if (process.ExitCode != 0)
                return;

            var discovered = ParseModelsOutput(await stdoutTask.ConfigAwait());
            _ = await stderrTask.ConfigAwait();
            if (discovered.Count == 0)
                return;

            var models = new Dictionary<string, JsonObject>();
            var mapModels = new Dictionary<string, string>();
            foreach (var (slug, displayName) in discovered)
            {
                var baseSlug = EffortSuffixRegex.Replace(slug, "");
                var baseInfo = catalogModels.GetValueOrDefault(slug) ?? catalogModels.GetValueOrDefault(baseSlug);
                var info = baseInfo?.Clone() ?? new JsonObject();
                info["id"] = slug;
                info["name"] = displayName;
                info["tool_call"] = false;
                info["modalities"] = new JsonObject
                {
                    ["input"] = new JsonArray("text"),
                    ["output"] = new JsonArray("text"),
                };
                info["cost"] = new JsonObject { ["input"] = 0, ["output"] = 0 };

                models[slug] = info;
                mapModels[slug] = slug;
                mapModels[displayName] = slug;
                mapModels.TryAdd(baseSlug, baseSlug);
            }

            Models = models;
            MapModels = mapModels;
            Log.LogInformation("Loaded {Count} agy models for {Name}", models.Count, Name);
        }
        catch (Exception e)
        {
            Log.LogInformation("Failed to load agy models: {Message}", e.Message);
        }
    }

    public override string? ProviderModel(string model)
    {
        var resolved = base.ProviderModel(model);
        if (resolved != null)
            return resolved;

        var prefix = Id + "/";
        var candidate = model.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? model[prefix.Length..]
            : model;
        foreach (var slug in Models.Keys)
        {
            if (slug.EqualsIgnoreCase(candidate) || EffortSuffixRegex.Replace(slug, "").EqualsIgnoreCase(candidate))
                return candidate;
        }
        return candidate.StartsWith("gemini-", StringComparison.OrdinalIgnoreCase) ? candidate : null;
    }

    static string ExtractText(JsonNode? content)
    {
        if (content is JsonValue v && v.TryGetValue<string>(out var s))
            return s;
        if (content is JsonArray arr)
        {
            var parts = new List<string>();
            foreach (var item in arr)
            {
                if (item is JsonValue sv && sv.TryGetValue<string>(out var text))
                {
                    parts.Add(text);
                }
                else if (item is JsonObject obj)
                {
                    var type = obj.GetString("type");
                    if (type is "text" or "input_text" && obj.GetString("text") is { Length: > 0 } t)
                        parts.Add(t);
                }
            }
            return string.Join("\n", parts);
        }
        return content?.ToString() ?? "";
    }

    public static string BuildPrompt(JsonArray? messages)
    {
        var systemParts = new List<string>();
        var conversation = new List<(string Role, string Text)>();
        foreach (var node in messages ?? [])
        {
            if (node is not JsonObject msg)
                continue;
            var role = msg.GetString("role") ?? "user";
            var text = ExtractText(msg["content"]).Trim();
            if (text.Length == 0)
                continue;
            if (role is "system" or "developer")
                systemParts.Add(text);
            else
                conversation.Add((role, text));
        }

        var lastUserIdx = -1;
        for (var i = conversation.Count - 1; i >= 0; i--)
        {
            if (conversation[i].Role == "user")
            {
                lastUserIdx = i;
                break;
            }
        }

        var sections = new List<string>();
        if (systemParts.Count > 0)
            sections.Add("System instructions:\n" + string.Join("\n\n", systemParts));

        if (lastUserIdx < 0)
        {
            if (conversation.Count > 0)
                sections.Add(string.Join("\n\n", conversation.Select(x => $"[{x.Role}]: {x.Text}")));
        }
        else
        {
            if (lastUserIdx > 0)
            {
                var history = string.Join("\n\n", conversation.Take(lastUserIdx).Select(x => $"[{x.Role}]: {x.Text}"));
                sections.Add($"Previous conversation:\n{history}\n\nContinue the conversation based on the above context.");
            }
            sections.Add(conversation[lastUserIdx].Text);
            if (lastUserIdx + 1 < conversation.Count)
                sections.Add(string.Join("\n\n", conversation.Skip(lastUserIdx + 1).Select(x => $"[{x.Role}]: {x.Text}")));
        }

        return string.Join("\n\n", sections);
    }

    public static string? ResolveEffort(JsonObject chat, string? model, string? defaultEffort = null)
    {
        if (!string.IsNullOrEmpty(model) && EffortSuffixRegex.IsMatch(model))
            return null;
        var effort = chat.GetString("reasoning_effort")
            ?? chat.GetObject("reasoning")?.GetString("reasoning_effort")
            ?? defaultEffort;
        return !string.IsNullOrEmpty(effort) && EffortLevels.Contains(effort) ? effort.ToLowerInvariant() : null;
    }

    public override async Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        var executable = FindExecutable();
        if (executable == null)
            return await base.ChatAsync(chat, context).ConfigAwait();

        if (chat.GetArray("modalities")?.Any(m => m?.GetValue<string>() != "text") == true)
            throw new InvalidOperationException("Google CLI (agy) supports text responses only.");

        var requestedModel = chat.GetString("model") ?? "";
        var model = ProviderModel(requestedModel) ?? requestedModel;
        chat["model"] = model;
        chat = await ProcessChatAsync(chat, Id).ConfigAwait();

        var prompt = BuildPrompt(chat.GetArray("messages"));
        var effort = ResolveEffort(chat, model, ReasoningEffort);

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        startInfo.ArgumentList.Add("--input-format");
        startInfo.ArgumentList.Add("stream-json");
        startInfo.ArgumentList.Add("--output-format");
        startInfo.ArgumentList.Add("stream-json");
        startInfo.ArgumentList.Add("--disable-slash-commands");

        if (!string.IsNullOrEmpty(model))
        {
            startInfo.ArgumentList.Add("--model");
            startInfo.ArgumentList.Add(model);
        }
        if (!string.IsNullOrEmpty(effort))
        {
            startInfo.ArgumentList.Add("--effort");
            startInfo.ArgumentList.Add(effort);
        }
        if (chat.GetObject("response_format") is { } rf && rf.GetString("type") == "json_schema"
            && rf.GetObject("json_schema")?.GetObject("schema") is { } schema)
        {
            startInfo.ArgumentList.Add("--json-schema");
            startInfo.ArgumentList.Add(schema.ToJsonString(ChatJson.Options));
        }

        var jsonInput = new JsonObject
        {
            ["event"] = "user",
            ["message"] = new JsonObject { ["content"] = prompt },
        }.ToJsonString(ChatJson.Options) + "\n";

        Log.LogInformation("Running: agy --input-format stream-json --output-format stream-json --model {Model}", model);

        var startedAt = DateTimeOffset.UtcNow;
        var writer = context.NoStore ? new StreamCheckpointWriter(null, null) : CreateStreamWriter(context);
        var content = new StringBuilder();
        JsonObject? cliResult = null;
        var rawLines = new List<string>();
        var workdir = Path.Combine(Path.GetTempPath(), "llms-agy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workdir);
        startInfo.WorkingDirectory = workdir;

        try
        {
            using var process = new Process { StartInfo = startInfo };
            process.Start();
            try
            {
                await process.StandardInput.WriteAsync(jsonInput).ConfigAwait();
                await process.StandardInput.FlushAsync().ConfigAwait();
                process.StandardInput.Close();

                var stderrTask = process.StandardError.ReadToEndAsync(context.CancellationToken);
                string? line;
                while ((line = await process.StandardOutput.ReadLineAsync(context.CancellationToken).ConfigAwait()) != null)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();
                    var trimmed = line.Trim();
                    if (trimmed.Length == 0)
                        continue;
                    rawLines.Add(trimmed);
                    var obj = ChatJson.TryParseObject(trimmed);
                    if (obj == null)
                        continue;

                    var ev = obj.GetString("event");
                    if (ev == "step_update" && obj.GetObject("step_update") is { } step)
                    {
                        if (step.GetString("step_type") == "agent_response" && step.GetString("text_delta") is { Length: > 0 } delta)
                        {
                            content.Append(delta);
                            await writer.WriteAsync(new JsonObject
                            {
                                ["role"] = "assistant",
                                ["content"] = content.ToString(),
                                ["model"] = model,
                                ["timestamp"] = startedAt.ToUnixTimeMilliseconds(),
                            }).ConfigAwait();
                        }
                    }
                    else if (ev == "result")
                    {
                        cliResult = obj.GetObject("result")?.Clone() ?? new JsonObject();
                    }
                }

                await process.WaitForExitAsync(context.CancellationToken).ConfigAwait();
                var stderr = (await stderrTask.ConfigAwait()).Trim();

                if (cliResult != null && cliResult.GetString("status") is { } status && status != "SUCCESS")
                {
                    var err = cliResult.GetString("error") ?? (stderr.Length > 0 ? stderr : $"agy status: {status}");
                    throw new InvalidOperationException($"agy CLI error: {err}");
                }
                if (process.ExitCode != 0)
                {
                    var err = stderr.Length > 0 ? stderr : string.Join("\n", rawLines.TakeLast(5));
                    throw new InvalidOperationException($"agy CLI error: {(err.Length > 0 ? err : $"agy exited with code {process.ExitCode}")}");
                }
                if (cliResult == null)
                    throw new InvalidOperationException("No result found in agy CLI stream-json output");

                var finalText = cliResult.GetString("response") ?? content.ToString();
                await writer.WriteAsync(new JsonObject
                {
                    ["role"] = "assistant",
                    ["content"] = finalText,
                    ["model"] = model,
                    ["timestamp"] = startedAt.ToUnixTimeMilliseconds(),
                }, final: true).ConfigAwait();

                return ToCliResponse(cliResult, chat, startedAt, content.ToString(), context);
            }
            catch
            {
                CliExecutable.TryTerminate(process);
                await writer.FlushAsync().ConfigAwait();
                throw;
            }
        }
        finally
        {
            try { Directory.Delete(workdir, recursive: true); } catch { }
        }
    }

    public static JsonObject ToCliResponse(JsonObject cliResponse, JsonObject chat, DateTimeOffset startedAt, string content = "", ChatContext? context = null)
    {
        if (context != null)
            context.ProviderResponse = cliResponse;

        var status = cliResponse.GetString("status") ?? "SUCCESS";
        var isError = status != "SUCCESS";
        var resultText = (cliResponse.GetString("response") ?? content).TrimEnd('\n');

        var usage = cliResponse.GetObject("usage");
        var inputTokens = usage?.GetLong("input_tokens") ?? 0;
        var outputTokens = usage?.GetLong("output_tokens") ?? 0;
        var totalTokens = usage?.GetLong("total_tokens") ?? (inputTokens + outputTokens);
        var usageOut = new JsonObject
        {
            ["prompt_tokens"] = inputTokens,
            ["completion_tokens"] = outputTokens,
            ["total_tokens"] = totalTokens,
        };
        if (usage?.GetLong("cache_read_tokens") is > 0 and var cached)
            usageOut["prompt_tokens_details"] = new JsonObject { ["cached_tokens"] = cached };
        if (usage?.GetLong("thinking_tokens") is > 0 and var reasoning)
            usageOut["completion_tokens_details"] = new JsonObject { ["reasoning_tokens"] = reasoning };

        var durationMs = cliResponse.GetDouble("duration_seconds") is { } secs
            ? (long)(secs * 1000)
            : (long)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;

        return new JsonObject
        {
            ["id"] = cliResponse.GetString("conversation_id") ?? "",
            ["object"] = "chat.completion",
            ["created"] = startedAt.ToUnixTimeSeconds(),
            ["model"] = chat.GetString("model") ?? "",
            ["choices"] = new JsonArray(new JsonObject
            {
                ["index"] = 0,
                ["message"] = new JsonObject
                {
                    ["role"] = "assistant",
                    ["content"] = resultText,
                },
                ["finish_reason"] = isError ? "error" : "stop",
            }),
            ["usage"] = usageOut,
            ["metadata"] = new JsonObject
            {
                ["duration"] = durationMs,
                ["pricing"] = "0/0",
            },
        };
    }
}

/// <summary>
/// Anthropic Provider using local `claude` CLI to make use of an existing Claude Code subscription
/// (port of AnthropicProviderCli in llms/extensions/providers/anthropic.py).
/// Falls back to <see cref="AnthropicProvider"/> HTTP API when `claude` is not in PATH and an API key is configured.
/// </summary>
public class AnthropicCliProvider : AnthropicProvider
{
    public override string Sdk => "@ai-sdk/anthropic-cli";

    protected virtual string? FindExecutable() => CliExecutable.FindInPath("claude");
    bool UseCli => FindExecutable() != null;

    public override void Populate(JsonObject kwargs)
    {
        base.Populate(kwargs);
        if (UseCli)
        {
            foreach (var model in Models.Values)
                model["tool_call"] = false;
        }
    }

    public override string? Validate()
    {
        if (UseCli)
            return null;
        if (!string.IsNullOrEmpty(ApiKey))
            return null;
        return $"Provider '{Name}' requires 'claude' in PATH or API Key {string.Join(", ", Env)}";
    }

    public override async Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        var executable = FindExecutable();
        if (executable == null)
            return await base.ChatAsync(chat, context).ConfigAwait();

        var requestedModel = chat.GetString("model") ?? "";
        var model = ProviderModel(requestedModel) ?? requestedModel;
        chat["model"] = model;
        chat = await ProcessChatAsync(chat, Id).ConfigAwait();

        var (systemPrompt, anthropicMessages) = ToAnthropicMessages(chat);

        JsonNode? lastUserContent = null;
        for (var i = anthropicMessages.Count - 1; i >= 0; i--)
        {
            if (anthropicMessages[i] is JsonObject m && m.GetString("role") == "user")
            {
                lastUserContent = m["content"]?.DeepClone();
                break;
            }
        }

        JsonArray contentBlocks;
        if (lastUserContent is JsonArray arr)
            contentBlocks = arr;
        else if (lastUserContent is JsonValue val && val.TryGetValue<string>(out var text) && text.Length > 0)
            contentBlocks = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = text });
        else
            contentBlocks = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = "" });

        if (anthropicMessages.Count > 1)
        {
            var historyParts = new List<string>();
            for (var i = 0; i < anthropicMessages.Count - 1; i++)
            {
                if (anthropicMessages[i] is not JsonObject msg)
                    continue;
                var role = msg.GetString("role") ?? "user";
                string? msgText = null;
                if (msg["content"] is JsonValue v && v.TryGetValue<string>(out var s))
                    msgText = s;
                else if (msg["content"] is JsonArray blocks)
                {
                    var textParts = blocks.OfType<JsonObject>()
                        .Where(b => b.GetString("type") == "text")
                        .Select(b => b.GetString("text") ?? "")
                        .Where(t => t.Length > 0)
                        .ToList();
                    if (textParts.Count > 0)
                        msgText = string.Join("\n", textParts);
                }
                if (!string.IsNullOrEmpty(msgText))
                    historyParts.Add($"[{role}]: {msgText}");
            }

            if (historyParts.Count > 0)
            {
                var historyContext = "Previous conversation:\n"
                    + string.Join("\n\n", historyParts)
                    + "\n\nContinue the conversation based on the above context.";
                contentBlocks.Insert(0, new JsonObject { ["type"] = "text", ["text"] = historyContext });
            }
        }

        var streamJsonMessage = new JsonObject
        {
            ["type"] = "user",
            ["message"] = new JsonObject
            {
                ["role"] = "user",
                ["content"] = contentBlocks,
            },
        };

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add("--input-format");
        startInfo.ArgumentList.Add("stream-json");
        startInfo.ArgumentList.Add("--output-format");
        startInfo.ArgumentList.Add("stream-json");
        startInfo.ArgumentList.Add("--verbose");
        startInfo.ArgumentList.Add("--no-session-persistence");

        if (!string.IsNullOrEmpty(model))
        {
            startInfo.ArgumentList.Add("--model");
            startInfo.ArgumentList.Add(model);
        }
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            startInfo.ArgumentList.Add("--system-prompt");
            startInfo.ArgumentList.Add(systemPrompt);
        }

        var jsonInput = streamJsonMessage.ToJsonString(ChatJson.Options);
        Log.LogInformation("Running: claude -p --input-format stream-json --output-format stream-json --model {Model}", model);

        var startedAt = DateTimeOffset.UtcNow;
        using var process = new Process { StartInfo = startInfo };
        process.Start();
        try
        {
            await process.StandardInput.WriteAsync(jsonInput).ConfigAwait();
            await process.StandardInput.FlushAsync().ConfigAwait();
            process.StandardInput.Close();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(context.CancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(context.CancellationToken);
            await process.WaitForExitAsync(context.CancellationToken).ConfigAwait();

            var stdout = await stdoutTask.ConfigAwait();
            var stderr = (await stderrTask.ConfigAwait()).Trim();

            if (process.ExitCode != 0)
            {
                var err = stderr.Length > 0 ? stderr : stdout.Trim();
                throw new InvalidOperationException($"claude CLI error: {(err.Length > 0 ? err : $"claude exited with code {process.ExitCode}")}");
            }

            JsonObject? cliResponse = null;
            foreach (var rawLine in stdout.Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.Length == 0)
                    continue;
                if (line.EndsWith("\x1b[<u", StringComparison.Ordinal))
                    line = line[..^4].Trim();
                var obj = ChatJson.TryParseObject(line);
                if (obj?.GetString("type") == "result")
                {
                    cliResponse = obj;
                    break;
                }
            }

            if (cliResponse == null)
                throw new InvalidOperationException("No result found in claude CLI stream-json output");

            return ToCliResponse(cliResponse, chat, startedAt, context);
        }
        catch
        {
            CliExecutable.TryTerminate(process);
            throw;
        }
    }

    public JsonObject ToCliResponse(JsonObject cliResponse, JsonObject chat, DateTimeOffset startedAt, ChatContext? context = null)
    {
        if (context != null)
            context.ProviderResponse = cliResponse;

        var isError = cliResponse.GetBool("is_error");
        var resultText = cliResponse.GetString("result") ?? "";

        var usage = cliResponse.GetObject("usage");
        var inputTokens = (usage?.GetLong("input_tokens") ?? 0)
            + (usage?.GetLong("cache_creation_input_tokens") ?? 0)
            + (usage?.GetLong("cache_read_input_tokens") ?? 0);
        var outputTokens = usage?.GetLong("output_tokens") ?? 0;

        var metadata = new JsonObject
        {
            ["duration"] = cliResponse.GetLong("duration_ms") ?? (long)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds,
        };
        if (cliResponse.GetDouble("total_cost_usd") is { } costUsd)
            metadata["cost_usd"] = costUsd;

        var model = chat.GetString("model");
        if (!string.IsNullOrEmpty(model) && ModelCost(model) is { } cost
            && cost["input"] != null && cost["output"] != null)
        {
            metadata["pricing"] = $"{cost["input"]}/{cost["output"]}";
        }

        return new JsonObject
        {
            ["id"] = cliResponse.GetString("session_id") ?? "",
            ["object"] = "chat.completion",
            ["created"] = startedAt.ToUnixTimeSeconds(),
            ["model"] = model ?? "",
            ["choices"] = new JsonArray(new JsonObject
            {
                ["index"] = 0,
                ["message"] = new JsonObject
                {
                    ["role"] = "assistant",
                    ["content"] = resultText,
                },
                ["finish_reason"] = isError ? "error" : "stop",
            }),
            ["usage"] = new JsonObject
            {
                ["prompt_tokens"] = inputTokens,
                ["completion_tokens"] = outputTokens,
                ["total_tokens"] = inputTokens + outputTokens,
            },
            ["metadata"] = metadata,
        };
    }
}
