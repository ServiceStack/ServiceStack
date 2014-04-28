namespace ServiceStack.Razor.BuildTask.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    using NUnit.Framework;

    [TestFixture]
    public class RazorBuildTaskTests
    {
        public const string BasePath = "..\\..\\..\\RazorRockstars.BuildTask\\";
        public const string OutputBasePath = BasePath + "obj\\";

        public static TestCaseData GetExecuteTest(string inputRelativePath)
        {
            var outputPath = MimicBuildTaskTargetsOutputFilesTransform(inputRelativePath);
            return new TestCaseData(new[] { inputRelativePath }, new[] { outputPath });
        }

        public static TestCaseData GetExecuteTest(string[] inputRelativePaths)
        {            
            var outputPaths = inputRelativePaths.Select(MimicBuildTaskTargetsOutputFilesTransform).ToArray();
            return new TestCaseData(inputRelativePaths, outputPaths);
        }

        // From BuildTask.targets: OutputFiles="@(RazorViewFiles->'$(IntermediateOutputPath)%(FileName)%(Extension).g.cs')">
        public static string MimicBuildTaskTargetsOutputFilesTransform(string inputFilePath)
        {
            return Path.Combine(OutputBasePath, Path.GetFileName(inputFilePath) + ".g.cs");
        }
        
        public IEnumerable<TestCaseData> IndividualExecuteTestCases
        {
            get
            {
                yield return GetExecuteTest(@"default.cshtml").SetName("Can_Execute_default.cshtml");
                yield return GetExecuteTest(@"Login.cshtml").SetName("Can_Execute_Login.cshtml");
                yield return GetExecuteTest(@"NoModelNoController.cshtml").SetName("Can_Execute_NoModelNoController.cshtml");
                yield return GetExecuteTest(@"NotFound.cshtml").SetName("Can_Execute_NotFound.cshtml");
                yield return GetExecuteTest(@"TypedModelNoController.cshtml").SetName("Can_Execute_TypedModelNoController.cshtml");

                yield return GetExecuteTest(@"stars\alive\_Layout.cshtml").SetName("Can_Execute_stars.alive._Layout.cshtml");
                yield return GetExecuteTest(@"stars\alive\Grohl.cshtml").SetName("Can_Execute_stars.alive.Grohl.cshtml");
                yield return GetExecuteTest(@"stars\alive\Love\default.cshtml").SetName("Can_Execute_stars.alive.Love.default.cshtml");
                yield return GetExecuteTest(@"stars\alive\Springsteen\default.cshtml").SetName("Can_Execute_stars.alive.Springsteen.default.cshtml");
                yield return GetExecuteTest(@"stars\alive\Vedder\default.cshtml").SetName("Can_Execute_stars.alive.Vedder.default.cshtml");

                yield return GetExecuteTest(@"stars\dead\_Layout.cshtml").SetName("Can_Execute_stars.dead._Layout.cshtml");
                yield return GetExecuteTest(@"stars\dead\Cobain\default.cshtml").SetName("Can_Execute_stars.dead.Cobain.default.cshtml");
                yield return GetExecuteTest(@"stars\dead\Hendrix\default.cshtml").SetName("Can_Execute_stars.dead.Hendrix.default.cshtml");
                yield return GetExecuteTest(@"stars\dead\Jackson\default.cshtml").SetName("Can_Execute_stars.dead.Jackson.default.cshtml");
                yield return GetExecuteTest(@"stars\dead\Joplin\default.cshtml").SetName("Can_Execute_stars.dead.Joplin.default.cshtml");
                yield return GetExecuteTest(@"stars\dead\Presley\default.cshtml").SetName("Can_Execute_stars.dead.Presley.default.cshtml");
                
                yield return GetExecuteTest(@"Views\_Layout.cshtml").SetName("Can_Execute_Views._Layout.cshtml");
                yield return GetExecuteTest(@"Views\AngularJS.cshtml").SetName("Can_Execute_Views.AngularJS.cshtml");
                yield return GetExecuteTest(@"Views\Rockstars.cshtml").SetName("Can_Execute_Views.Rockstars.cshtml");
                
                yield return GetExecuteTest(@"Views\Shared\Empty.cshtml").SetName("Can_Execute_Views.Shared.Empty.cshtml");
                yield return GetExecuteTest(@"Views\Shared\HtmlReport.cshtml").SetName("Can_Execute_Views.Shared.HtmlReport.cshtml");
                yield return GetExecuteTest(@"Views\Shared\MenuAlive.cshtml").SetName("Can_Execute_Views.Shared.MenuAlive.cshtml");
                yield return GetExecuteTest(@"Views\Shared\MenuDead.cshtml").SetName("Can_Execute_Views.Shared.MenuDead.cshtml");
                yield return GetExecuteTest(@"Views\Shared\OtherPages.cshtml").SetName("Can_Execute_Views.Shared.OtherPages.cshtml");
                yield return GetExecuteTest(@"Views\Shared\SimpleLayout.cshtml").SetName("Can_Execute_Views.Shared.SimpleLayout.cshtml");
            }
        }

        public IEnumerable<TestCaseData> SpecificExecuteTestCases
        {
            get
            {
                yield return GetExecuteTest(new string[] { }).SetName("Does_Execute_PassSilently_WhenThereAreNoInputFiles");
                yield return GetExecuteTest(@"stars\alive\_Layout.cshtml").SetName("Can_Execute_WhenFileIsInSubFolder");
                yield return GetExecuteTest(new[] { @"stars\alive\_Layout.cshtml", @"Views\_Layout.cshtml" }).SetName("Can_Execute_TwoFilesWithTheSameName_WithNoCollisions");
                yield return GetExecuteTest(new[] { @"TypedModelNoController.cshtml", @"stars\alive\Vedder\default.cshtml", @"Views\Shared\Empty.cshtml" }).SetName("Can_Execute_MultipleFiles");
            }
        }

        [Test, TestCaseSource("IndividualExecuteTestCases"), TestCaseSource("SpecificExecuteTestCases")]
        public void Can_Execute(string[] inputFiles, string[] outputFiles)
        {
            var task = new RazorGeneratorBuildTask
                           {
                               ProjectDir = BasePath,
                               ProjectTargetPath = BasePath + "bin\\Debug\\RazorRockstars.BuildTask.dll",
                               ProjectRootNamespace = "RazorRockstars",
                               InputFiles = inputFiles.Select(s => (ITaskItem)new TaskItem(s)).ToArray(),
                               OutputFiles = outputFiles.Select(s => (ITaskItem)new TaskItem(s)).ToArray()
                           };

            var result = task.Execute();

            Assert.That(result, Is.True);

            foreach (var output in task.OutputFiles)
                Assert.That(File.Exists(output.ItemSpec), Is.True);
        }

        public IEnumerable<TestCaseData> ToUniqueFilePathTestCases
        {
            get
            {
                yield return new TestCaseData(@"obj\_Layout.cshtml", "Views").SetName("Can_ToUniqueFilePath_WhenNamespaceIsSet").Returns(@"obj\Views._Layout.cshtml");
                yield return new TestCaseData(@"obj\_Layout.cshtml", null).SetName("Can_ToUniqueFilePath_WhenNamespaceisNull").Returns(@"obj\_Layout.cshtml");
                yield return new TestCaseData(@"obj\_Layout.cshtml", String.Empty).SetName("Can_ToUniqueFilePath_WhenNamespaceisEmpty").Returns(@"obj\_Layout.cshtml");
            }
        }

        [Test, TestCaseSource("ToUniqueFilePathTestCases")]
        public string Does_ToUniqueFilePath_OutputCorrectFilePath(string filePath, string @namespace)
        {
            return RazorGeneratorBuildTask.ToUniqueFilePath(filePath, @namespace);
        }

        public IEnumerable<TestCaseData> ConfigurationMatchesTestCases
        {
            get
            {
                yield return new TestCaseData(null, null).SetName("Does_ConfigurationMatches_ReturnTrue_WhenNullCurrentConfigAndNullAllowedConfig").Returns(true);
                yield return new TestCaseData(String.Empty, String.Empty).SetName("Does_ConfigurationMatches_ReturnTrue_WhenEmptyCurrentConfigAndEmptyAllowedConfig").Returns(true);
                
                yield return new TestCaseData(null, "DEBUG,RELEASE").Returns(false).SetName("Does_ConfigurationMatches_ReturnFalse_WhenCurrentConfigIsNullAndAllowedConfigsIsNotNullOrEmpty");
                yield return new TestCaseData("DEBUG", null).Returns(true).SetName("Does_ConfigurationMatches_ReturnTrue_WhenCurrentConfigSetAndAllowedConfigsNull");
                
                yield return new TestCaseData(String.Empty, "DEBUG,RELEASE").Returns(false).SetName("Does_ConfigurationMatches_ReturnFalse_WhenCurrentConfigIsEmptyAndAllowedConfigsIsNotNullOrEmpty");
                yield return new TestCaseData("DEBUG", String.Empty).Returns(true).SetName("Does_ConfigurationMatches_ReturnTrue_WhenCurrentConfigSetAndAllowedConfigsEmpty");
                
                yield return new TestCaseData("DEBUG", "DEBUG").SetName("Does_ConfigurationMatches_ReturnTrue_WhenCurrentConfigAndAllowedConfigMatchExactly").Returns(true);
                yield return new TestCaseData("DEBUG", "DEBOG").SetName("Does_ConfigurationMatches_ReturnFalse_WhenCurrentConfigAndAllowedConfigDoNotMatch").Returns(false);
                yield return new TestCaseData("DeBuG", "dEbUg").SetName("Does_ConfigurationMatches_IgnoreCase").Returns(true);
                
                yield return new TestCaseData("DEBUG", "Debug,Release").SetName("Does_ConfigurationMatches_WhenCurrentConfigIsFirstInAllowedConfigsList").Returns(true);
                yield return new TestCaseData("RELEASE", "Debug,Mono,Release").SetName("Does_ConfigurationMatches_WhenCurrentConfigIsNotFirstInAllowedConfigsList").Returns(true);
                yield return new TestCaseData("RELEASE", "Debug,Mono").SetName("Does_ConfigurationMatches_WhenCurrentConfigIsNotInAllowedConfigsList").Returns(false);

                yield return new TestCaseData("DEBUG", "Debug, Release").SetName("Does_ConfigurationMatches_WhenCurrentConfigIsFirstInAllowedConfigsList_AndListHasASpace").Returns(true);
                yield return new TestCaseData("RELEASE", "Debug, Mono, Release").SetName("Does_ConfigurationMatches_WhenCurrentConfigIsNotFirstInAllowedConfigsList_AndListHasASpace").Returns(true);
                yield return new TestCaseData("RELEASE", "Debug, Mono").SetName("Does_ConfigurationMatches_WhenCurrentConfigIsNotInAllowedConfigsList_AndListHasASpace").Returns(false);
            }
        }

        [Test, TestCaseSource("ConfigurationMatchesTestCases")]
        public bool Can_ConfigurationMatches(string currentConfiguration, string allowedConfigs)
        {
            return RazorGeneratorBuildTask.ConfigurationMatches(currentConfiguration, allowedConfigs);
        }

        [Test]
        public void Can_GetPageBaseTypeName()
        {
            var result = new RazorGeneratorBuildTask().GetPageBaseTypeName();
            Assert.That(result, Is.EqualTo("ServiceStack.Razor.ViewPage"));
        }
    }
}
