#nullable enable

using System;
using System.IO;
using System.Xml.Linq;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Script;
using ServiceStack.VirtualPath;

namespace ServiceStack.Extensions.Tests;

public class TestVirtualPathProvider : AbstractVirtualPathProviderBase
{
    public override IVirtualDirectory RootDirectory => null!;
    public override string VirtualPathSeparator => "/";
    public override string RealPathSeparator => Path.DirectorySeparatorChar.ToString();

    protected override void Initialize() {}
}

[TestFixture]
public class CommonSecurityAndBugTests
{
    [Test]
    public void FindExePath_Locates_Existing_Binary_And_ProtectedScripts_Delegates_Correctly()
    {
        var exePath = ProcessUtils.FindExePath("dotnet");
        Assert.That(exePath, Is.Not.Null);
        Assert.That(File.Exists(exePath), Is.True);

        var protectedExePath = ProtectedScripts.Instance.exePath("dotnet");
        Assert.That(protectedExePath, Is.EqualTo(exePath));

        var nonExistent = ProcessUtils.FindExePath("non_existent_binary_xyz_123");
        Assert.That(nonExistent, Is.Null);
    }

    [Test]
    public void XLinqExtensions_FirstElement_Handles_Null_Empty_And_Comments_Safely()
    {
        XElement? nullElement = null;
        Assert.That(nullElement.FirstElement(), Is.Null);

        var emptyElement = new XElement("root");
        Assert.That(emptyElement.FirstElement(), Is.Null);

        var xmlWithComments = XElement.Parse("<root><!-- a comment --><child id='1'/></root>");
        var firstChild = xmlWithComments.FirstElement();
        Assert.That(firstChild, Is.Not.Null);
        Assert.That(firstChild!.Name.LocalName, Is.EqualTo("child"));
        Assert.That(firstChild.GetStringAttributeOrDefault("id"), Is.EqualTo("1"));
    }

    [Test]
    public void FileSystemVirtualDirectory_EnumerateDirectories_Handles_Empty_DirName()
    {
        var tempDir = new DirectoryInfo(Path.GetTempPath());
        var vfsDir = new FileSystemVirtualDirectory(new TestVirtualPathProvider(), null, tempDir);

        Assert.DoesNotThrow(() =>
        {
            var results = vfsDir.EnumerateDirectories(string.Empty);
            Assert.That(results, Is.Not.Null);
        });
    }

    [Test]
    public void AbstractVirtualPathProviderBase_SanitizePath_Normalizes_Backslashes()
    {
        var provider = new TestVirtualPathProvider();

        Assert.That(provider.SanitizePath(null!), Is.Null);
        Assert.That(provider.SanitizePath(""), Is.Null);
        Assert.That(provider.SanitizePath("/folder/file.txt"), Is.EqualTo("folder/file.txt"));
        Assert.That(provider.SanitizePath(@"\folder\file.txt"), Is.EqualTo("folder/file.txt"));
        Assert.That(provider.SanitizePath(@"\\server\share\file.txt"), Is.EqualTo("server/share/file.txt"));
    }

    [Test]
    public void ProtectedScripts_Default_Handles_NonGeneric_Types_Gracefully()
    {
        var context = new ScriptContext().Init();
        var scripts = new ProtectedScripts { Context = context };

        // Non-generic type
        var intDefault = scripts.@default("int");
        Assert.That(intDefault, Is.EqualTo(0));

        // Malformed generic type string shouldn't crash with ArgumentOutOfRangeException
        Assert.Throws<NotSupportedException>(() => scripts.@default("NonExistent<TypeWithoutEnd"));
    }
}
