using System;
using System.Collections.Generic;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes;

public interface ILangGenerator
{
    Lang Lang { get; }
    MetadataTypesConfig Config { get; }
    
    bool WithoutOptions { get; set; }
    
    List<string> AddQueryParamOptions { get; set; }
    
    string GetCode(MetadataTypes metadata, IRequest request, INativeTypesMetadata nativeTypes);
}

public static class LangGeneratorExtensions
{
    public static string GenerateSourceCode(this List<MetadataType> metadataTypes, string lang, IRequest req,
        Action<ILangGenerator> configure = null)
    {
        var nativeTypes = HostContext.AppHost?.GetPlugin<NativeTypesFeature>();
        var request = new NativeTypesBase {
            ExportAsTypes = true,
        };
        var nativeTypesMeta = req?.TryResolve<INativeTypesMetadata>() 
            ?? HostContext.AppHost?.TryResolve<INativeTypesMetadata>()
            ?? new NativeTypesMetadata(HostContext.AppHost?.Metadata ?? new ServiceMetadata([]), nativeTypes?.MetadataTypesConfig ?? NativeTypesFeature.CreateMetadataTypesConfig());
        var typesConfig = nativeTypesMeta.GetConfig(request);
        var types = new MetadataTypes {
            Config = typesConfig,
            Types = metadataTypes ?? [],
        };
        types.Config.BaseUrl = nativeTypes?.MetadataTypesConfig?.BaseUrl ?? req?.GetBaseUrl();
        return types.GenerateSourceCode(types.Config, lang, req, configure);
    }

    public static string GenerateSourceCode(this MetadataTypes metadataTypes, MetadataTypesConfig typesConfig, string lang, IRequest req, 
        Action<ILangGenerator> configure = null)
    {
        typesConfig ??= NativeTypesFeature.CreateMetadataTypesConfig();
        metadataTypes ??= new MetadataTypes { Config = typesConfig, Types = [] };
        metadataTypes.Config ??= typesConfig;

        string Generate(ILangGenerator gen)
        {
            configure?.Invoke(gen);
            var meta = req?.TryResolve<INativeTypesMetadata>() 
                ?? HostContext.AppHost?.TryResolve<INativeTypesMetadata>()
                ?? new NativeTypesMetadata(HostContext.AppHost?.Metadata ?? new ServiceMetadata([]), typesConfig);
            return gen.GetCode(metadataTypes, req, meta);
        }
            
        var src = (lang?.ToLowerInvariant()) switch {
            "csharp" => Generate(new CSharp.CSharpGenerator(typesConfig)),
            "mjs" => Generate(new TypeScript.MjsGenerator(typesConfig)),
            "typescript" => Generate(new TypeScript.TypeScriptGenerator(typesConfig)),
            "dart" => Generate(new Dart.DartGenerator(typesConfig)),
            "java" => Generate(new Java.JavaGenerator(typesConfig)),
            "kotlin" => Generate(new Kotlin.KotlinGenerator(typesConfig)),
            "python" => Generate(new Python.PythonGenerator(typesConfig)),
            "php" => Generate(new Php.PhpGenerator(typesConfig)),
            "swift" => Generate(new Swift.SwiftGenerator(typesConfig)),
            "vbnet" => Generate(new VbNet.VbNetGenerator(typesConfig)),
            "fsharp" => Generate(new FSharp.FSharpGenerator(typesConfig)),
            "go" => Generate(new Go.GoGenerator(typesConfig)),
            "rust" => Generate(new Rust.RustGenerator(typesConfig)),
            "ruby" => Generate(new Ruby.RubyGenerator(typesConfig)),
            "zig" => Generate(new Zig.ZigGenerator(typesConfig)),
            _ => throw new NotSupportedException(
                $"Unknown language: '{lang}'. Supported languages: " +
                $"csharp, mjs, typescript, dart, java, kotlin, python, php, swift, vbnet, fsharp, go, rust, ruby, zig")
        };
        return src;
    }
}
