using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Built-in LLM tools (port of llms-py's "core_tools" extension): fetch_url, grep_search, get_current_time and calc
/// are always available; the run_* code-execution tools require the host to opt in via
/// ChatFeature.ToolsConfig.EnableCodeExecution (a web host is not a localhost sandbox).
/// </summary>
public class CoreToolsExtension() : ChatExtension("core_tools")
{
    public override void Install(ExtensionContext ctx)
    {
        const string group = "core_tools";

        ctx.RegisterTool(ToolDef("fetch_url",
                "Fetch content from a URL via HTTP request and convert HTML to clean, structured Markdown. Non-HTML content (JSON, plain text) is returned as raw text.",
                new JsonObject
                {
                    ["url"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "The HTTP/HTTPS URL to fetch content from",
                    },
                    ["max_length"] = new JsonObject
                    {
                        ["type"] = "integer",
                        ["description"] = "Maximum character length of content to return (default: 20000)",
                        ["default"] = 20000,
                    },
                }, required: ["url"]),
            async (args, c) => await FetchUrlAsync(args.GetString("url") ?? "", args.GetInt("max_length") ?? 20000, c.CancellationToken).ConfigAwait(),
            group);

        ctx.RegisterTool(ToolDef("grep_search",
                "Search for exact text or regex patterns across files within a directory tree. Returns matched file paths, line numbers, and matching line content.",
                new JsonObject
                {
                    ["query"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Text or regular expression to search for across files",
                    },
                    ["path"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Directory or file path to search (default is current working directory)",
                    },
                    ["is_regex"] = new JsonObject
                    {
                        ["type"] = "boolean",
                        ["description"] = "Whether to treat query as a regular expression (default: false)",
                        ["default"] = false,
                    },
                    ["case_sensitive"] = new JsonObject
                    {
                        ["type"] = "boolean",
                        ["description"] = "Whether the search is case-sensitive (default: false)",
                        ["default"] = false,
                    },
                    ["file_pattern"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Optional glob pattern to filter filenames (e.g. '*.py', '*.ts', '*.cs')",
                    },
                    ["max_matches"] = new JsonObject
                    {
                        ["type"] = "integer",
                        ["description"] = "Maximum number of matching lines to return (default: 50)",
                        ["default"] = 50,
                    },
                }, required: ["query"]),
            (args, c) => Task.FromResult<object?>(GrepSearch(
                args.GetString("query") ?? "",
                args.GetString("path"),
                args.GetBool("is_regex", false),
                args.GetBool("case_sensitive", false),
                args.GetString("file_pattern"),
                args.GetInt("max_matches") ?? 50,
                c)),
            group);

        ctx.RegisterTool(ToolDef("get_current_time", "Get current time in ISO-8601 format.",
                new JsonObject
                {
                    ["tz_name"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Optional timezone name (e.g. 'America/New_York'). Defaults to UTC.",
                    },
                }),
            (args, _) => Task.FromResult<object?>(GetCurrentTime(args.GetString("tz_name"))), group);

        ctx.RegisterTool(ToolDef("calc", "Evaluate a mathematical expression with boolean operations",
                new JsonObject
                {
                    ["expression"] = new JsonObject { ["type"] = "string" },
                }, required: ["expression"]),
            (args, _) => Task.FromResult<object?>(Calculator.Evaluate(args.GetString("expression") ?? "")), group);

        if (ctx.Tools.EnableCodeExecution)
        {
            RegisterRunTool(group, "run_python", "Execute Python code in a temporary sandboxed environment.", "python");
            RegisterRunTool(group, "run_typescript", "Execute TypeScript code in a temporary sandboxed environment using bun or tsx.", "typescript");
            RegisterRunTool(group, "run_javascript", "Execute JavaScript code in a temporary sandboxed environment using bun or node.", "javascript");
            RegisterRunTool(group, "run_csharp", "Execute C# code in a temporary sandboxed environment using dotnet.", "csharp");
        }

        ctx.AddPost("code/{language}/run", async req =>
        {
            if (!ctx.Tools.EnableCodeExecution)
                return ChatResult.Json(ChatJson.CreateErrorResponse(
                    "Code execution is disabled. Enable with ChatFeature.ToolsConfig.EnableCodeExecution", "Forbidden"), 403);

            var language = req.GetPathParam("language");
            var code = await req.Request.GetRawBodyAsync().ConfigAwait();
            var result = await ExecLanguageAsync(language, code ?? "").ConfigAwait();
            return result;
        });

        ctx.AddGet("calc", _ =>
        {
            string[] operators = ["+", "-", "*", "/", "%", "^", "==", "!=", "<", "<=", ">", ">=", "and", "or", "not"];
            return Task.FromResult<object?>(new JsonObject
            {
                ["numbers"] = new JsonArray("0", "1", "2", "3", "4", "5", "6", "7", "8", "9"),
                ["constants"] = new JsonArray(Calculator.Constants.Select(x => (JsonNode)x).ToArray()),
                ["operators"] = new JsonArray(operators.Select(x => (JsonNode)$" {x} ").ToArray()),
                ["functions"] = new JsonArray(Calculator.FunctionNames.Select(x => (JsonNode)x).ToArray()),
            });
        });

        ctx.AddPost("calc", async req =>
        {
            var code = await req.Request.GetRawBodyAsync().ConfigAwait();
            return new JsonObject { ["result"] = Calculator.Evaluate(code ?? "") };
        });

        // JSON -> typed classes / UI schema, used by the /code json tab and the pdf designer
        ctx.AddPost("schema", GenerateUiSchemaAsync);

        ctx.AddIndexFooter($"""

            <link rel="stylesheet" href="{ctx.ExtPrefix}/codemirror/codemirror.css">
            <link rel="stylesheet" href="{ctx.ExtPrefix}/codemirror/theme/mocha.css">
            <script src="{ctx.ExtPrefix}/codemirror/codemirror.js"></script>
            <script src="{ctx.ExtPrefix}/codemirror/mode/clike/clike.js"></script>
            <script src="{ctx.ExtPrefix}/codemirror/mode/javascript/javascript.js"></script>
            <script src="{ctx.ExtPrefix}/codemirror/mode/python/python.js"></script>
            <script src="{ctx.ExtPrefix}/codemirror/addon/edit/matchbrackets.js"></script>
            <script src="{ctx.ExtPrefix}/codemirror/addon/selection/active-line.js"></script>
            """);
    }

    // ── JSON -> JSON Schema generation ──

    /// <summary>The schema file that belongs to a data file: invoice.json -> invoice.ui.json</summary>
    public const string SchemaSuffix = ".ui.json";

    /// <summary>invoice.json / invoice.ui.json -> invoice</summary>
    public static string JsonStem(string? name)
    {
        var baseName = Path.GetFileName(name ?? "data.json");
        if (baseName.EndsWith(SchemaSuffix, StringComparison.OrdinalIgnoreCase))
            return baseName[..^SchemaSuffix.Length];
        var stem = Path.GetFileNameWithoutExtension(baseName);
        return stem.Length > 0 ? stem : "data";
    }

    /// <summary>Turn a JSON document into a JSON Schema that JsonSchemaForm renders</summary>
    async Task<object?> GenerateUiSchemaAsync(ChatRequestContext req)
    {
        var user = req.AssertUserName();
        var body = await req.GetJsonBodyAsync().ConfigAwait();

        // every codegen request carries the JSON document itself, so nothing touches the filesystem
        var name = body.GetString("name") ?? body.GetString("path") ?? "data.json";
        var content = body.GetString("content");
        var model = body.GetString("model");
        if (string.IsNullOrEmpty(model))
            throw new ArgumentException("No model selected");
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("No JSON content supplied");
        try
        {
            ChatJson.Parse(content!);
        }
        catch (System.Text.Json.JsonException e)
        {
            throw new ArgumentException($"'{Path.GetFileName(name)}' is not valid JSON: {e.Message}");
        }
        ModelPrompt.AssertTextModel(Feature, model!, "");

        var systemPrompt = Ctx.GetBundledText("prompts/generate-ui-schema.md")
            ?? throw new Exception("Missing prompts/generate-ui-schema.md");

        var outName = JsonStem(name) + SchemaSuffix;
        var (answer, usage) = await ModelPrompt.AskAsync(Feature, user, model!,
            ModelPrompt.Messages(systemPrompt,
                $"Data file: `{Path.GetFileName(name)}`\nSchema file: `{outName}`\n\n```json\n{content}\n```"),
            req.Request).ConfigAwait();

        var schemaText = ModelPrompt.FirstCodeBlock(answer);
        if (ChatJson.TryParseObject(schemaText) is not { } schema)
            throw new Exception("The model did not return valid JSON Schema");
        if (!schema.ContainsKey("properties"))
            throw new Exception("The model's schema has no 'properties'");

        return new JsonObject
        {
            ["path"] = outName,
            ["content"] = schema.ToJsonString(ChatJson.Indented) + "\n",
            ["model"] = model,
            ["usage"] = usage,
        };
    }

    static JsonObject ToolDef(string name, string description, JsonObject properties, string[]? required = null)
    {
        var parameters = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
        };
        if (required is { Length: > 0 })
            parameters["required"] = new JsonArray(required.Select(x => (JsonNode)x).ToArray());
        return new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = name,
                ["description"] = description,
                ["parameters"] = parameters,
            },
        };
    }

    void RegisterRunTool(string group, string name, string description, string language)
    {
        Ctx.RegisterTool(ToolDef(name, description,
                new JsonObject { ["code"] = new JsonObject { ["type"] = "string" } }, required: ["code"]),
            async (args, _) => await ExecLanguageAsync(language, args.GetString("code") ?? "").ConfigAwait(), group);
    }

    static string GetCurrentTime(string? tzName)
    {
        if (string.IsNullOrEmpty(tzName))
            return DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffffK");
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzName);
            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz).ToString("yyyy-MM-dd'T'HH:mm:ss.ffffffK");
        }
        catch (Exception)
        {
            return $"Error: Invalid timezone '{tzName}'";
        }
    }

    // ── Code execution (port of run_python/run_javascript/run_typescript/run_csharp) ──

    /// <summary>Max CPU seconds (ulimit -t) — the cap that actually bounds runaway code</summary>
    const int CpuSecondsLimit = 10;

    /// <summary>
    /// Max virtual address space in KB (ulimit -v). This bounds *reserved* address space rather than
    /// resident memory, and JIT runtimes reserve tens of GB of it on startup regardless of use —
    /// bun/JavaScriptCore aborts with SIGABRT (exit 134, no output) under an 8GB cap and node's tsx
    /// loader fails — so it has to stay generous. Python's ulimit -v 8589934592 is likewise ~8TB.
    /// </summary>
    const long VirtualMemoryLimitKb = 16L * 1024 * 1024; // 16GB

    async Task<JsonObject> ExecLanguageAsync(string language, string code)
    {
        var (runtime, fileName, argsFormat) = language switch
        {
            "python" => (Which("python3") ?? Which("python"), "script.py", "{0}"),
            "javascript" => (Which("bun") ?? Which("node"), "script.js", "{0}"),
            "typescript" => (Which("bun") ?? Which("tsx"), "script.ts", "{0}"),
            "csharp" => (Which("dotnet"), "script.cs", "run {0}"),
            _ => (null, "", ""),
        };
        if (fileName.Length == 0)
            return RunResult("", "Error: Invalid language", -1);
        if (runtime == null)
            return RunResult("", $"Error: No runtime available to run {language}", -1);

        var tempDir = Path.Combine(Path.GetTempPath(), "chat-run-" + Guid.NewGuid().ToString("n")[..8]);
        Directory.CreateDirectory(tempDir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, fileName), code).ConfigAwait();

            var psi = new ProcessStartInfo
            {
                WorkingDirectory = tempDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            // strip the environment except PATH (+ what dotnet needs to run out of a temp HOME)
            psi.EnvironmentVariables.Clear();
            psi.EnvironmentVariables["PATH"] = Environment.GetEnvironmentVariable("PATH") ?? "";
            psi.EnvironmentVariables["HOME"] = tempDir;
            psi.EnvironmentVariables["DOTNET_CLI_HOME"] = tempDir;
            psi.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

            var args = string.Format(argsFormat, fileName);
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                // ulimit-based resource caps, matching Python's sandboxing
                psi.FileName = "bash";
                psi.ArgumentList.Add("-c");
                psi.ArgumentList.Add($"ulimit -t {CpuSecondsLimit}; ulimit -v {VirtualMemoryLimitKb}; " +
                                     $"{EscapeArg(runtime)} {args}");
            }
            else
            {
                psi.FileName = runtime;
                foreach (var arg in args.Split(' '))
                    psi.ArgumentList.Add(arg);
            }

            using var process = new Process();
            process.StartInfo = psi;
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(language == "csharp" ? 60 : 10));
            try
            {
                await process.WaitForExitAsync(cts.Token).ConfigAwait();
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch (Exception) { /* already exited */ }
                return RunResult("", "Execution timed out", -1);
            }

            return RunResult(await stdoutTask.ConfigAwait(), await stderrTask.ConfigAwait(), process.ExitCode);
        }
        catch (Exception e)
        {
            Log.LogError(e, "Failed to execute {Language} code", language);
            return RunResult("", $"Error: {e.Message}", -1);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch (Exception) { /* best effort */ }
        }
    }

    static JsonObject RunResult(string stdout, string stderr, int returnCode) => new()
    {
        ["stdout"] = stdout,
        ["stderr"] = stderr,
        ["returncode"] = returnCode,
    };

    static string EscapeArg(string path) => path.Contains(' ') ? $"\"{path}\"" : path;

    static string? Which(string name)
    {
        var paths = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator);
        var extensions = OperatingSystem.IsWindows() ? new[] { ".exe", ".cmd", ".bat" } : [""];
        foreach (var dir in paths)
        {
            foreach (var ext in extensions)
            {
                var fullPath = Path.Combine(dir, name + ext);
                if (File.Exists(fullPath))
                    return fullPath;
            }
        }
        return null;
    }

    // ── Web / URL Fetching (port of fetch_url) ──

    private static readonly HttpClient SharedHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    async Task<object?> FetchUrlAsync(string url, int maxLength, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "Error: URL is required.";

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            req.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/json,text/plain;q=0.9,*/*;q=0.8");
            req.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.5");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(TimeSpan.FromSeconds(15));

            using var response = await SharedHttpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigAwait();
            response.EnsureSuccessStatusCode();

            var mediaType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "";
            using var stream = await response.Content.ReadAsStreamAsync(cts.Token).ConfigAwait();

            const int maxBytes = 2 * 1024 * 1024;
            var buffer = new byte[maxBytes];
            int totalRead = 0;
            while (totalRead < maxBytes)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(totalRead, maxBytes - totalRead), cts.Token).ConfigAwait();
                if (read == 0) break;
                totalRead += read;
            }

            var charset = response.Content.Headers.ContentType?.CharSet;
            Encoding encoding = Encoding.UTF8;
            if (!string.IsNullOrEmpty(charset))
            {
                try { encoding = Encoding.GetEncoding(charset); } catch { /* fallback to UTF8 */ }
            }

            var rawText = encoding.GetString(buffer, 0, totalRead);

            string content;
            if (mediaType.Contains("html") ||
                rawText.StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase) ||
                rawText.IndexOf("<html", 0, Math.Min(500, rawText.Length), StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var parser = new HtmlToMarkdownParser(url);
                content = parser.Parse(rawText);
            }
            else
            {
                content = rawText.Trim();
            }

            if (content.Length > maxLength)
            {
                var remaining = content.Length - maxLength;
                return content[..maxLength] + $"\n\n... [Truncated: {remaining} additional characters]";
            }

            return string.IsNullOrWhiteSpace(content) ? "No content found on page." : content;
        }
        catch (Exception e)
        {
            return $"Error fetching URL '{url}': {e.Message}";
        }
    }

    // ── File Content Search / Grep (port of grep_search) ──

    private static readonly HashSet<string> IgnoredSearchDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".venv", "venv", ".env", "node_modules", "__pycache__", "dist", "build", "bin", "obj", "target", "vendor", ".next", ".nuxt", ".cache", ".tox"
    };

    string GrepSearch(string query, string? path, bool isRegex, bool caseSensitive, string? filePattern, int maxMatches, ChatContext c)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "Error: query is required.";

        var searchDir = path;
        if (string.IsNullOrEmpty(searchDir))
        {
            var allowed = Ctx.ResolveAllowedDirectories(c.User);
            searchDir = allowed.Count > 0 ? allowed[0] : Directory.GetCurrentDirectory();
        }

        searchDir = Path.GetFullPath(Environment.ExpandEnvironmentVariables(searchDir));
        if (!Directory.Exists(searchDir) && !File.Exists(searchDir))
            return $"Error: Path '{searchDir}' does not exist.";

        Regex regex;
        try
        {
            var pattern = isRegex ? query : Regex.Escape(query);
            var options = RegexOptions.Compiled;
            if (!caseSensitive)
                options |= RegexOptions.IgnoreCase;
            regex = new Regex(pattern, options);
        }
        catch (Exception e)
        {
            return $"Error: Invalid regular expression '{query}': {e.Message}";
        }

        var matches = new List<string>();
        var baseDir = Directory.Exists(searchDir) ? searchDir : Path.GetDirectoryName(searchDir) ?? searchDir;

        void SearchFile(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                var header = new byte[Math.Min(1024, (int)stream.Length)];
                int read = stream.Read(header, 0, header.Length);
                if (header.AsSpan(0, read).IndexOf((byte)0) >= 0)
                    return; // skip binary file

                stream.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                int lineNo = 0;
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNo++;
                    if (regex.IsMatch(line))
                    {
                        var relPath = Path.GetRelativePath(baseDir, filePath);
                        matches.Add($"{relPath}:{lineNo}: {line.TrimEnd()}");
                        if (matches.Count >= maxMatches)
                            return;
                    }
                }
            }
            catch
            {
                // best effort, ignore unreadable/locked files
            }
        }

        if (File.Exists(searchDir))
        {
            SearchFile(searchDir);
        }
        else
        {
            SearchDirectory(searchDir, regex, filePattern, maxMatches, matches, baseDir, SearchFile);
        }

        if (matches.Count == 0)
            return $"No matches found for '{query}'.";

        var output = string.Join("\n", matches);
        if (matches.Count >= maxMatches)
            output += $"\n\n... [Capped at {maxMatches} matches]";
        return output;
    }

    static void SearchDirectory(string dirPath, Regex regex, string? filePattern, int maxMatches, List<string> matches, string baseDir, Action<string> searchFile)
    {
        var dirInfo = new DirectoryInfo(dirPath);
        try
        {
            foreach (var file in dirInfo.EnumerateFiles().OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(filePattern) && !PathMatchesPattern(file.Name, filePattern))
                    continue;

                searchFile(file.FullName);
                if (matches.Count >= maxMatches)
                    return;
            }

            foreach (var subDir in dirInfo.EnumerateDirectories().OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (subDir.Name.StartsWith('.') || IgnoredSearchDirs.Contains(subDir.Name))
                    continue;

                SearchDirectory(subDir.FullName, regex, filePattern, maxMatches, matches, baseDir, searchFile);
                if (matches.Count >= maxMatches)
                    return;
            }
        }
        catch
        {
            // Ignore access errors on protected directories
        }
    }

    static bool PathMatchesPattern(string filename, string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern == "*")
            return true;
        var regexPattern = "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
        return Regex.IsMatch(filename, regexPattern, RegexOptions.IgnoreCase);
    }
}
