using Funq;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Admin;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.HtmlModules;
using ServiceStack.IO;
using ServiceStack.Jobs;
using ServiceStack.NativeTypes;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using System.Text;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
            
            // ["marked.mjs"] = "https://cdn.jsdelivr.net/npm/marked/lib/marked.esm.min.js",
            // ["vue-router.mjs"] = "https://unpkg.com/vue-router@4/dist/vue-router.esm-browser.prod.js",
            // ["idb.mjs"] = "https://cdn.jsdelivr.net/npm/idb/+esm",
            
            // ["chart.js"] = "https://cdn.jsdelivr.net/npm/chart.js/+esm",
            // ["color.js"] = "https://cdn.jsdelivr.net/npm/@kurkle/color/+esm",
            //https://cdn.jsdelivr.net/npm/chart.js@4.4.8/dist/chunks/helpers.segment.js
            //["chart.plugin.datalabels.js"] = "https://cdn.jsdelivr.net/npm/chartjs-plugin-datalabels@2",
        };

        var jsDir = "../../src/ServiceStack/js";

        foreach (var jsFile in jsFiles)
        {
            var toFile = Path.GetFullPath(jsDir.CombineWith(jsFile.Key));
            if (jsFile.Value.StartsWith("https://"))
            {
                $"GET {jsFile.Value}".Print();
                var js = jsFile.Value.GetStringFromUrl();

                if (jsFile.Key == "chart.js")
                {
                    js = js.Replace("/npm/@kurkle/color@0.3.4/+esm", "color.js");
                }
                
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
        
        // modules are copied over as debug versions then minified/cached at runtime on load

        // copy to modules/shared
        transformOptions.CopyAll(
            source: new FileSystemVirtualFiles(FromModulesDir.CombineWith("shared")),
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("shared")),
            cleanTarget: true,
            afterCopy: (file, contents) => $"{file.VirtualPath} ({contents.Length})".Print());

        // copy to js
        // gets served as a static file so need to copy prod version to /js
        FilesTransformer.Defaults(debugMode:false).CopyAll(
            source: new FileSystemVirtualFiles(FromModulesDir.CombineWith("wwwroot/js")),
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("../js")),
            cleanTarget: true,
            afterCopy: (file, contents) => $"{file.VirtualPath} ({contents.Length})".Print());

        FilesTransformer.Defaults(debugMode:false).CopyAll(
            source: new FileSystemVirtualFiles(FromModulesDir.CombineWith("wwwroot/css/")),
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("../css")),
            cleanTarget: true,
            afterCopy: (file, contents) => $"{file.VirtualPath} ({contents.Length})".Print());


        // copy to modules/locode
        var moduleOptions = FilesTransformer.Defaults(debugMode: true);
        moduleOptions.FileExtensions["html"].LineTransformers = FilesTransformer.HtmlModuleLineTransformers.ToList();
        moduleOptions.CopyAll(
            source: new FileSystemVirtualFiles(FromModulesDir.CombineWith("locode")), 
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("locode")), 
            cleanTarget: true,
            ignore: file => IgnoreUiFiles.Contains(file.VirtualPath),
            afterCopy: (file, contents) => $"{file.VirtualPath} ({contents.Length})".Print());

        // copy to modules/admin-ui
        moduleOptions.CopyAll(
            source: new FileSystemVirtualFiles(FromModulesDir.CombineWith("admin-ui")), 
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("admin-ui")), 
            cleanTarget: true,
            ignore: file => IgnoreAdminUiFiles.Contains(file.VirtualPath),
            afterCopy: (file, contents) => $"{file.VirtualPath} ({contents.Length})".Print());

        moduleOptions.CopyAll(
            source: new FileSystemVirtualFiles(FromModulesDir.CombineWith("ui")), 
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("ui")), 
            cleanTarget: true,
            ignore: file => IgnoreUiFiles.Contains(file.VirtualPath),
            afterCopy: (file, contents) => $"{file.VirtualPath} ({contents.Length})".Print());
        
        // copy to /Templates/HtmlFormat.html
        moduleOptions.CopyAll(
            source: new FileSystemVirtualFiles(FromModulesDir.CombineWith("wwwroot/Templates/")),
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("../Templates")),
            cleanTarget: true,
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
        
        string[] ResolveTargetDirs(string name) =>
        [
            $"../../tests/ServiceStack.Blazor.Tests/" + (name == "ServiceModel" ? name : "Server/" + name) + "/",
            $"../../../NetCoreTemplates/blazor-wasm/MyApp.{name}/",
            $"../../../NetCoreTemplates/vue-vite/api/MyApp.{name}/",
            $"../../../NetCoreTemplates/vue-ssg/api/MyApp.{name}/",
            $"../../../NetCoreTemplates/nextjs/api/MyApp.{name}/"
        ];

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
        public AppHost() : base(nameof(PublishTasks), typeof(MetadataAppService), typeof(UiServices), typeof(AdminServices)) {}
        public override void Configure(Container container)
        {
            Metadata.ForceInclude =
            [
                typeof(MetadataApp),
                typeof(AppMetadata),
                typeof(AdminQueryUsers),typeof(AdminUsersResponse),typeof(AdminGetUser),typeof(AdminCreateUser),typeof(AdminUpdateUser),typeof(AdminDeleteUser),

                typeof(AdminGetRoles),typeof(AdminGetRolesResponse),typeof(AdminGetRole),typeof(AdminGetRoleResponse),
                typeof(AdminCreateRole),typeof(AdminUpdateRole),typeof(AdminDeleteRole),

                typeof(GetCrudEvents),
                typeof(GetValidationRules),
                typeof(ModifyValidationRules),
                typeof(RequestLogs),
                typeof(GetAnalyticsReports), typeof(GetAnalyticsInfo), typeof(AnalyticsReports),
                typeof(AdminDashboard),
                typeof(AdminProfiling),
                typeof(AdminRedis),
                typeof(AdminDatabase),
                typeof(AdminQueryApiKeys),
                typeof(AdminCreateApiKey),
                typeof(AdminUpdateApiKey),
                typeof(AdminDeleteApiKey),
                typeof(RequestLogsInfo),
                typeof(ViewCommands),
                typeof(ExecuteCommand),
                
                ..UiServices.AutoQueryTypes,
                ..UiServices.BackgroundJobTypes,
            ];
            
            UiServices.BackgroundJobTypes.Each(x =>
            {
                NativeTypesService.BuiltInClientDtos.RemoveAll(x => UiServices.BackgroundJobTypes.Contains(x));
            });
            
            Plugins.Add(new AuthFeature(() => new AuthUserSession(), [
                new CredentialsAuthProvider(AppSettings)
            ]));
            
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
            Plugins.Add(new RequestLogsFeature {
                RequestLogger = new SqliteRequestLogger()
            });
            Plugins.Add(new ProfilingFeature());
            Plugins.Add(new AdminRedisFeature());
            Plugins.Add(new AdminDatabaseFeature());
            Plugins.Add(new CommandsFeature());
            Plugins.Add(new ApiKeysFeature());
            Plugins.Add(new BackgroundsJobFeature { EnableAdmin = true });
        }
    }

    [Test]
    public void Generate_AppMetadata_details()
    {
        var baseUrl = "http://localhost:20000";
        using var appHost = new AppHost().Init().Start(baseUrl);
        
        Thread.Sleep(20000);
        
        var sb = new StringBuilder("import { ApiResult } from './client';\n\n");
        var dtos = baseUrl.CombineWith("/types/typescript").GetStringFromUrl();
        sb.AppendLine(dtos);
        sb.AppendTypeDefinitionFile(filePath:Path.Combine(NetCoreTestsDir, "custom", "types.d.ts"));

        var mjs = baseUrl.CombineWith("/types/mjs").GetStringFromUrl();

        Directory.SetCurrentDirectory(ProjectDir);
        File.WriteAllText(Path.GetFullPath("./lib/types.ts"), sb.ToString());
        
        File.WriteAllText(Path.GetFullPath("./admin-ui/lib/dtos.mjs"), mjs);
        
        var origMetadata = baseUrl.CombineWith("/api/AdminMetadataTypes").GetStringFromUrl();
        var elMetadata = JsonSerializer.Deserialize<JsonElement>(origMetadata);
        var metadata = JsonSerializer.Serialize(elMetadata, new JsonSerializerOptions { WriteIndented = true });

        var metadataJs = $"export default {metadata}";
        File.WriteAllText(Path.GetFullPath("./admin-ui/lib/metadata.mjs"), metadataJs);
        
        File.WriteAllText(Path.GetFullPath("./wwwroot/dtos/dtos.mjs"), mjs);
        File.WriteAllText(Path.GetFullPath("./wwwroot/dtos/dtos.ts"), dtos);

        var python = baseUrl.CombineWith("/types/python").GetStringFromUrl();
        File.WriteAllText(Path.GetFullPath("./wwwroot/dtos/dtos.py"), python);

        var dart = baseUrl.CombineWith("/types/dart").GetStringFromUrl();
        File.WriteAllText(Path.GetFullPath("./wwwroot/dtos/dtos.dart"), dart);

        var php = baseUrl.CombineWith("/types/php").GetStringFromUrl();
        File.WriteAllText(Path.GetFullPath("./wwwroot/dtos/dtos.php"), php);

        var java = baseUrl.CombineWith("/types/java").GetStringFromUrl();
        File.WriteAllText(Path.GetFullPath("./wwwroot/dtos/dtos.java"), java);

        var kotlin = baseUrl.CombineWith("/types/kotlin").GetStringFromUrl();
        File.WriteAllText(Path.GetFullPath("./wwwroot/dtos/dtos.kt"), kotlin);

        var swift = baseUrl.CombineWith("/types/swift").GetStringFromUrl();
        File.WriteAllText(Path.GetFullPath("./wwwroot/dtos/dtos.swift"), swift);

        var fsharp = baseUrl.CombineWith("/types/fsharp").GetStringFromUrl();
        File.WriteAllText(Path.GetFullPath("./wwwroot/dtos/dtos.fs"), fsharp);

        var vb = baseUrl.CombineWith("/types/vbnet").GetStringFromUrl();
        File.WriteAllText(Path.GetFullPath("./wwwroot/dtos/dtos.vb"), vb);
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
        await memFs.WriteFileAsync("0_" + typesFile.Name, typesFile);
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

[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminMetadataTypes : IGet, IReturn<MetadataTypes> {}


public class UiServices : Service
{
    public static Type[] AutoQueryTypes =
    [
        typeof(AdminQueryBackgroundJobs),
        typeof(AdminQueryJobSummary),
        typeof(AdminQueryScheduledTasks),
        typeof(AdminQueryCompletedJobs),
        typeof(AdminQueryFailedJobs),
        typeof(AdminQueryRequestLogs),
    ];
    
    public static Type[] BackgroundJobTypes =
    [
        typeof(AdminJobInfo),
        typeof(AdminGetJob),
        typeof(AdminGetJobProgress),
        typeof(AdminCancelJobs),
        typeof(AdminRequeueFailedJobs),
        typeof(AdminJobDashboard),
        typeof(AdminQueryBackgroundJobs),
        typeof(AdminQueryJobSummary),
        typeof(AdminQueryScheduledTasks),
        typeof(AdminQueryCompletedJobs),
        typeof(AdminQueryFailedJobs),
        typeof(AdminQueryRequestLogs),

        typeof(BackgroundJobBase),
        typeof(BackgroundJobOptions),
        typeof(BackgroundJob),
        typeof(JobSummary),
        typeof(ScheduledTask),
        typeof(CompletedJob),
        typeof(FailedJob),
        typeof(WorkerStats),
    ];

    // APIs using AutoForm APIs
    public static Type[] AdminAuthTypes =
    [
        typeof(AdminCreateRole),
        typeof(AdminUpdateRole),
    ];
    
    // Generate app metadata.js required for all AutoQuery APIs in built-in UIs
    public object Any(AdminMetadataTypes request)
    {
        var meta = new ServiceMetadata([]);
        Type[] requestTypes = AutoQueryTypes;
        foreach (var requestType in requestTypes)
        {
            var returnMarker = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IReturn<>));
            var responseType = returnMarker?.GetGenericArguments()[0];
            meta.Add(typeof(AdminJobServices), requestType, responseType);
        }

        foreach (var requestType in AdminAuthTypes)
        {
            var returnMarker = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IReturn<>));
            var responseType = returnMarker?.GetGenericArguments()[0];
            meta.Add(typeof(AdminServices), requestType, responseType);
        }

        var config = new MetadataTypesConfig
        {
            AddNamespaces = [],
            DefaultNamespaces = [],
            DefaultImports = [],
            IncludeTypes = [],
            ExcludeTypes = [],
            ExportTags = [],
            TreatTypesAsStrings = [],
            IgnoreTypes = [],
            ExportTypes = [],
            ExportAttributes = [],
            IgnoreTypesInNamespaces = [],
        };
        var generator = new MetadataTypesGenerator(meta, config);
        var to = generator.GetMetadataTypes(Request);
        to.Config = null;
        return to;
    }
}

public class AdminServices : Service
{
    public object Any(AdminCreateRole request) => request;
    public object Any(AdminGetRoles request) => request;
    public object Any(AdminGetRole request) => request;
    public object Any(AdminUpdateRole request) => request;
    public object Any(AdminDeleteRole request) => request;
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
