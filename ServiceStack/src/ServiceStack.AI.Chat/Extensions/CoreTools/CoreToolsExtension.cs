using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json.Nodes;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Built-in LLM tools (port of llms-py's "core_tools" extension): get_current_time and calc are
/// always available; the run_* code-execution tools require the host to opt in via
/// ChatFeature.ToolsConfig.EnableCodeExecution (a web host is not a localhost sandbox).
/// </summary>
public class CoreToolsExtension() : ChatExtension("core_tools")
{
    public override void Install(ExtensionContext ctx)
    {
        const string group = "core_tools";

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
}
