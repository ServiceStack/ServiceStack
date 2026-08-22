#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.Extensions.Tests;

public class AiChatFeatureTests
{
    [Test]
    public void Config_files_auto_update_by_default()
    {
        var feature = new ChatFeature();

        Assert.That(feature.AutoUpdate, Is.EquivalentTo(new[]
        {
            "llms.json", "providers.json", "providers-extra.json",
        }));
    }

    [Test]
    public void App_data_can_auto_update_or_preserve_existing_config()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ai-chat-{Guid.NewGuid():N}");
        try
        {
            var appData = new ChatAppData(dir);
            Assert.That(appData.SeedOrUpdateFile("llms.json", "v1", false), Is.EqualTo("v1"));
            Assert.That(appData.SeedOrUpdateFile("llms.json", "v2", false), Is.EqualTo("v1"));
            Assert.That(appData.SeedOrUpdateFile("llms.json", "v2", true), Is.EqualTo("v2"));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public void Runs_every_registered_shutdown_handler()
    {
        var feature = new ChatFeature();
        var ran = new List<string>();
        feature.Filters.ShutdownHandlers.Add(() => ran.Add("first"));
        feature.Filters.ShutdownHandlers.Add(() => ran.Add("second"));

        feature.RunShutdownHandlers();

        Assert.That(ran, Is.EqualTo(new[] { "first", "second" }));
    }

    [Test]
    public void A_failing_shutdown_handler_does_not_skip_the_others()
    {
        var feature = new ChatFeature();
        var ran = new List<string>();
        feature.Filters.ShutdownHandlers.Add(() => throw new Exception("boom"));
        feature.Filters.ShutdownHandlers.Add(() => ran.Add("after"));

        Assert.DoesNotThrow(() => feature.RunShutdownHandlers());
        Assert.That(ran, Is.EqualTo(new[] { "after" }));
    }
}
