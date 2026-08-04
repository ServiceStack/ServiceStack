using System.Diagnostics;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Renders the published typst templates in App_Data/pdf.
/// <para>
/// Data reaches a template through typst's <c>--input data=&lt;json&gt;</c>, which lib.typ's
/// <c>load-data()</c> reads before falling back to the template's .json sidecar — so nothing is
/// written to disk and concurrent renders of the same template can't clobber each other.
/// </para>
/// </summary>
public interface IPdfRenderer
{
    /// <summary>Whether the typst CLI was found — false disables rendering, not listing</summary>
    bool IsAvailable { get; }

    /// <summary>Published template names, without the .typ (e.g. "invoice")</summary>
    List<string> GetTemplateNames();

    /// <summary>Absolute path of a published file, throwing if the name escapes App_Data/pdf</summary>
    string ResolvePath(string name, string ext = ".typ", bool mustExist = true);

    /// <summary>Compile a published template to PDF. A null dataJson uses the template's own .json</summary>
    Task<byte[]> RenderAsync(string name, string? dataJson = null, CancellationToken token = default);

    /// <summary>Compile with any object or dictionary, serialised to camelCase JSON for the template</summary>
    Task<byte[]> RenderAsync(string name, object? data, CancellationToken token = default);

    /// <summary>Rasterise one page to PNG, used for the &lt;name&gt;.preview.png thumbnails</summary>
    Task<byte[]> RenderPngAsync(string name, string? dataJson = null, int page = 1, int? ppi = null,
        CancellationToken token = default);
}

/// <summary>A typst compile that failed, carrying what typst said about it</summary>
public class PdfRenderException(string message) : Exception(message)
{
    /// <summary>Raw typst stderr, for showing the author the actual compile errors</summary>
    public string? Diagnostics { get; set; }
    public int ExitCode { get; set; }
}

public partial class PdfRenderer(PdfFeature feature) : IPdfRenderer
{
    /// <summary>
    /// Published names are flat, so a valid one has no separators at all — which is also what keeps
    /// "..", "/" and absolute paths out of <see cref="ResolvePath"/>.
    /// </summary>
    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    private static partial Regex ValidNameRegex();

    /// <summary>Names reserved for the shared library, which isn't a document</summary>
    public static readonly string[] ReservedNames = ["lib", "lib.preview"];

    /// <summary>Bounds how many typst processes a burst of requests can fork</summary>
    readonly SemaphoreSlim renderLock = new(Math.Max(1, feature.MaxConcurrentRenders));

    public bool IsAvailable => !string.IsNullOrEmpty(feature.TypstPath);

    public List<string> GetTemplateNames()
    {
        var root = feature.PdfPath;
        if (root == null || !Directory.Exists(root))
            return [];
        return Directory.EnumerateFiles(root, "*.typ")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(x => !ReservedNames.Contains(x, StringComparer.OrdinalIgnoreCase))
            .Distinct()
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string ResolvePath(string name, string ext = ".typ", bool mustExist = true)
    {
        var stem = name ?? "";
        // Do not normalise a path into a valid name: callers must never be able to smuggle
        // traversal segments through an API that happens to only use the final file name.
        if (stem.Contains('/') || stem.Contains('\\'))
            throw new ArgumentException($"Invalid template name '{name}'");
        if (stem.EndsWith(".typ", StringComparison.OrdinalIgnoreCase))
            stem = stem[..^".typ".Length];
        if (!ValidNameRegex().IsMatch(stem))
            throw new ArgumentException($"Invalid template name '{name}'");

        var root = feature.PdfPath
            ?? throw new Exception($"{nameof(PdfFeature)}.{nameof(PdfFeature.PdfPath)} is not configured");
        var fullPath = Path.Combine(root, stem + ext);
        // ValidName already rules out traversal; this is the belt to its braces
        if (!PdfExtension.IsSafePath(root, fullPath))
            throw new ArgumentException($"Invalid template name '{name}'");
        if (mustExist && !File.Exists(fullPath))
            throw new FileNotFoundException($"PDF template '{stem}{ext}' does not exist", fullPath);
        return fullPath;
    }

    /// <summary>The template name callers may render, i.e. not the shared library</summary>
    public string AssertTemplatePath(string name, bool mustExist = true)
    {
        var stem = (name ?? "").EndsWith(".typ", StringComparison.OrdinalIgnoreCase)
            ? name![..^".typ".Length]
            : name ?? "";
        if (ReservedNames.Contains(stem, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"'{stem}' is the shared library, not a template");
        return ResolvePath(name!, ".typ", mustExist);
    }

    public Task<byte[]> RenderAsync(string name, object? data, CancellationToken token = default) =>
        RenderAsync(name, data != null ? ChatJson.Serialize(data) : null, token);

    public Task<byte[]> RenderAsync(string name, string? dataJson = null, CancellationToken token = default) =>
        CompileAsync(name, dataJson, png: false, page: 1, ppi: null, feature.RenderTimeout, token);

    public Task<byte[]> RenderPngAsync(string name, string? dataJson = null, int page = 1, int? ppi = null,
        CancellationToken token = default) =>
        CompileAsync(name, dataJson, png: true, page, ppi ?? feature.PreviewPpi, feature.PreviewTimeout, token);

    async Task<byte[]> CompileAsync(string name, string? dataJson, bool png, int page, int? ppi,
        TimeSpan timeout, CancellationToken token)
    {
        if (!IsAvailable)
            throw new PdfRenderException("typst is not installed — install it from https://typst.app");
        if (dataJson != null && dataJson.Length > feature.MaxDataBytes)
            throw new ArgumentException(
                $"Data is too large: {dataJson.Length / 1024}KB, at most {feature.MaxDataBytes / 1024}KB");

        var templatePath = AssertTemplatePath(name);
        var root = feature.PdfPath!;

        var psi = new ProcessStartInfo(feature.TypstPath!)
        {
            WorkingDirectory = root,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add("compile");
        psi.ArgumentList.Add("--root");
        psi.ArgumentList.Add(root);
        if (dataJson != null)
        {
            // through ArgumentList, never a joined command line — the JSON must not be shell-parsed
            psi.ArgumentList.Add("--input");
            psi.ArgumentList.Add("data=" + dataJson);
        }
        var fontsDir = Path.Combine(root, "fonts");
        if (Directory.Exists(fontsDir))
        {
            psi.ArgumentList.Add("--font-path");
            psi.ArgumentList.Add(fontsDir);
        }
        if (png)
        {
            psi.ArgumentList.Add("--pages");
            psi.ArgumentList.Add(page.ToString());
            psi.ArgumentList.Add("-f");
            psi.ArgumentList.Add("png");
            psi.ArgumentList.Add("--ppi");
            psi.ArgumentList.Add((ppi ?? 96).ToString());
        }
        psi.ArgumentList.Add("--diagnostic-format");
        psi.ArgumentList.Add("short");
        psi.ArgumentList.Add(templatePath);
        psi.ArgumentList.Add("-"); // write to stdout

        await renderLock.WaitAsync(token).ConfigAwait();
        try
        {
            return await RunAsync(psi, name, timeout, token).ConfigAwait();
        }
        finally
        {
            renderLock.Release();
        }
    }

    static async Task<byte[]> RunAsync(ProcessStartInfo psi, string name, TimeSpan timeout, CancellationToken token)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(timeout);

        using var process = Process.Start(psi)
            ?? throw new PdfRenderException($"Could not start '{psi.FileName}'");
        try
        {
            using var stdout = new MemoryStream();
            // stderr is drained concurrently: a diagnostic large enough to fill its pipe would
            // otherwise deadlock both sides
            var stdoutTask = process.StandardOutput.BaseStream.CopyToAsync(stdout, cts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);
            await Task.WhenAll(stdoutTask, stderrTask).ConfigAwait();
            await process.WaitForExitAsync(cts.Token).ConfigAwait();

            var stderr = await stderrTask.ConfigAwait();
            if (process.ExitCode != 0 || stdout.Length == 0)
            {
                throw new PdfRenderException(FirstError(stderr) ?? $"typst compile {name} failed")
                {
                    Diagnostics = stderr,
                    ExitCode = process.ExitCode,
                };
            }
            return stdout.ToArray();
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            Kill(process);
            throw new TimeoutException($"typst compile {name} timed out after {timeout.TotalSeconds:0}s");
        }
        catch (Exception)
        {
            Kill(process);
            throw;
        }
    }

    static void Kill(Process process)
    {
        try { process.Kill(entireProcessTree: true); } catch (Exception) { /* already exited */ }
    }

    /// <summary>The first line typst complained about, for an error message a human can act on</summary>
    static string? FirstError(string? stderr) => stderr?
        .Split('\n')
        .Select(x => x.Trim())
        .FirstOrDefault(x => x.Length > 0);
}
