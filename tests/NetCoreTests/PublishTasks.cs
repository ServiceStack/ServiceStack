using NUnit.Framework;
using ServiceStack;
using ServiceStack.HtmlModules;
using ServiceStack.IO;
using ServiceStack.Text;

namespace NetCoreTests;

[Category("Publish Tasks")]
public class PublishTasks
{
    readonly string ProjectDir = Path.GetFullPath("../../../../NorthwindAuto");
    string FromModulesDir => Path.GetFullPath(".");
    string ToModulesDir => Path.GetFullPath("../../src/ServiceStack/modules");
    string[] IgnoreUiFiles = { "index.css" };

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

        // copy to modules/shared
        transformOptions.CopyAll(
            source: new FileSystemVirtualFiles(FromModulesDir.CombineWith("shared")),
            target: new FileSystemVirtualFiles(ToModulesDir.CombineWith("shared")),
            cleanTarget: true,
            afterCopy: (file, contents) => $"{file.VirtualPath} ({contents.Length})".Print());
    }

    [Test]
    public void Publish_Bookings_cs()
    {
        Directory.SetCurrentDirectory(ProjectDir);
        var bookingsName = "Bookings.cs";
        var toFolders = new[]
        {
            "../../tests/ServiceStack.Blazor.Tests/ServiceModel/",
            "../../../NetCoreTemplates/blazor-wasm/MyApp.ServiceModel/",
            "../../../NetCoreTemplates/vue-vite/api/MyApp.ServiceModel/",
            "../../../NetCoreTemplates/vue-ssg/api/MyApp.ServiceModel/",
            "../../../NetCoreTemplates/nextjs/api/MyApp.ServiceModel/",
        };
        
        var copyFile = "ServiceModel".CombineWith(bookingsName);
        $"Copying {Path.GetFullPath(copyFile)}...".Print();
        foreach (var folder in toFolders)
        {
            var toFile = Path.GetFullPath(folder.CombineWith(bookingsName));
            $"Writing to {toFile}".Print();
            File.Copy(copyFile, toFile, overwrite:true);
        }
    }
}