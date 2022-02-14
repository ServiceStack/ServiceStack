using System.Reflection;
using Funq;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Admin;
using ServiceStack.Auth;
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
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("ui").AssertDir()), 
            cleanTarget: true,
            ignore: file => IgnoreUiFiles.Contains(file.VirtualPath),
            afterCopy: (file, contents) => $"{file.VirtualPath} ({contents.Length})".Print());
        
        // copy to modules/admin-ui
        transformOptions.CopyAll(
            source: new FileSystemVirtualFiles(FromModulesDir.CombineWith("admin-ui")), 
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("admin-ui").AssertDir()), 
            cleanTarget: true,
            ignore: file => IgnoreAdminUiFiles.Contains(file.VirtualPath),
            afterCopy: (file, contents) => $"{file.VirtualPath} ({contents.Length})".Print());

        // copy to modules/shared
        transformOptions.CopyAll(
            source: new FileSystemVirtualFiles(FromModulesDir.CombineWith("shared")),
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("shared").AssertDir()),
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
            };
            
            Plugins.Add(new AuthFeature(() => new AuthUserSession(), new [] {
                new CredentialsAuthProvider(AppSettings),
            }));
            
            Plugins.Add(new AdminUsersFeature());
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
}