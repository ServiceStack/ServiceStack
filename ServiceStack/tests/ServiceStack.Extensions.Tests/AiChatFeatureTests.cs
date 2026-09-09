#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.Host;
using ServiceStack.NativeTypes;
using ServiceStack.Testing;

namespace ServiceStack.Extensions.Tests;

public class AiChatFeatureTests
{
    [Test]
    public void Excludes_AI_Chat_types_from_generated_DTOs_by_default()
    {
        var metadata = GenerateMetadata(new ChatFeature());

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Operations.Any(x => x.Request.Namespace == typeof(ChatFeature).Namespace), Is.False);
            Assert.That(metadata.Types.Any(x => x.Namespace == typeof(ChatFeature).Namespace), Is.False);
        });
    }

    [Test]
    public void Can_opt_in_to_AI_Chat_types_in_generated_DTOs()
    {
        var metadata = GenerateMetadata(new ChatFeature { IncludeInGeneratedDtos = true });

        Assert.That(metadata.Operations.Any(x => x.Request.Name == nameof(ChatCompletion)), Is.True);
    }

    private static MetadataTypes GenerateMetadata(ChatFeature feature)
    {
        var nativeTypes = new NativeTypesFeature();
        using var appHost = new BasicAppHost
        {
            Config = new HostConfig(),
            Plugins = { nativeTypes },
        };
        feature.BeforePluginsLoaded(appHost);

        var serviceMetadata = new ServiceMetadata();
        serviceMetadata.Add(typeof(ChatServices), typeof(ChatCompletion), typeof(ChatResponse));
        return new NativeTypesMetadata(serviceMetadata, nativeTypes.MetadataTypesConfig)
            .GetMetadataTypes(new BasicRequest());
    }

    [Test]
    public void Config_files_auto_update_by_default()
    {
        var feature = new ChatFeature();

        Assert.That(feature.PreserveConfigs, Is.Empty);
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

    [Test]
    public void Every_schema_owning_extension_implements_IHasSchema()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new AppExtension(), Is.InstanceOf<IHasSchema>());
            Assert.That(new GeminiExtension(), Is.InstanceOf<IHasSchema>());
            Assert.That(new ApiToolsExtension(), Is.InstanceOf<IHasSchema>());
        });
    }

    [Test]
    public void Drops_installed_extension_schemas_in_reverse_order()
    {
        var feature = new ChatFeature();
        var dropped = new List<string>();
        var first = new TestSchemaExtension("first", dropped);
        var withoutSchema = new TestExtension("without-schema");
        var last = new TestSchemaExtension("last", dropped);
        var installed = (IList)typeof(ChatFeature)
            .GetField("installedExtensions", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(feature)!;
        installed.Add(((ChatExtension)first, new ExtensionContext(feature, first.Name)));
        installed.Add(((ChatExtension)withoutSchema, new ExtensionContext(feature, withoutSchema.Name)));
        installed.Add(((ChatExtension)last, new ExtensionContext(feature, last.Name)));

        feature.DropSchema();

        Assert.That(dropped, Is.EqualTo(new[] { "last", "first" }));
    }

    sealed class TestExtension(string name) : ChatExtension(name)
    {
        public override void Install(ExtensionContext ctx) { }
    }

    sealed class TestSchemaExtension(string name, List<string> dropped) : ChatExtension(name), IHasSchema
    {
        public override void Install(ExtensionContext ctx) { }
        public void InitSchema() { }
        public void DropSchema() => dropped.Add(Name);
    }
}
