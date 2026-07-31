using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Anthropic-style skills (port of llms-py's "skills" extension): packaged instruction folders
/// with a SKILL.md manifest. Shared skills live under App_Data/chat/.agent/skills (seeded from
/// the bundled set on first run); per-user skills under App_Data/chat/user/&lt;user&gt;/skills.
/// The localhost-only roots (~/.claude/skills, ./.agent/skills) are not scanned on a web host.
/// </summary>
public partial class SkillsExtension() : ChatExtension("skills")
{
    JsonArray availableSkills = [];

    string HomeSkillsPath => Ctx.GetHomePath(Path.Combine(".agent", "skills"));
    string UserSkillsPath(string user) => Path.Combine(Ctx.GetUserPath(user), "skills");

    public override void Install(ExtensionContext ctx)
    {
        SeedBundledSkills();
        LoadAvailableSkills();

        ctx.AddGet("", req => Task.FromResult<object?>(ResolveAllSkills(req.UserName)));
        ctx.AddGet("search", req => Task.FromResult<object?>(SearchSkills(req)));
        ctx.AddPost("install/{id}", InstallSkillAsync);

        ctx.AddGet("contents/{name}", req =>
        {
            var content = SkillTool(req.GetPathParam("name"), req.QueryString("file"), req.UserName);
            return Task.FromResult<object?>(new ChatResult { Text = content, ContentType = MimeTypes.PlainText });
        });

        ctx.AddGet("file/{name}/{path:.*}", req =>
        {
            var (skillInfo, _) = AssertSkill(req.GetPathParam("name"), req.UserName);
            var filePath = req.GetPathParam("path");
            var fullPath = SafeSkillFile(skillInfo, filePath);
            if (!File.Exists(fullPath))
                throw new Exception($"File '{filePath}' not found");
            return Task.FromResult<object?>(new JsonObject
            {
                ["content"] = File.ReadAllText(fullPath),
                ["path"] = filePath,
            });
        });

        ctx.AddPost("file/{name}", async req =>
        {
            var name = req.GetPathParam("name");
            var data = await req.GetJsonBodyAsync().ConfigAwait();
            var filePath = data.GetString("path");
            var content = data.GetString("content");
            if (filePath == null || content == null)
                throw new Exception("Missing 'path' or 'content' in request body");

            var user = req.UserName;
            var (skillInfo, _) = AssertSkill(name, user);
            AssertValidLocation(skillInfo.GetString("location")!, user);

            var fullPath = SafeSkillFile(skillInfo, filePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, content).ConfigAwait();

            return new JsonObject
            {
                ["path"] = filePath,
                ["skill"] = ResolveAllSkills(user).GetObject(name)?.Clone(),
            };
        });

        ctx.AddDelete("file/{name}", req =>
        {
            var name = req.GetPathParam("name");
            var filePath = req.QueryString("path")
                ?? throw new Exception("Missing 'path' query parameter");
            if (filePath.EqualsIgnoreCase("skill.md"))
                throw new Exception("Cannot delete SKILL.md - delete the entire skill instead");

            var user = req.UserName;
            var (skillInfo, _) = AssertSkill(name, user);
            var location = skillInfo.GetString("location")!;
            AssertValidLocation(location, user);

            var fullPath = SafeSkillFile(skillInfo, filePath);
            if (!File.Exists(fullPath))
                throw new Exception($"File '{filePath}' not found");
            File.Delete(fullPath);

            // prune empty parent dirs up to the skill root
            var parent = Path.GetDirectoryName(fullPath);
            while (parent != null && parent != location
                && Directory.Exists(parent) && !Directory.EnumerateFileSystemEntries(parent).Any())
            {
                Directory.Delete(parent);
                parent = Path.GetDirectoryName(parent);
            }

            return Task.FromResult<object?>(new JsonObject
            {
                ["path"] = filePath,
                ["skill"] = ResolveAllSkills(user).GetObject(name)?.Clone(),
            });
        });

        ctx.AddPost("create", async req =>
        {
            var data = await req.GetJsonBodyAsync().ConfigAwait();
            var skillName = data.GetString("name")
                ?? throw new Exception("Missing 'name' in request body");
            if (!Regex.IsMatch(skillName, "^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$"))
                throw new Exception("Skill name must be lowercase, use hyphens, start/end with alphanumeric");
            if (skillName.Length > 40)
                throw new Exception("Skill name must be 40 characters or less");

            var user = req.UserName;
            var writePath = ResolveSkillsWritePath(user);
            var skillDir = Path.Combine(writePath, skillName);
            if (Directory.Exists(skillDir))
                throw new Exception($"Skill '{skillName}' already exists");

            // create from a minimal template (Python shells out to skill-creator's init_skill.py)
            Directory.CreateDirectory(skillDir);
            await File.WriteAllTextAsync(Path.Combine(skillDir, "SKILL.md"),
                $"""
                ---
                name: {skillName}
                description: TODO - describe when this skill should be used
                ---

                # {skillName}

                TODO - instructions for this skill
                """).ConfigAwait();

            return new JsonObject
            {
                ["skill"] = ResolveAllSkills(user).GetObject(skillName)?.Clone(),
                ["output"] = $"Created skill '{skillName}'",
            };
        });

        ctx.AddDelete("skill/{name}", req =>
        {
            var name = req.GetPathParam("name");
            var user = req.UserName;
            var skillInfo = ResolveAllSkills(user).GetObject(name);
            var location = skillInfo?.GetString("location")
                ?? (Directory.Exists(Path.Combine(HomeSkillsPath, name))
                    ? Path.Combine(HomeSkillsPath, name)
                    : throw new Exception($"Skill '{name}' not found"));

            AssertValidLocation(location, user);
            if (Directory.Exists(location))
                Directory.Delete(location, recursive: true);
            return Task.FromResult<object?>(new JsonObject { ["deleted"] = name });
        });

        // the "skill" tool lets the LLM load skill instructions on demand
        ctx.RegisterTool(new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = "skill",
                ["description"] = "Get the content of a skill or a specific file within a skill.",
                ["parameters"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["name"] = new JsonObject { ["type"] = "string", ["description"] = "skill name" },
                        ["file"] = new JsonObject { ["type"] = "string", ["description"] = "skill file" },
                        ["user"] = new JsonObject { ["type"] = "string" },
                    },
                    ["required"] = new JsonArray("name"),
                },
            },
        }, (args, c) => Task.FromResult<object?>(
            SkillTool(args.GetString("name") ?? "", args.GetString("file"), args.GetString("user") ?? c.User)),
            "core_tools");
    }

    /// <summary>Copy the bundled skills (synced from llms-py) into App_Data on first run</summary>
    void SeedBundledSkills()
    {
        if (Directory.Exists(HomeSkillsPath))
            return;
        Log.LogInformation("Creating initial skills folder: {Path}", HomeSkillsPath);
        var bundled = HostContext.VirtualFileSources.GetDirectory("chat/ext/skills/skills");
        if (bundled == null)
            return;
        foreach (var file in bundled.GetAllMatchingFiles("*"))
        {
            // VirtualPath is like chat/ext/skills/skills/<skill>/<file>
            var relativePath = file.VirtualPath["chat/ext/skills/skills/".Length..];
            var targetPath = Path.Combine(HomeSkillsPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.WriteAllBytes(targetPath, file.ReadAllBytes());
        }
    }

    void LoadAvailableSkills()
    {
        var file = HostContext.VirtualFileSources.GetFile("chat/ext/skills/data/skills-top-5000.json");
        if (file != null && ChatJson.TryParseObject(file.ReadAllText())?.GetArray("skills") is { } skills)
        {
            availableSkills = skills;
        }
    }

    JsonObject SearchSkills(ChatRequestContext req)
    {
        var q = (req.QueryString("q") ?? "").ToLowerInvariant();
        var limit = int.TryParse(req.QueryString("limit"), out var l) ? l : 50;
        var offset = int.TryParse(req.QueryString("offset"), out var o) ? o : 0;

        var filtered = availableSkills.OfType<JsonObject>()
            .Where(s => (s.GetString("name") ?? "").Contains(q) || (s.GetString("topSource") ?? "").Contains(q))
            .OrderByDescending(s => s.GetLong("installs") ?? 0)
            .ToList();

        var results = new JsonArray();
        foreach (var skill in filtered.Skip(offset).Take(limit))
        {
            results.Add(skill.Clone());
        }
        return new JsonObject { ["results"] = results, ["total"] = filtered.Count };
    }

    /// <summary>Install a skill from its source GitHub repo via a shallow git clone</summary>
    async Task<object?> InstallSkillAsync(ChatRequestContext req)
    {
        var id = req.GetPathParam("id");
        var skill = availableSkills.OfType<JsonObject>().FirstOrDefault(s => s.GetString("id") == id)
            ?? throw new Exception($"Skill '{id}' not found");
        var source = skill.GetString("topSource")
            ?? throw new Exception($"Skill '{id}' has no source repository");

        var writePath = ResolveSkillsWritePath(req.UserName);
        Log.LogInformation("Installing skill '{Id}' from '{Source}' to '{Path}'", id, source, writePath);

        var tempDir = Path.Combine(Path.GetTempPath(), "skill-install-" + Guid.NewGuid().ToString("n")[..8]);
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            foreach (var arg in new[] { "clone", "--depth", "1", $"https://github.com/{source}.git", tempDir })
                psi.ArgumentList.Add(arg);

            using var process = Process.Start(psi)!;
            var stderrTask = process.StandardError.ReadToEndAsync();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            await process.WaitForExitAsync(cts.Token).ConfigAwait();
            if (process.ExitCode != 0)
                throw new Exception($"git clone failed: {await stderrTask.ConfigAwait()}");

            // find the skill folder (a dir named <id> containing SKILL.md) anywhere in the repo
            var skillDir = Directory.EnumerateDirectories(tempDir, id, SearchOption.AllDirectories)
                .FirstOrDefault(d => File.Exists(Path.Combine(d, "SKILL.md")) || File.Exists(Path.Combine(d, "skill.md")));
            if (skillDir == null)
                throw new Exception($"Skill '{id}' not found in {source}");

            var targetDir = Path.Combine(writePath, id);
            if (Directory.Exists(targetDir))
                Directory.Delete(targetDir, recursive: true);
            CopyDirectory(skillDir, targetDir);

            return new JsonObject
            {
                ["success"] = true,
                ["installed"] = new JsonArray(id),
            };
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch (Exception) { /* best effort */ }
        }
    }

    static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(targetDir, Path.GetRelativePath(sourceDir, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    // ── Skill resolution ──

    string ResolveSkillsWritePath(string? user)
    {
        var path = user != null ? UserSkillsPath(user) : HomeSkillsPath;
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>All visible skills keyed by name (port of resolve_all_skills)</summary>
    JsonObject ResolveAllSkills(string? user)
    {
        var skillRoots = new Dictionary<string, string>
        {
            ["~/.llms/.agent/skills"] = HomeSkillsPath,
        };
        if (user != null && Directory.Exists(UserSkillsPath(user)))
        {
            skillRoots[$"{user}/skills"] = UserSkillsPath(user);
        }

        var ret = new JsonObject();
        foreach (var (group, root) in skillRoots)
        {
            if (!Directory.Exists(root))
                continue;
            foreach (var entryPath in Directory.EnumerateDirectories(root))
            {
                var skillMd = File.Exists(Path.Combine(entryPath, "SKILL.md")) ? Path.Combine(entryPath, "SKILL.md")
                    : File.Exists(Path.Combine(entryPath, "skill.md")) ? Path.Combine(entryPath, "skill.md")
                    : null;
                if (skillMd == null)
                    continue;
                try
                {
                    var skillDir = Path.GetFullPath(entryPath);
                    var props = ReadSkillProperties(skillMd, Path.GetFileName(entryPath));

                    var files = new JsonArray();
                    foreach (var file in Directory.EnumerateFiles(skillDir, "*", SearchOption.AllDirectories))
                    {
                        files.Add(Path.GetRelativePath(skillDir, file));
                    }

                    var writable = Ctx.IsAuthEnabled
                        ? user != null && IsSafePath(UserSkillsPath(user), skillDir)
                        : IsSafePath(HomeSkillsPath, skillDir);

                    props["group"] = group;
                    props["location"] = skillDir;
                    props["files"] = files;
                    props["writable"] = writable;
                    ret[props.GetString("name") ?? Path.GetFileName(entryPath)] = props;
                }
                catch (Exception e)
                {
                    Log.LogInformation("Failed to load skill {Name}: {Message}",
                        Path.GetFileName(entryPath), e.Message);
                }
            }
        }
        return ret;
    }

    /// <summary>Parse SKILL.md YAML frontmatter (name, description, license, allowed-tools, metadata.*)</summary>
    static JsonObject ReadSkillProperties(string skillMdPath, string dirName)
    {
        var props = new JsonObject { ["name"] = dirName, ["description"] = "" };
        var lines = File.ReadLines(skillMdPath).ToList();
        if (lines.Count == 0 || lines[0].Trim() != "---")
            return props;

        foreach (var line in lines.Skip(1))
        {
            if (line.Trim() == "---")
                break;
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#') || !trimmed.Contains(':'))
                continue;
            var key = trimmed.LeftPart(':').Trim();
            var val = trimmed.RightPart(':').Trim().Trim('"', '\'');
            if (val.Length > 0)
                props[key] = val;
        }
        return props;
    }

    (JsonObject SkillInfo, string Location) AssertSkill(string name, string? user)
    {
        var skillInfo = ResolveAllSkills(user).GetObject(name)
            ?? throw new Exception($"Skill '{name}' not found");
        return (skillInfo, skillInfo.GetString("location")!);
    }

    string SafeSkillFile(JsonObject skillInfo, string filePath)
    {
        var location = skillInfo.GetString("location")!;
        var fullPath = Path.GetFullPath(Path.Combine(location, filePath));
        if (!IsSafePath(location, fullPath))
            throw new Exception("Invalid file path");
        return fullPath;
    }

    void AssertValidLocation(string location, string? user)
    {
        if (Ctx.IsAuthEnabled && user == null)
            throw new UnauthorizedAccessException("Unauthorized");
        if (user != null)
        {
            if (!IsSafePath(ResolveSkillsWritePath(user), location))
                throw new Exception("Cannot modify skills outside of allowed user directory");
            return;
        }
        if (!IsSafePath(HomeSkillsPath, location))
            throw new Exception("Cannot modify skills outside of allowed directories");
    }

    static bool IsSafePath(string basePath, string requestedPath)
    {
        var baseFull = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar);
        var targetFull = Path.GetFullPath(requestedPath);
        return targetFull == baseFull
            || targetFull.StartsWith(baseFull + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    static string Sanitize(string name) =>
        name.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();

    /// <summary>The "skill" tool + contents/{name} handler (port of the skill() function)</summary>
    string SkillTool(string name, string? file, string? user)
    {
        var skills = ResolveAllSkills(user);
        var skill = skills.GetObject(name);
        if (skill == null)
        {
            var sanitized = Sanitize(name);
            skill = skills.FirstOrDefault(entry => Sanitize(entry.Key) == sanitized).Value as JsonObject;
        }
        if (skill == null)
            return $"Error: Skill {name} not found. Available skills: {string.Join(", ", skills.Select(x => x.Key))}";

        var location = skill.GetString("location");
        if (location == null || !Directory.Exists(location))
            return $"Error: Skill {name} not found at location {location}";

        if (!string.IsNullOrEmpty(file))
        {
            if (file.StartsWith(location, StringComparison.Ordinal))
                file = file[(location.Length + 1)..];
            var filePath = Path.GetFullPath(Path.Combine(location, file));
            if (!IsSafePath(location, filePath) || !File.Exists(filePath))
            {
                var files = skill.GetArray("files")?.Select(x => x?.GetValue<string>()) ?? [];
                return $"Error: File {file} not found in skill {name}. Available files: {string.Join(", ", files)}";
            }
            return File.ReadAllText(filePath);
        }

        var skillMd = File.Exists(Path.Combine(location, "SKILL.md"))
            ? Path.Combine(location, "SKILL.md")
            : Path.Combine(location, "skill.md");
        var content = File.ReadAllText(skillMd);
        if (skill.GetArray("files") is { Count: > 1 } allFiles)
        {
            content += "\n\n## Skill Files:\n```\n"
                + string.Join("\n", allFiles.Select(x => x?.GetValue<string>()))
                + "\n```";
        }
        return content;
    }
}
