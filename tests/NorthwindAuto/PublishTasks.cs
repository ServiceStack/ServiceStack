using NUnit.Framework;
using ServiceStack;
using ServiceStack.HtmlModules;
using ServiceStack.IO;
using ServiceStack.Text;

namespace MyApp;

public class PublishTasks
{
    readonly string ProjectDir = Path.GetFullPath("../../..");
    string FromModulesDir => Path.GetFullPath(".");
    string ToModulesDir => Path.GetFullPath("../../src/ServiceStack/modules");
    string[] IgnoreUiFiles = {
        "index.css"
    };

    /*  publish.bat
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

        await PublishTailwind();
        CopyUiFiles();
        CopySharedFiles();
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

    public async Task PublishTailwind()
    {
        await ProcessUtils.RunShellAsync("npm run ui:build",
            onOut: Console.WriteLine, 
            onError:Console.Error.WriteLine);
    }

    private void CopySharedFiles()
    {
        FileSystemVirtualFiles.RecreateDirectory(ToModulesDir.CombineWith("shared"));
        PublishFiles(
            new FileSystemVirtualFiles(FromModulesDir.CombineWith("shared")),
            new FileSystemVirtualFiles(ToModulesDir.CombineWith("shared")));
    }

    private void CopyUiFiles()
    {
        FileSystemVirtualFiles.RecreateDirectory(ToModulesDir.CombineWith("ui"));
        PublishFiles(
            new FileSystemVirtualFiles(FromModulesDir.CombineWith("ui")), 
            new FileSystemVirtualFiles(ToModulesDir.CombineWith("ui")), 
            IgnoreUiFiles);
    }

    private void PublishFiles(FileSystemVirtualFiles fromVfs, FileSystemVirtualFiles toVfs, string[]? ignoreFiles = null)
    {
        foreach (var file in fromVfs.GetAllFiles())
        {
            if (ignoreFiles != null && ignoreFiles.Contains(file.VirtualPath)) 
                continue;

            var contents = FileReader.Read(file);
            toVfs.WriteFile(file.VirtualPath, contents);
            $"{file.VirtualPath} ({contents.Length})".Print();
        }
    }
}