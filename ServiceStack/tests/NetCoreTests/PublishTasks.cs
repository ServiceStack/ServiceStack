using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Admin;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.HtmlModules;
using ServiceStack.IO;
using ServiceStack.NativeTypes;
using ServiceStack.OrmLite;
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

    [Test]
    public void Update_js()
    {
        Directory.SetCurrentDirectory(NetCoreTestsDir);

        var jsFiles = new Dictionary<string, string>
        {
            ["servicestack-client.js"] = "../../../../servicestack-client/dist/servicestack-client.min.js",
            ["servicestack-client.mjs"] = "../../../../servicestack-client/dist/servicestack-client.min.mjs",
            ["servicestack-vue.mjs"] = "../../../../servicestack-vue/dist/servicestack-vue.min.mjs",
            ["vue.mjs"] = "https://unpkg.com/vue@3/dist/vue.esm-browser.prod.js",
        };

        var jsDir = "../../src/ServiceStack/js";

        foreach (var jsFile in jsFiles)
        {
            var toFile = Path.GetFullPath(jsDir.CombineWith(jsFile.Key));
            if (jsFile.Value.StartsWith("https://"))
            {
                $"GET {jsFile.Value}".Print();
                var js = jsFile.Value.GetStringFromUrl();
                File.WriteAllText(toFile, js);
            }
            else
            {
                var fromFile = Path.GetFullPath(jsFile.Value);
                $"COPY {fromFile} {toFile}".Print();
                File.Copy(fromFile, toFile, overwrite:true);
            }
        }
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

        // copy to js
        FilesTransformer.Defaults(debugMode:false).CopyAll(
            source: new FileSystemVirtualFiles(FromModulesDir.CombineWith("wwwroot/js")),
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("../js")),
            cleanTarget: true,
            afterCopy: (file, contents) => $"{file.VirtualPath} ({contents.Length})".Print());
        
        // copy to modules/locode2
        var moduleOptions = FilesTransformer.Defaults(debugMode: true);
        moduleOptions.FileExtensions["html"].LineTransformers = FilesTransformer.HtmlModuleLineTransformers.ToList();
        moduleOptions.CopyAll(
            source: new FileSystemVirtualFiles(FromModulesDir.CombineWith("locode2")), 
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("locode2")), 
            cleanTarget: true,
            ignore: file => IgnoreUiFiles.Contains(file.VirtualPath),
            afterCopy: (file, contents) => $"{file.VirtualPath} ({contents.Length})".Print());
    }

    [Test]
    public void Copy_mjs()
    {
        Directory.SetCurrentDirectory(ProjectDir);

        // copy to js
        FilesTransformer.Defaults(debugMode: false).CopyAll(
            source: new FileSystemVirtualFiles(FromModulesDir.CombineWith("wwwroot/js")),
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("../js")),
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
        public AppHost() : base(nameof(PublishTasks), typeof(MetadataAppService), typeof(TestService)) {}
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
                typeof(GetValidationRules),
                typeof(ModifyValidationRules),
                typeof(RequestLogs),
                typeof(AdminDashboard),
                typeof(AdminProfiling),
                typeof(AdminRedis),
                typeof(AdminDatabase),
            };
            
            Plugins.Add(new AuthFeature(() => new AuthUserSession(), new [] {
                new CredentialsAuthProvider(AppSettings),
            }));
            
            var dbFactory = new OrmLiteConnectionFactory(":memory:",
                SqliteDialect.Provider);
            container.AddSingleton<IDbConnectionFactory>(dbFactory);
            container.AddSingleton<IAuthRepository>(c => new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));

            container.AddSingleton<ICrudEvents>(c =>
                new OrmLiteCrudEvents(c.Resolve<IDbConnectionFactory>()));
            // Add support for dynamically generated db rules
            container.AddSingleton<IValidationSource>(c => 
                new OrmLiteValidationSource(c.Resolve<IDbConnectionFactory>()));            
            container.Resolve<IValidationSource>().InitSchema();

            Plugins.Add(new AdminUsersFeature());
            Plugins.Add(new AutoQueryFeature());
            Plugins.Add(new RequestLogsFeature());
            Plugins.Add(new ProfilingFeature());
            Plugins.Add(new AdminRedisFeature());
            Plugins.Add(new AdminDatabaseFeature());
        }
    }

    [Test]
    public void Generate_AppMetadata_details()
    {
        var baseUrl = "http://localhost:20000";
        using var appHost = new AppHost().Init().Start(baseUrl);
        
        var sb = new StringBuilder("import { ApiResult } from './client';\n\n");
        var dtos = baseUrl.CombineWith("/types/typescript").GetStringFromUrl();
        sb.AppendLine(dtos);
        sb.AppendTypeDefinitionFile(filePath:Path.Combine(NetCoreTestsDir, "custom", "types.d.ts"));

        Directory.SetCurrentDirectory(ProjectDir);
        File.WriteAllText(Path.GetFullPath("./lib/types.ts"), sb.ToString());
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
        File.Copy("../NorthwindAuto/node_modules/@servicestack/client/dist/index.d.ts", "../NorthwindAuto/lib/client.d.ts", overwrite:true);

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

public class TestService : Service
{
}



public static class TypeScriptDefinitionUtils
{
    private static string Header = @"import { ApiResult, JsonServiceClient } from './client'
import { App, Meta, Forms, Routes, Breakpoints, Transition, MetadataOperationType, MetadataType, MetadataPropertyType, InputInfo, ThemeInfo, LinkInfo, AuthenticateResponse, AdminUsersInfo } from './shared'
";
    private static string Footer = @"export let App:App;";

    private static Dictionary<string, string> Headers = new()
    {
        ["shared"] = "import { ApiResult } from './client';",
        ["explorer"] = Header + "import { ExplorerRoutes, ExplorerRoutesExtend, ExplorerStore } from './shared';",
        ["locode"] = Header + "import { LocodeRoutes, LocodeRoutesExtend, LocodeStore, LocodeSettings, ApiState, CrudApisState } from './shared';",
        ["admin"] = Header + "import { AdminRoutes, AdminStore } from './shared';",
    };
    
    static FilesTransformer TransformerOptions = new()
    {
        FileExtensions =
        {
            ["ts"] = new FileTransformerOptions
            {
                LineTransformers = new()
                {
                    new RemoveLineStartingWith(new[] { "import " }, ignoreWhiteSpace:false, Run.Always),
                    new RemoveLineStartingWith("export {};", ignoreWhiteSpace:false, Run.Always),
                    new RemoveLineContaining("= import(", Run.Always),
                    new ApplyToLineContaining(": import(", line => $"{line.LeftPart(':')}:{line.LastRightPart('.')}".AsMemory(), Run.Always),
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
            sb.AppendLine(header).AppendLine();
        wipFs.GetAllFiles()
            .Each(file => sb.AppendLine(file.ReadAllText()));

        sb.AppendTypeDefinitionFile(Path.Combine(distFs.RootDirectory.RealPath, "..", "custom", $"{path}.d.ts"));
        
        distFs.WriteFile($"{path}.d.ts", sb.ToString());
    }
    
    public static void AppendTypeDefinitionFile(this StringBuilder sb, string filePath)
    {
        if (!File.Exists(filePath))
            return;
        
        var file = new FileInfo(filePath);
        using var sr = file.OpenRead();
        foreach (var line in sr.ReadLines())
        {
            if (line.StartsWith("import "))
                continue;
            sb.AppendLine(line);
        }
    }

}
