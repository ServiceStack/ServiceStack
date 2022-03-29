using System.Reflection;
using System.Text;
using Funq;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Admin;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.HtmlModules;
using ServiceStack.IO;
using ServiceStack.NativeTypes;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace NetCoreTests;

[Category("Publish Tasks")]
public class PublishTasks
{
    readonly string ProjectDir = Path.GetFullPath("../../../../NorthwindAuto");
    string FromModulesDir => Path.GetFullPath(".");
    string ToModulesDir => Path.GetFullPath("../../src/ServiceStack/modules");
    readonly string NetCoreTestsDir = Path.GetFullPath("../../../");
    string[] IgnoreUiFiles = { };
    string[] IgnoreAdminUiFiles = { };

    FilesTransformer transformOptions => FilesTransformer.Defaults(debugMode: true);

    [Test]
    public void Print_paths()
    {
        Directory.SetCurrentDirectory(ProjectDir);
        FromModulesDir.Print();
        ToModulesDir.Print();
    }

    /*  publish.bat:
        call npm run ui:build 
        RD /q /s ..\..\src\ServiceStack\modules\ui 
        XCOPY /Y /E /H /C /I ui ..\..\src\ServiceStack\modules\ui 
        DEL ..\..\src\ServiceStack\modules\ui\index.css
        RD /q /s ..\..\src\ServiceStack\modules\shared 
        XCOPY /Y /E /H /C /I shared ..\..\src\ServiceStack\modules\shared
     */
    [Test]
    public async Task Publish_ui()
    {
        Directory.SetCurrentDirectory(ProjectDir);
        FromModulesDir.Print();
        ToModulesDir.Print();

        //publish tailwind
        await ProcessUtils.RunShellAsync("npm run ui:build",
            onOut:   Console.WriteLine, 
            onError: Console.Error.WriteLine);
        
        // copy to modules/ui
        transformOptions.CopyAll(
            source: new FileSystemVirtualFiles(FromModulesDir.CombineWith("ui")), 
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("ui")), 
            cleanTarget: true,
            ignore: file => IgnoreUiFiles.Contains(file.VirtualPath),
            afterCopy: (file, contents) => $"{file.VirtualPath} ({contents.Length})".Print());
        
        // copy to modules/locode
        transformOptions.CopyAll(
            source: new FileSystemVirtualFiles(FromModulesDir.CombineWith("locode")), 
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("locode")), 
            cleanTarget: true,
            ignore: file => IgnoreUiFiles.Contains(file.VirtualPath),
            afterCopy: (file, contents) => $"{file.VirtualPath} ({contents.Length})".Print());
        
        // copy to modules/admin-ui
        transformOptions.CopyAll(
            source: new FileSystemVirtualFiles(FromModulesDir.CombineWith("admin-ui")), 
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("admin-ui")), 
            cleanTarget: true,
            ignore: file => IgnoreAdminUiFiles.Contains(file.VirtualPath),
            afterCopy: (file, contents) => $"{file.VirtualPath} ({contents.Length})".Print());

        // copy to modules/shared
        transformOptions.CopyAll(
            source: new FileSystemVirtualFiles(FromModulesDir.CombineWith("shared")),
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("shared")),
            cleanTarget: true,
            afterCopy: (file, contents) => $"{file.VirtualPath} ({contents.Length})".Print());
    }

    [Test]
    public void Publish_Bookings_and_Todos_cs()
    {
        Directory.SetCurrentDirectory(ProjectDir);
        
        string[] ResolveTargetDirs(string name) => new[]
        {
            $"../../tests/ServiceStack.Blazor.Tests/" + (name == "ServiceModel" ? name : "Server/" + name) + "/",
            $"../../../NetCoreTemplates/blazor-wasm/MyApp.{name}/",
            $"../../../NetCoreTemplates/vue-vite/api/MyApp.{name}/",
            $"../../../NetCoreTemplates/vue-ssg/api/MyApp.{name}/",
            $"../../../NetCoreTemplates/nextjs/api/MyApp.{name}/",
        };

        void CopyFile(string copyFile, string fileName, string[] targetDirs)
        {
            $"Copying {Path.GetFullPath(copyFile)}...".Print();
            foreach (var folder in targetDirs)
            {
                var toFile = Path.GetFullPath(folder.CombineWith(fileName));
                $"Writing to {toFile}".Print();
                File.Copy(copyFile, toFile, overwrite: true);
            }
            "".Print();
        }

        CopyFile("ServiceModel/Bookings.cs", "Bookings.cs", ResolveTargetDirs("ServiceModel"));
        CopyFile("ServiceModel/Todos.cs", "Todos.cs", ResolveTargetDirs("ServiceModel"));
        // TodosServices.cs specific to: Learn, Blazor, WASM
        // CopyFile("ServiceInterface/TodosServices.cs", "TodosServices.cs", ResolveTargetDirs("ServiceInterface"));
    }

    class AppHost : AppSelfHostBase
    {
        public AppHost() : base(nameof(PublishTasks), typeof(MetadataAppService)) {}
        public override void Configure(Container container)
        {
            Metadata.ForceInclude = new() {
                typeof(MetadataApp),
                typeof(AppMetadata),
                typeof(AdminQueryUsers),
                typeof(AdminGetUser),
                typeof(AdminCreateUser),
                typeof(AdminUpdateUser),
                typeof(AdminDeleteUser),
                typeof(GetCrudEvents),
            };
            
            Plugins.Add(new AuthFeature(() => new AuthUserSession(), new [] {
                new CredentialsAuthProvider(AppSettings),
            }));
            
            container.AddSingleton<ICrudEvents>(c =>
                new OrmLiteCrudEvents(c.Resolve<IDbConnectionFactory>()));

            Plugins.Add(new AdminUsersFeature());
            Plugins.Add(new AutoQueryFeature());
        }
    }

    [Test]
    public void Generate_AppMetadata_details()
    {
        var baseUrl = "http://localhost:20000";
        using var appHost = new AppHost().Init().Start(baseUrl);
        
        var dtos = baseUrl.CombineWith("/types/typescript").GetStringFromUrl();
        dtos += @"
// declare Types used in /ui 
// @ts-ignore
export declare var APP:AppMetadata
";
        
        Directory.SetCurrentDirectory(ProjectDir);
        File.WriteAllText(Path.GetFullPath("./lib/types.ts"), dtos);
    }

    [Test]
    public async Task Create_TypeScript_Definitions()
    {
        Directory.SetCurrentDirectory(NetCoreTestsDir);
        
        await ProcessUtils.RunShellAsync("rd /q /s types && tsc",
            onOut:   Console.WriteLine, 
            onError: Console.Error.WriteLine);
    }

    [Test]
    public async Task Create_TypeScript_Definitions_Publish()
    {
        Directory.SetCurrentDirectory(NetCoreTestsDir);
        await ProcessUtils.RunShellAsync("rd /q /s types && tsc",
            onOut:   Console.WriteLine, 
            onError: Console.Error.WriteLine);
        
        // Export API Explorer's .d.ts to 'explorer' 
        Directory.Move("types/ui", "types/explorer");
        Directory.Move("types/admin-ui", "types/admin");

        FileSystemVirtualFiles.RecreateDirectory("dist");
        File.Copy("../NorthwindAuto/node_modules/@servicestack/client/dist/index.d.ts", "dist/client.d.ts");

        var memFs = new MemoryVirtualFiles();
        var typesFs = new FileSystemVirtualFiles("types");
        var distFs = new FileSystemVirtualFiles("dist");

        var typesFile = typesFs.GetFile("lib/types.d.ts");
        memFs.WriteFile("0_" + typesFile.Name, typesFile);
        memFs.TransformAndCopy("shared", typesFs, distFs);

        memFs.Clear();
        memFs.TransformAndCopy("locode", typesFs, distFs);
        
        memFs.Clear();
        memFs.TransformAndCopy("explorer", typesFs, distFs);
        
        memFs.Clear();
        memFs.TransformAndCopy("admin", typesFs, distFs);

        var libFs = new FileSystemVirtualFiles("../../../../servicestack-ui".AssertDir());
        libFs.CopyFrom(distFs.GetAllFiles());
    }
}

public static class TypeScriptDefinitionUtils
{
    private static string Header = @"import { ApiResult, JsonServiceClient } from './client'
import { App, MetadataOperationType, MetadataType, MetadataPropertyType, InputInfo, ThemeInfo, LinkInfo, Breakpoints, AuthenticateResponse, AdminUsersInfo } from './shared'
";
    private static string Footer = @"export declare var App:App";

    private static Dictionary<string, string> Headers = new()
    {
        ["locode"] = Header,
        ["explorer"] = Header,
        ["admin"] = Header,
    };

    private static Dictionary<string, string> Footers = new()
    {
        ["locode"] = Footer,
        ["explorer"] = Footer,
        ["admin"] = Footer,
    };

    static FilesTransformer TransformerOptions = new()
    {
        FileExtensions =
        {
            ["ts"] = new FileTransformerOptions
            {
                // BlockTransformers = {
                //     new RemoveBlock("/**", "*/", Run.IgnoreInDebug),
                // },
                LineTransformers = new()
                {
                    new RemoveLineStartingWith(new[] { "import " }, ignoreWhiteSpace:false, Run.Always),
                    new RemoveLineStartingWith("export {};", ignoreWhiteSpace:false, Run.Always),
                    new RemoveLineStartingWith("export type App = import(", ignoreWhiteSpace:false, Run.Always),
                    new RemoveLineStartingWith("export type Breakpoints = import(", ignoreWhiteSpace:false, Run.Always),
                },
            },
        }
    };

    public static void TransformAndCopy(this MemoryVirtualFiles memFs, string path, FileSystemVirtualFiles typesFs, FileSystemVirtualFiles distFs)
    {
        var i = memFs.GetAllFiles().Count() + 1;
        typesFs.GetDirectory(path).GetAllFiles()
            .Each(file => memFs.WriteFile(++i + "_" + file.Name, file));

        var wipFs = new MemoryVirtualFiles();
        TransformerOptions.CopyAll(
            source: memFs, 
            target: wipFs,
            cleanTarget:true,
            afterCopy: (file, contents) => $"{file.VirtualPath} ({contents.Length})".Print());
        
        var sb = new StringBuilder();
        if (Headers.TryGetValue(path, out var header))
            sb.AppendLine(header);
        wipFs.GetAllFiles()
            .Each(file => sb.AppendLine(file.ReadAllText()));
        if (Footers.TryGetValue(path, out var footer))
            sb.AppendLine(footer);
        
        distFs.WriteFile($"{path}.d.ts", sb.ToString());
    }
}
