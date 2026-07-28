#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.Extensions.Tests;

public class AiChatFeatureTests
{
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
