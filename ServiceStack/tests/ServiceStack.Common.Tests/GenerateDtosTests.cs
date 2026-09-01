#nullable enable

using System;
using System.IO;
using NUnit.Framework;
using ServiceStack.NativeTypes;
using ServiceStack.Testing;

namespace ServiceStack.Common.Tests;

[TestFixture]
[NonParallelizable]
public class GenerateDtosTests
{
    private ServiceStackHost appHost = null!;
    private string tempDir = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        appHost = new BasicAppHost(typeof(GenerateDtosRequest).Assembly)
        {
            TestMode = true,
            Plugins = { new NativeTypesFeature() },
            Config = new HostConfig(),
        }.Init();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "servicestack-generate-dtos-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, recursive: true);
    }

    [Test]
    public void Does_update_matching_references_and_preserve_explicit_options()
    {
        var localRef = Path.Combine(tempDir, "client.dtos.ts");
        var remoteRef = Path.Combine(tempDir, "remote", "dtos.ts");
        var headerlessRef = Path.Combine(tempDir, "headerless", "dtos.mjs");
        var ignoredRef = Path.Combine(tempDir, "generated", "dtos.ts");

        Directory.CreateDirectory(Path.GetDirectoryName(remoteRef)!);
        Directory.CreateDirectory(Path.GetDirectoryName(headerlessRef)!);
        Directory.CreateDirectory(Path.GetDirectoryName(ignoredRef)!);

        File.WriteAllText(localRef, TypeScriptReference("http://localhost:5000"));
        File.WriteAllText(remoteRef, TypeScriptReference("https://example.org"));
        File.WriteAllText(headerlessRef, "export class HandWritten {}\n");
        File.WriteAllText(ignoredRef, TypeScriptReference("http://localhost:5000"));

        var options = new GenerateDtosOptions
        {
            Directory = tempDir,
            BaseUrls = ["http://localhost:5000"],
        };
        options.IgnoreDirectories.Add("generated");

        var feature = appHost.GetPlugin<NativeTypesFeature>();
        var result = feature.GenerateDtos(options);

        Assert.That(result.Scanned, Is.EqualTo(3));
        Assert.That(result.Updated, Is.EqualTo(new[] { localRef }));
        Assert.That(result.Skipped.Keys, Is.EquivalentTo(new[] { remoteRef, headerlessRef }));
        Assert.That(result.Errors, Is.Empty);

        var generated = File.ReadAllText(localRef);
        Assert.That(generated, Does.Contain("export class GenerateDtosRequest"));
        Assert.That(generated, Does.Contain("\nIncludeTypes: GenerateDtosRequest\n"));
        Assert.That(generated, Does.Not.Contain("\n//IncludeTypes: GenerateDtosRequest\n"));
        Assert.That(File.ReadAllText(ignoredRef), Does.Contain("stale"));

        var secondRun = feature.GenerateDtos(options);
        Assert.That(secondRun.Updated, Is.Empty);
        Assert.That(secondRun.Unchanged, Is.EqualTo(new[] { localRef }));
        Assert.That(secondRun.Errors, Is.Empty);
    }

    [Test]
    public void Uses_localhost_fallback_when_host_url_cannot_be_determined()
    {
        var localRef = Path.Combine(tempDir, "dtos.ts");
        File.WriteAllText(localRef, TypeScriptReference("https://localhost:5001"));

        var result = appHost.GetPlugin<NativeTypesFeature>().GenerateDtos(new GenerateDtosOptions
        {
            Directory = tempDir,
        });

        Assert.That(result.Updated, Is.EqualTo(new[] { localRef }));
        Assert.That(result.Errors, Is.Empty);
    }

    private static string TypeScriptReference(string baseUrl) =>
        $"""
        /* Options:
        Date: 2000-01-01 00:00:00
        Version: 1.0
        Tip: To override a DTO option, remove "//" prefix before updating
        BaseUrl: {baseUrl}

        IncludeTypes: GenerateDtosRequest
        //AddServiceStackTypes: True
        */

        stale
        """;
}

public class GenerateDtosRequest : IReturn<GenerateDtosResponse>
{
    public string? Name { get; set; }
}

public class GenerateDtosResponse
{
    public string? Result { get; set; }
}

public class GenerateDtosServices : Service
{
    public object Any(GenerateDtosRequest request) => new GenerateDtosResponse { Result = request.Name };
}
