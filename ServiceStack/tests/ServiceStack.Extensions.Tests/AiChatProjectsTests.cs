#nullable enable
using System;
using System.IO;
using System.Text.Json.Nodes;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.Extensions.Tests;

/// <summary>
/// The projects folder model (llms-py v4). Expectations are the output of llms-py's
/// kebab_case()/sanitize_publish_path() for the same inputs.
/// </summary>
public class AiChatProjectsTests
{
    const string ProjectDir = "/home/u/.llms/user/bob/projects/my-app";

    [TestCase("Tic Tac Toe", ExpectedResult = "tic-tac-toe")]
    [TestCase("Breakout", ExpectedResult = "breakout")]
    [TestCase("2048", ExpectedResult = "2048")]
    [TestCase("My App (v2)", ExpectedResult = "my-app-v2")]
    [TestCase("  Spaced  Out  ", ExpectedResult = "spaced-out")]
    [TestCase("Hello__World", ExpectedResult = "hello-world")]
    [TestCase("--dashes--", ExpectedResult = "dashes")]
    [TestCase("Ünïcode Nàme", ExpectedResult = "ünïcode-nàme")]
    [TestCase("", ExpectedResult = "")]
    [TestCase(null, ExpectedResult = "")]
    public string Kebab_cases_a_project_name(string? name) => ProjectsExtension.KebabCase(name);

    [Test]
    public void Project_folder_defaults_to_the_kebab_cased_name()
    {
        Assert.That(ProjectsExtension.GetProjectFolder(new JsonObject { ["name"] = "Tic Tac Toe" }),
            Is.EqualTo("tic-tac-toe"));

        // an explicit folder wins over the name
        Assert.That(ProjectsExtension.GetProjectFolder(new JsonObject
        {
            ["name"] = "Tic Tac Toe",
            ["folder"] = " ttt ",
        }), Is.EqualTo("ttt"));

        // a blank folder falls back to the name
        Assert.That(ProjectsExtension.GetProjectFolder(new JsonObject
        {
            ["name"] = "Tic Tac Toe",
            ["folder"] = "  ",
        }), Is.EqualTo("tic-tac-toe"));
    }

    [Test]
    public void Project_dir_is_under_the_users_projects_folder()
    {
        Assert.That(ProjectsExtension.GetProjectDir("/data/user/bob", new JsonObject { ["name"] = "My App" }),
            Is.EqualTo("/data/user/bob/projects/my-app"));
    }

    // already relative
    [TestCase("", ExpectedResult = "")]
    [TestCase("   ", ExpectedResult = "")]
    [TestCase("dist", ExpectedResult = "dist")]
    [TestCase("/dist", ExpectedResult = "dist")]
    [TestCase("dist/", ExpectedResult = "dist")]
    [TestCase("./dist", ExpectedResult = "dist")]
    [TestCase(@"dist\assets", ExpectedResult = "dist/assets")]
    // absolute paths inside the project are made relative
    [TestCase(ProjectDir, ExpectedResult = "")]
    [TestCase(ProjectDir + "/dist", ExpectedResult = "dist")]
    [TestCase(ProjectDir + "/dist/assets", ExpectedResult = "dist/assets")]
    // a redundant folder prefix is dropped
    [TestCase("my-app", ExpectedResult = "")]
    [TestCase("my-app/dist", ExpectedResult = "dist")]
    [TestCase("projects/my-app", ExpectedResult = "")]
    [TestCase("projects/my-app/dist", ExpectedResult = "dist")]
    // traversal segments are stripped, so the result stays inside the project
    [TestCase("../../../etc/passwd", ExpectedResult = "etc/passwd")]
    [TestCase("dist/../../secrets", ExpectedResult = "dist/secrets")]
    [TestCase("/etc/passwd", ExpectedResult = "etc/passwd")]
    [TestCase("/home/u/.llms/user/eve/projects/other/dist",
        ExpectedResult = "home/u/.llms/user/eve/projects/other/dist")]
    public string Sanitizes_a_publish_path_against_a_project_dir(string publish) =>
        ProjectsExtension.SanitizePublishPath(publish, ProjectDir);

    [TestCase("dist", ExpectedResult = "dist")]
    [TestCase("/a/b/projects/my-app/dist", ExpectedResult = "dist")]
    [TestCase("projects/my-app", ExpectedResult = "")]
    [TestCase("/x/y/z", ExpectedResult = "x/y/z")]
    public string Sanitizes_a_publish_path_without_a_project_dir(string publish) =>
        ProjectsExtension.SanitizePublishPath(publish);

    [Test]
    public void A_sanitized_publish_path_always_resolves_inside_the_project()
    {
        string[] hostile = [
            "../../../etc/passwd", "/etc/passwd", "..", "../..", "dist/../../../..",
            "/home/u/.llms/user/eve/projects/other", @"..\..\windows\system32",
        ];
        foreach (var publish in hostile)
        {
            var resolved = Path.GetFullPath(Path.Combine(ProjectDir,
                ProjectsExtension.SanitizePublishPath(publish, ProjectDir)));
            Assert.That(ProjectsExtension.IsWithin(resolved, ProjectDir), Is.True,
                $"'{publish}' escaped the project folder as '{resolved}'");
        }
    }

    [Test]
    public void IsWithin_does_not_match_a_sibling_with_the_same_prefix()
    {
        Assert.That(ProjectsExtension.IsWithin(ProjectDir, ProjectDir), Is.True);
        Assert.That(ProjectsExtension.IsWithin(ProjectDir + "/dist", ProjectDir), Is.True);
        Assert.That(ProjectsExtension.IsWithin(ProjectDir + "-other", ProjectDir), Is.False);
        Assert.That(ProjectsExtension.IsWithin("/home/u/.llms/user/bob/projects", ProjectDir), Is.False);
    }

    [Test]
    public void Reading_migrates_projects_saved_before_the_folder_model()
    {
        var appDataPath = Path.Combine(Path.GetTempPath(), "chat-" + Guid.NewGuid().ToString("N"));
        try
        {
            var feature = new ChatFeature { AppData = new ChatAppData(appDataPath) };
            var ext = new ProjectsExtension();
            ext.Install(new ExtensionContext(feature, ext.Name));

            var userProjects = Path.Combine(feature.AppData.GetUserPath("bob"), "projects");
            Directory.CreateDirectory(userProjects);
            File.WriteAllText(Path.Combine(userProjects, "projects.json"), """
                [{
                  "name": "Tic Tac Toe",
                  "paths": ["$WORKSPACE", "/home/user/src/tic-tac-toe"],
                  "publish": "/home/user/src/tic-tac-toe/dist"
                }]
                """);

            var project = ext.GetUserProjects("bob")[0];

            Assert.That(project.GetString("folder"), Is.EqualTo("tic-tac-toe"));
            // an old publish path outside the project is flattened to a relative one inside it
            // (matching llms-py) — junk, but it can no longer reach the original directory
            Assert.That(project.GetString("publish"), Is.EqualTo("home/user/src/tic-tac-toe/dist"));
            // `paths` is only dropped on save, so it survives a read
            Assert.That(project.ContainsKey("paths"), Is.True);
        }
        finally
        {
            if (Directory.Exists(appDataPath))
                Directory.Delete(appDataPath, recursive: true);
        }
    }
}
