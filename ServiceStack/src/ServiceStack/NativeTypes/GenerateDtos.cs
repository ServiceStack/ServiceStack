#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.Serialization;

#if NETCORE
using Microsoft.AspNetCore.Hosting.Server.Features;
#endif

namespace ServiceStack;

/// <summary>
/// Options for updating existing ServiceStack Reference DTO files from the current AppHost.
/// </summary>
public class GenerateDtosOptions
{
    /// <summary>
    /// Physical directory to scan. Defaults to the AppHost project content root.
    /// </summary>
    public string? Directory { get; set; }

    /// <summary>
    /// Directory names excluded from the recursive scan.
    /// </summary>
    public List<string> IgnoreDirectories { get; set; } =
    [
        ".git",
        ".vscode",
        ".idea",
        "node_modules",
        "bin",
        "obj",
        "dist",
        "build",
        ".venv",
        "packages",
        "gradle",
        "dart_tool",
        "vendor",
    ];

    /// <summary>
    /// Additional absolute URLs considered to belong to this AppHost.
    /// </summary>
    public List<string> BaseUrls { get; set; } = [];

    /// <summary>
    /// URLs used when the running AppHost URL cannot be determined.
    /// </summary>
    public List<string> FallbackBaseUrls { get; set; } =
    [
        "https://localhost:5001",
        "http://localhost:5000",
    ];

    /// <summary>
    /// Avoid writing files when only their generated Date header changed.
    /// </summary>
    public bool SkipUnchanged { get; set; } = true;
}

public class GenerateDtosResult
{
    public string Directory { get; internal set; } = string.Empty;
    public int Scanned { get; internal set; }
    public List<string> Updated { get; } = [];
    public List<string> Unchanged { get; } = [];
    public Dictionary<string, string> Skipped { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Errors { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public partial class NativeTypesFeature
{
    private static readonly ILog DtoLog = LogManager.GetLogger(typeof(NativeTypesFeature));

    private static readonly DtoLanguage[] DtoLanguages =
    [
        new("typescript.d", "dtos.d.ts", typeof(NativeTypes.TypesTypeScriptDefinition)),
        new("csharp", "dtos.cs", typeof(NativeTypes.TypesCSharp)),
        new("typescript", "dtos.ts", typeof(NativeTypes.TypesTypeScript)),
        new("mjs", "dtos.mjs", typeof(NativeTypes.TypesMjs)),
        new("python", "dtos.py", typeof(NativeTypes.TypesPython)),
        new("dart", "dtos.dart", typeof(NativeTypes.TypesDart)),
        new("php", "dtos.php", typeof(NativeTypes.TypesPhp)),
        new("java", "dtos.java", typeof(NativeTypes.TypesJava)),
        new("kotlin", "dtos.kt", typeof(NativeTypes.TypesKotlin)),
        new("swift", "dtos.swift", typeof(NativeTypes.TypesSwift)),
        new("fsharp", "dtos.fs", typeof(NativeTypes.TypesFSharp)),
        new("vbnet", "dtos.vb", typeof(NativeTypes.TypesVbNet)),
        new("go", "dtos.go", typeof(NativeTypes.TypesGo)),
        new("ruby", "dtos.rb", typeof(NativeTypes.TypesRuby)),
        new("rust", "dtos.rs", typeof(NativeTypes.TypesRust)),
        new("zig", "dtos.zig", typeof(NativeTypes.TypesZig)),
    ];

    private static readonly Regex GeneratedDateRegex = new(
        @"(?m)^(?<prefix>(?:/// |')?Date: )[^\r\n]*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Recursively updates existing dtos.* ServiceStack Reference files whose BaseUrl belongs
    /// to the current AppHost. Generation is performed in-process by NativeTypesService.
    /// </summary>
    public GenerateDtosResult GenerateDtos(GenerateDtosOptions? options = null)
    {
        options ??= new GenerateDtosOptions();

        var appHost = HostContext.AppHost
            ?? throw new InvalidOperationException("GenerateDtos requires an initialized AppHost");
        var directory = Path.GetFullPath(options.Directory
            ?? appHost.Config.WebHostPhysicalPath
            ?? appHost.MapProjectPath("~/"));

        if (!System.IO.Directory.Exists(directory))
            throw new DirectoryNotFoundException($"DTO scan directory does not exist: {directory}");

        var result = new GenerateDtosResult { Directory = directory };
        var hostUrls = GetHostUrls(appHost, options);
        var ignoredDirectories = new HashSet<string>(
            options.IgnoreDirectories ?? [], StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in FindDtoFiles(directory, ignoredDirectories, result))
        {
            result.Scanned++;
            try
            {
                var language = GetDtoLanguage(filePath)!;
                var existingSource = File.ReadAllText(filePath);
                var reference = ParseReference(existingSource, language.Name, filePath);

                if (!hostUrls.Any(x => UrlsMatch(reference.BaseUrl, x)))
                {
                    result.Skipped[filePath] = $"BaseUrl does not belong to this AppHost: {reference.BaseUrl}";
                    continue;
                }

                var generatedSource = GenerateDtoSource(appHost, language, reference);
                if (generatedSource.IndexOf("Options:", StringComparison.Ordinal) < 0)
                    throw new InvalidDataException($"Invalid NativeTypes response for {language.Name}");

                if (options.SkipUnchanged && SourcesEqualIgnoringDate(existingSource, generatedSource))
                {
                    result.Unchanged.Add(filePath);
                    continue;
                }

                ReplaceFile(filePath, generatedSource);
                result.Updated.Add(filePath);
                DtoLog.Info($"Updated DTOs: {filePath}");
            }
            catch (InvalidDataException e)
            {
                result.Skipped[filePath] = e.Message;
            }
            catch (Exception e)
            {
                result.Errors[filePath] = e.Message;
                DtoLog.Error($"Could not update DTOs: {filePath}", e);
            }
        }

        DtoLog.Info($"DTO generation complete: {result.Updated.Count} updated, " +
                    $"{result.Unchanged.Count} unchanged, {result.Skipped.Count} skipped, " +
                    $"{result.Errors.Count} failed");
        return result;
    }

    private List<string> GetHostUrls(IAppHost appHost, GenerateDtosOptions options)
    {
        var urls = new List<string>();

        AddUrl(urls, options.BaseUrls);
        AddUrl(urls, MetadataTypesConfig.BaseUrl);
        AddUrl(urls, appHost.Config.WebHostUrl);

#if NETCORE
        try
        {
            var addresses = appHost.GetApp().ServerFeatures
                .Get<IServerAddressesFeature>()?.Addresses;
            if (addresses != null)
            {
                foreach (var address in addresses)
                    AddUrl(urls, AppendPathBase(address, appHost.PathBase));
            }
        }
        catch (Exception e)
        {
            DtoLog.Debug($"Could not determine AppHost listening URLs: {e.Message}");
        }
#endif

        if (urls.Count == 0)
            AddUrl(urls, options.FallbackBaseUrls);

        return urls;
    }

    private static void AddUrl(List<string> urls, IEnumerable<string>? candidates)
    {
        if (candidates == null)
            return;
        foreach (var candidate in candidates)
            AddUrl(urls, candidate);
    }

    private static void AddUrl(List<string> urls, string? candidate)
    {
        if (!TryGetHttpUri(candidate, out var uri))
            return;

        var normalized = uri.GetLeftPart(UriPartial.Authority) + NormalizePath(uri.AbsolutePath);
        if (!urls.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            urls.Add(normalized);
    }

    private static string AppendPathBase(string address, string? pathBase)
    {
        if (string.IsNullOrEmpty(pathBase))
            return address;
        return address.TrimEnd('/') + "/" + pathBase.Trim('/');
    }

    private static IEnumerable<string> FindDtoFiles(
        string rootDirectory, HashSet<string> ignoredDirectories, GenerateDtosResult result)
    {
        var pending = new Stack<string>();
        pending.Push(rootDirectory);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            string[] files;
            string[] directories;
            try
            {
                files = System.IO.Directory.GetFiles(current);
                directories = System.IO.Directory.GetDirectories(current);
            }
            catch (Exception e)
            {
                result.Errors[current] = e.Message;
                continue;
            }

            foreach (var file in files.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                if (GetDtoLanguage(file) != null)
                    yield return file;
            }

            foreach (var child in directories.OrderByDescending(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var info = new DirectoryInfo(child);
                if (ignoredDirectories.Contains(info.Name) ||
                    (info.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                    continue;
                pending.Push(child);
            }
        }
    }

    private static DtoLanguage? GetDtoLanguage(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return DtoLanguages.FirstOrDefault(x =>
            fileName.EndsWith(x.Suffix, StringComparison.OrdinalIgnoreCase));
    }

    private static DtoReference ParseReference(string? source, string language, string filePath)
    {
        if (source == null)
            throw new InvalidDataException($"Source content is null: {filePath}");

        var startPos = source.IndexOf("Options:", StringComparison.Ordinal);
        if (startPos < 0)
            throw new InvalidDataException($"Not an existing ServiceStack Reference: {filePath}");

        var baseUrl = string.Empty;
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = source.Substring(startPos).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        foreach (var sourceLine in lines)
        {
            var line = sourceLine;
            if (line.StartsWith("*/", StringComparison.Ordinal) ||
                line.StartsWith("*)", StringComparison.Ordinal) ||
                line.StartsWith("\"\"\"", StringComparison.Ordinal))
                break;
            if (language == "zig" && line.Trim() == "///")
                break;

            if (language == "vbnet")
            {
                if (line.Trim().Length == 0)
                    break;
                if (line.Length > 0 && line[0] == '\'')
                    line = line.Substring(1);
            }
            if (language == "zig" && line.StartsWith("/// ", StringComparison.Ordinal))
                line = line.Substring(4);

            const string baseUrlPrefix = "BaseUrl: ";
            if (line.StartsWith(baseUrlPrefix, StringComparison.Ordinal))
            {
                baseUrl = line.Substring(baseUrlPrefix.Length);
            }
            else if (baseUrl.Length > 0 &&
                     !line.StartsWith("//", StringComparison.Ordinal) &&
                     !line.StartsWith("'", StringComparison.Ordinal) &&
                     !line.StartsWith("#", StringComparison.Ordinal))
            {
                var colonPos = line.IndexOf(':');
                if (colonPos >= 0)
                    options[line.Substring(0, colonPos).Trim()] = line.Substring(colonPos + 1).Trim();
            }
        }

        if (!TryGetHttpUri(baseUrl, out _))
            throw new InvalidDataException($"Could not find a valid BaseUrl in {filePath}");

        return new DtoReference(baseUrl, options);
    }

    private static string GenerateDtoSource(IAppHost appHost, DtoLanguage language, DtoReference reference)
    {
        var requestDto = (NativeTypes.NativeTypesBase)KeyValueDataContractDeserializer.Instance
            .Parse(reference.Options, language.RequestType);
        requestDto.BaseUrl = reference.BaseUrl;

        // Avoid optimized-result caching if this API is called explicitly outside DebugMode.
        if (requestDto is NativeTypes.TypesMjs mjs)
            mjs.Cache = false;

        var typesUrl = reference.BaseUrl.TrimEnd('/') + "/types/" + language.Name;
        var request = new BasicHttpRequest(requestDto)
        {
            Resolver = appHost,
            Verb = HttpMethods.Get,
            HttpMethod = HttpMethods.Get,
            PathInfo = "/types/" + language.Name,
            AbsoluteUri = typesUrl,
            RawUrl = typesUrl,
            UserHostAddress = IPAddress.IPv6Loopback.ToString(),
            RemoteIp = IPAddress.IPv6Loopback.ToString(),
            IsSecureConnection = reference.BaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase),
            ResponseContentType = MimeTypes.PlainText,
        };

        foreach (var entry in reference.Options)
            request.QueryString[entry.Key] = entry.Value;

        var response = appHost is ServiceStackHost ssh 
            ? ssh.ExecuteService(requestDto, request)
            : HostContext.ServiceController.Execute(requestDto, request);
        return response as string
            ?? throw new InvalidDataException($"NativeTypesService returned {response?.GetType().Name ?? "null"}");
    }

    private static bool SourcesEqualIgnoringDate(string existingSource, string generatedSource) =>
        GeneratedDateRegex.Replace(existingSource, m => m.Groups["prefix"].Value) ==
        GeneratedDateRegex.Replace(generatedSource, m => m.Groups["prefix"].Value);

    private static void ReplaceFile(string filePath, string contents)
    {
        var tempFile = filePath + ".tmp." + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(tempFile, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
#if NETCORE
            File.Move(tempFile, filePath, overwrite: true);
#else
            File.Replace(tempFile, filePath, null);
#endif
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    private static bool UrlsMatch(string left, string right)
    {
        if (!TryGetHttpUri(left, out var leftUri) || !TryGetHttpUri(right, out var rightUri))
            return false;
        if (!leftUri.Scheme.Equals(rightUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
            leftUri.Port != rightUri.Port ||
            !NormalizePath(leftUri.AbsolutePath).Equals(NormalizePath(rightUri.AbsolutePath), StringComparison.Ordinal))
            return false;

        if (leftUri.Host.Equals(rightUri.Host, StringComparison.OrdinalIgnoreCase))
            return true;
        if (IsLoopbackHost(leftUri.Host) && IsLoopbackHost(rightUri.Host))
            return true;
        return IsLoopbackHost(leftUri.Host) && IsWildcardHost(rightUri.Host) ||
               IsWildcardHost(leftUri.Host) && IsLoopbackHost(rightUri.Host);
    }

    private static bool TryGetHttpUri(string? value, out Uri uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out uri!) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return true;
        uri = null!;
        return false;
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.TrimEnd('/');
        return normalized == "/" ? string.Empty : normalized;
    }

    private static bool IsLoopbackHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        host == "127.0.0.1" || host == "::1" || host == "[::1]";

    private static bool IsWildcardHost(string host) =>
        host == "0.0.0.0" || host == "::" || host == "[::]" || host == "*" || host == "+";

    private sealed class DtoLanguage(string name, string suffix, Type requestType)
    {
        public string Name { get; } = name;
        public string Suffix { get; } = suffix;
        public Type RequestType { get; } = requestType;
    }

    private sealed class DtoReference(string baseUrl, Dictionary<string, string> options)
    {
        public string BaseUrl { get; } = baseUrl;
        public Dictionary<string, string> Options { get; } = options;
    }
}
