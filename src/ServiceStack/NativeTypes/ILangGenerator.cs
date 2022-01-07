using System;
using System.Collections.Generic;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes;

public interface ILangGenerator
{
    bool WithoutOptions { get; set; }
    
    List<string> AddQueryParamOptions { get; set; }
    
    string GetCode(MetadataTypes metadata, IRequest request, INativeTypesMetadata nativeTypes);
}

public static class LangGeneratorExtensions
{
    public static string GenerateSourceCode(this List<MetadataType> metadataTypes, string lang, IRequest req,
        Action<ILangGenerator> configure = null)
    {
        var nativeTypes = HostContext.GetPlugin<NativeTypesFeature>();
        var request = new NativeTypesBase {
            // GlobalNamespace = "dtos",
            ExportAsTypes = true,
        };
        var types = new MetadataTypes {
            Config = req.Resolve<INativeTypesMetadata>().GetConfig(request),
            Types = metadataTypes,
        };
        types.Config.BaseUrl = nativeTypes.MetadataTypesConfig.BaseUrl ?? req.GetBaseUrl();
        return types.GenerateSourceCode(types.Config, lang, req, configure);
    }

    public static string GenerateSourceCode(this MetadataTypes metadataTypes, MetadataTypesConfig typesConfig, string lang, IRequest req, 
        Action<ILangGenerator> configure = null)
    {
        string Generate(ILangGenerator gen)
        {
            configure?.Invoke(gen);
            return gen.GetCode(metadataTypes, req, req.Resolve<INativeTypesMetadata>());
        }
            
        var src = lang switch {
            "csharp" => Generate(new CSharp.CSharpGenerator(typesConfig)),
            "typescript" => Generate(new TypeScript.TypeScriptGenerator(typesConfig)),
            "dart" => Generate(new Dart.DartGenerator(typesConfig)),
            "java" => Generate(new Java.JavaGenerator(typesConfig)),
            "kotlin" => Generate(new Kotlin.KotlinGenerator(typesConfig)),
            "python" => Generate(new Python.PythonGenerator(typesConfig)),
            "swift" => Generate(new Swift.SwiftGenerator(typesConfig)),
            "vbnet" => Generate(new VbNet.VbNetGenerator(typesConfig)),
            "fsharp" => Generate(new FSharp.FSharpGenerator(typesConfig)),
            _ => throw new NotSupportedException($"Unknown language '{lang}', Supported languages: " +
                                                 $"csharp, typescript, dart, java, kotlin, python, swift, vbnet, fsharp")
        };
        return src;
    }
}
