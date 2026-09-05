using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.NativeTypes;
using ServiceStack.NativeTypes.CSharp;
using ServiceStack.NativeTypes.Dart;
using ServiceStack.NativeTypes.FSharp;
using ServiceStack.NativeTypes.Go;
using ServiceStack.NativeTypes.Java;
using ServiceStack.NativeTypes.Kotlin;
using ServiceStack.NativeTypes.Php;
using ServiceStack.NativeTypes.Python;
using ServiceStack.NativeTypes.Ruby;
using ServiceStack.NativeTypes.Rust;
using ServiceStack.NativeTypes.Swift;
using ServiceStack.NativeTypes.TypeScript;
using ServiceStack.NativeTypes.VbNet;
using ServiceStack.NativeTypes.Zig;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class NativeTypesModernizationTests
{
    [Test]
    public void NativeTypesFeature_ExportAttribute_guards_null_inputs()
    {
        var feature = new NativeTypesFeature();
        Assert.Throws<ArgumentNullException>(() => feature.ExportAttribute(null, attr => null));
        Assert.Throws<ArgumentNullException>(() => feature.ExportAttribute(typeof(ObsoleteAttribute), null));
    }

    [Test]
    public void NativeTypesFeature_Register_guards_null_appHost()
    {
        var feature = new NativeTypesFeature();
        Assert.DoesNotThrow(() => feature.Register(null));
    }

    [Test]
    public void NativeTypesFeature_GetGenerator_returns_generator_even_without_apphost()
    {
        var feature = new NativeTypesFeature();
        var generator = feature.GetGenerator();
        Assert.That(generator, Is.Not.Null);
    }

    [Test]
    public void StringBuilderWrapper_handles_null_sb_and_negative_indent()
    {
        var wrapper = new StringBuilderWrapper(null, -5);
        Assert.That(wrapper.Length, Is.EqualTo(0));
        wrapper.AppendLine("test");
        Assert.That(wrapper.ToString().Trim(), Is.EqualTo("test"));

        var unindented = wrapper.UnIndent();
        Assert.That(unindented, Is.Not.Null);
    }

    [Test]
    public void StringBuilderWrapper_Chop_handles_empty_and_missing_character_safely()
    {
        // Empty sb
        var sbEmpty = new StringBuilder();
        var wrapperEmpty = new StringBuilderWrapper(sbEmpty);
        Assert.DoesNotThrow(() => wrapperEmpty.Chop(','));
        Assert.That(wrapperEmpty.Length, Is.EqualTo(0));

        // Character not found - preserves content without wiping
        var sbMissing = new StringBuilder("hello world");
        var wrapperMissing = new StringBuilderWrapper(sbMissing);
        Assert.DoesNotThrow(() => wrapperMissing.Chop('z'));
        Assert.That(wrapperMissing.ToString(), Is.EqualTo("hello world"));

        // Normal chop with newline
        var sbNormal = new StringBuilder("foo, bar,\n");
        var wrapperNormal = new StringBuilderWrapper(sbNormal);
        wrapperNormal.Chop(',');
        Assert.That(wrapperNormal.ToString(), Is.EqualTo("foo, bar\n"));

        // Normal chop without newline
        var sbNoNewline = new StringBuilder("foo, bar,");
        var wrapperNoNewline = new StringBuilderWrapper(sbNoNewline);
        wrapperNoNewline.Chop(',');
        Assert.That(wrapperNoNewline.ToString(), Is.EqualTo("foo, bar"));
    }

    [Test]
    public void NativeTypesMetadata_GetConfig_handles_null_request()
    {
        var meta = new NativeTypesMetadata(new ServiceMetadata([]), NativeTypesFeature.CreateMetadataTypesConfig());
        var config = meta.GetConfig(null);
        Assert.That(config, Is.Not.Null);
    }

    [Test]
    public void NativeTypesMetadata_RemoveIgnoredTypes_handles_null_metadata()
    {
        MetadataTypes nullTypes = null;
        var result = nullTypes.RemoveIgnoredTypes(null);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public void NativeTypesMetadata_RemoveIgnoredTypes_handles_null_operation_request()
    {
        var types = new MetadataTypes
        {
            Operations = [
                new MetadataOperationType { Request = null },
                new MetadataOperationType { Request = new MetadataType { Name = "ValidReq" } }
            ]
        };
        var config = NativeTypesFeature.CreateMetadataTypesConfig();
        Assert.DoesNotThrow(() => types.RemoveIgnoredTypes(config));
        Assert.That(types.Operations.Count, Is.EqualTo(1));
        Assert.That(types.Operations[0].Request.Name, Is.EqualTo("ValidReq"));
    }

    [Test]
    [TestCase("csharp")]
    [TestCase("CSharp")]
    [TestCase("typescript")]
    [TestCase("TypeScript")]
    [TestCase("mjs")]
    [TestCase("dart")]
    [TestCase("fsharp")]
    [TestCase("go")]
    [TestCase("java")]
    [TestCase("kotlin")]
    [TestCase("python")]
    [TestCase("ruby")]
    [TestCase("rust")]
    [TestCase("swift")]
    [TestCase("vbnet")]
    [TestCase("zig")]
    public void GenerateSourceCode_extension_works_with_null_request_across_all_languages(string lang)
    {
        var metadataTypes = new List<MetadataType>
        {
            new MetadataType
            {
                Name = "TestDto",
                Namespace = "MyServices",
                Properties = [
                    new MetadataPropertyType { Name = "Id", Type = "Int32" },
                    new MetadataPropertyType { Name = "Name", Type = "String" }
                ]
            }
        };

        var code = metadataTypes.GenerateSourceCode(lang, null);
        Assert.That(code, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void All_16_Generators_GetCode_safely_handle_null_request()
    {
        var metadata = new MetadataTypes
        {
            Config = NativeTypesFeature.CreateMetadataTypesConfig(),
            Types = [
                new MetadataType
                {
                    Name = "SampleRequest",
                    Namespace = "Sample",
                    Properties = [
                        new MetadataPropertyType { Name = "Query", Type = "String" }
                    ]
                }
            ]
        };

        var generators = new ILangGenerator[]
        {
            new CSharpGenerator(metadata.Config),
            new TypeScriptGenerator(metadata.Config),
            new CommonJsGenerator(metadata.Config),
            new MjsGenerator(metadata.Config),
            new DartGenerator(metadata.Config),
            new FSharpGenerator(metadata.Config),
            new GoGenerator(metadata.Config),
            new JavaGenerator(metadata.Config),
            new KotlinGenerator(metadata.Config),
            new PhpGenerator(metadata.Config),
            new PythonGenerator(metadata.Config),
            new RubyGenerator(metadata.Config),
            new RustGenerator(metadata.Config),
            new SwiftGenerator(metadata.Config),
            new VbNetGenerator(metadata.Config),
            new ZigGenerator(metadata.Config),
        };

        var nativeTypesMeta = new NativeTypesMetadata(new ServiceMetadata([]), metadata.Config);

        foreach (var gen in generators)
        {
            var code = gen.GetCode(metadata, null, nativeTypesMeta);
            Assert.That(code, Is.Not.Null.And.Not.Empty, $"Generator {gen.GetType().Name} returned null or empty code with null request.");
        }
    }

    [Test]
    public void RubyGenerator_EnumNameFormat_handles_null_and_empty_safely()
    {
        Assert.That(RubyGenerator.EnumNameFormat(null), Is.Null);
        Assert.That(RubyGenerator.EnumNameFormat(string.Empty), Is.EqualTo(string.Empty));
        Assert.That(RubyGenerator.EnumNameFormat("my_enum_value"), Is.EqualTo("MY_ENUM_VALUE"));
        Assert.That(RubyGenerator.EnumNameFormat("MyEnumValue"), Is.EqualTo("MY_ENUM_VALUE"));
        Assert.That(RubyGenerator.EnumNameFormat("VALUE"), Is.EqualTo("VALUE"));
    }

    [Test]
    public void All_16_Generators_safely_handle_null_AddQueryParamOptions()
    {
        var metadata = new MetadataTypes
        {
            Config = NativeTypesFeature.CreateMetadataTypesConfig(),
            Types = [
                new MetadataType
                {
                    Name = "SampleRequest",
                    Namespace = "Sample",
                    Properties = [
                        new MetadataPropertyType { Name = "Query", Type = "String" }
                    ]
                }
            ]
        };

        var generators = new ILangGenerator[]
        {
            new CSharpGenerator(metadata.Config),
            new TypeScriptGenerator(metadata.Config),
            new CommonJsGenerator(metadata.Config),
            new MjsGenerator(metadata.Config),
            new DartGenerator(metadata.Config),
            new FSharpGenerator(metadata.Config),
            new GoGenerator(metadata.Config),
            new JavaGenerator(metadata.Config),
            new KotlinGenerator(metadata.Config),
            new PhpGenerator(metadata.Config),
            new PythonGenerator(metadata.Config),
            new RubyGenerator(metadata.Config),
            new RustGenerator(metadata.Config),
            new SwiftGenerator(metadata.Config),
            new VbNetGenerator(metadata.Config),
            new ZigGenerator(metadata.Config),
        };

        var nativeTypesMeta = new NativeTypesMetadata(new ServiceMetadata([]), metadata.Config);

        foreach (var gen in generators)
        {
            // Explicitly set to null to test defensiveness even if uninitialized or assigned null
            gen.AddQueryParamOptions = null;
            Assert.DoesNotThrow(() =>
            {
                var code = gen.GetCode(metadata, null, nativeTypesMeta);
                Assert.That(code, Is.Not.Null.And.Not.Empty);
            }, $"Generator {gen.GetType().Name} threw when AddQueryParamOptions was null");
        }
    }

    [Test]
    public void All_Generators_initialize_collections_without_active_AppHost_or_Feature()
    {
        var config = NativeTypesFeature.CreateMetadataTypesConfig();
        config.InitializeCollections = true;

        var metadata = new MetadataTypes
        {
            Config = config,
            Types = [
                new MetadataType
                {
                    Name = "CollectionDto",
                    Namespace = "TestNamespace",
                    Properties = [
                        new MetadataPropertyType { Name = "Items", Type = "List`1", GenericArgs = ["String"] },
                        new MetadataPropertyType { Name = "Map", Type = "Dictionary`2", GenericArgs = ["String", "Int32"] }
                    ]
                }
            ]
        };

        var generators = new ILangGenerator[]
        {
            new CSharpGenerator(metadata.Config),
            new TypeScriptGenerator(metadata.Config),
            new CommonJsGenerator(metadata.Config),
            new MjsGenerator(metadata.Config),
            new DartGenerator(metadata.Config),
            new FSharpGenerator(metadata.Config),
            new GoGenerator(metadata.Config),
            new JavaGenerator(metadata.Config),
            new KotlinGenerator(metadata.Config),
            new PhpGenerator(metadata.Config),
            new PythonGenerator(metadata.Config),
            new RubyGenerator(metadata.Config),
            new RustGenerator(metadata.Config),
            new SwiftGenerator(metadata.Config),
            new VbNetGenerator(metadata.Config),
            new ZigGenerator(metadata.Config),
        };

        var nativeTypesMeta = new NativeTypesMetadata(new ServiceMetadata([]), metadata.Config);

        foreach (var gen in generators)
        {
            Assert.DoesNotThrow(() =>
            {
                var code = gen.GetCode(metadata, null, nativeTypesMeta);
                Assert.That(code, Is.Not.Null.And.Not.Empty);
            }, $"Generator {gen.GetType().Name} failed during collection initialization when feature was null");
        }
    }
}
