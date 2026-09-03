using System;
using NUnit.Framework;
using ServiceStack.GoogleCloud;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class GoogleCloudUnitTests
{
    [Test]
    public void GoogleCloudConfig_Defaults_Are_Expected()
    {
        var config = new GoogleCloudConfig();
        Assert.That(config.RecognizerModel, Is.EqualTo("latest_short"));
        Assert.That(config.RecognizerLanguageCodes, Is.EqualTo(new[] { "en-US", "en-AU" }));
        Assert.That(config.Project, Is.Null);
        Assert.That(config.Location, Is.Null);
        Assert.That(config.Bucket, Is.Null);
    }

    [Test]
    public void GoogleCloudConfig_Clone_Copies_All_Properties()
    {
        var original = new GoogleCloudConfig
        {
            Project = "my-project",
            Location = "us-central1",
            Bucket = "my-bucket",
            PhraseSetId = "phrases-1",
            RecognizerId = "rec-1",
            RecognizerModel = "telephony",
            RecognizerLanguageCodes = new[] { "en-GB" }
        };

        var clone = original.Clone();

        Assert.That(clone.Project, Is.EqualTo(original.Project));
        Assert.That(clone.Location, Is.EqualTo(original.Location));
        Assert.That(clone.Bucket, Is.EqualTo(original.Bucket));
        Assert.That(clone.PhraseSetId, Is.EqualTo(original.PhraseSetId));
        Assert.That(clone.RecognizerId, Is.EqualTo(original.RecognizerId));
        Assert.That(clone.RecognizerModel, Is.EqualTo(original.RecognizerModel));
        Assert.That(clone.RecognizerLanguageCodes, Is.EqualTo(original.RecognizerLanguageCodes));
    }

    [Test]
    public void GoogleCloudConfig_ToSpeechToTextConfig_Throws_When_Required_Props_Missing()
    {
        var config = new GoogleCloudConfig();
        Assert.Throws<ArgumentNullException>(() => config.ToSpeechToTextConfig());

        config.Project = "proj";
        Assert.Throws<ArgumentNullException>(() => config.ToSpeechToTextConfig());

        config.Location = "us-central1";
        Assert.Throws<ArgumentNullException>(() => config.ToSpeechToTextConfig());

        config.Bucket = "bucket";
        var result = config.ToSpeechToTextConfig(c => c.RecognizerModel = "custom");
        Assert.That(result.RecognizerModel, Is.EqualTo("custom"));
        Assert.That(result.Project, Is.EqualTo("proj"));
    }

    [Test]
    public void GoogleCloudConfig_ToRecognitionConfig_Maps_Correctly()
    {
        var config = new GoogleCloudConfig
        {
            RecognizerModel = "latest_long",
            RecognizerLanguageCodes = new[] { "fr-FR", "es-ES" }
        };

        var recConfig = config.ToRecognitionConfig();
        Assert.That(recConfig.Model, Is.EqualTo("latest_long"));
        Assert.That(recConfig.AutoDecodingConfig, Is.Not.Null);
        Assert.That(recConfig.LanguageCodes, Contains.Item("fr-FR"));
        Assert.That(recConfig.LanguageCodes, Contains.Item("es-ES"));
    }

    [Test]
    public void GoogleCloudVirtualFiles_Constructor_Guards_Nulls()
    {
        Assert.Throws<ArgumentNullException>(() => new GoogleCloudVirtualFiles(null!, "bucket"));
    }

    [Test]
    public void GoogleCloudVirtualFiles_SanitizePath_Works_As_Expected()
    {
        var vfs = (GoogleCloudVirtualFiles)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GoogleCloudVirtualFiles));

        Assert.That(vfs.SanitizePath(null), Is.Null);
        Assert.That(vfs.SanitizePath(""), Is.Null);
        Assert.That(vfs.SanitizePath("/folder/file.txt"), Is.EqualTo("folder/file.txt"));
        Assert.That(vfs.SanitizePath("folder\\sub\\file.txt"), Is.EqualTo("folder/sub/file.txt"));
        Assert.That(vfs.SanitizePath("\\folder\\file.txt"), Is.EqualTo("folder/file.txt"));
        Assert.That(vfs.SanitizePath("file.txt"), Is.EqualTo("file.txt"));
    }

    [Test]
    public void GoogleCloudVirtualFiles_GetFileName_Works_As_Expected()
    {
        Assert.That(GoogleCloudVirtualFiles.GetFileName(null), Is.Null);
        Assert.That(GoogleCloudVirtualFiles.GetFileName("file.txt"), Is.EqualTo("file.txt"));
        Assert.That(GoogleCloudVirtualFiles.GetFileName("a/b/c.json"), Is.EqualTo("c.json"));
    }

    [Test]
    public void GoogleCloudVirtualFiles_GetDirPath_Works_As_Expected()
    {
        var vfs = (GoogleCloudVirtualFiles)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GoogleCloudVirtualFiles));

        Assert.That(vfs.GetDirPath(null), Is.Null);
        Assert.That(vfs.GetDirPath(""), Is.Null);
        Assert.That(vfs.GetDirPath("file.txt"), Is.Null);
        Assert.That(vfs.GetDirPath("folder/file.txt"), Is.EqualTo("folder"));
        Assert.That(vfs.GetDirPath("a/b/c.txt"), Is.EqualTo("a/b"));
    }

    [Test]
    public void GoogleCloudVirtualFiles_GetImmediateSubDirPath_Works_As_Expected()
    {
        var vfs = (GoogleCloudVirtualFiles)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GoogleCloudVirtualFiles));

        Assert.That(vfs.GetImmediateSubDirPath(null, null), Is.Null);
        Assert.That(vfs.GetImmediateSubDirPath(null, "folder"), Is.EqualTo("folder"));
        Assert.That(vfs.GetImmediateSubDirPath(null, "folder/sub"), Is.EqualTo("folder"));
        Assert.That(vfs.GetImmediateSubDirPath("folder", "folder/sub"), Is.EqualTo("folder/sub"));
        Assert.That(vfs.GetImmediateSubDirPath("folder", "folder/sub/nested"), Is.Null);
        Assert.That(vfs.GetImmediateSubDirPath("folder", "other/sub"), Is.Null);
    }

    [Test]
    public void GoogleCloudVirtualDirectory_Properties_And_VirtualPath_Behave_Correctly()
    {
        var vfs = (GoogleCloudVirtualFiles)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GoogleCloudVirtualFiles));

        var rootDir = new GoogleCloudVirtualDirectory(vfs, null, null);
        Assert.That(rootDir.DirPath, Is.Null);
        Assert.That(rootDir.VirtualPath, Is.EqualTo(string.Empty));
        Assert.That(rootDir.Name, Is.Null);

        var subDir = new GoogleCloudVirtualDirectory(vfs, "docs/sub", rootDir);
        Assert.That(subDir.DirPath, Is.EqualTo("docs/sub"));
        Assert.That(subDir.VirtualPath, Is.EqualTo("docs/sub"));
        Assert.That(subDir.Name, Is.EqualTo("sub"));
    }
}
