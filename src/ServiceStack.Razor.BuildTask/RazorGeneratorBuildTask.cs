using System.Xml.Linq;
using ServiceStack.Razor.Compilation.CodeTransformers;

namespace ServiceStack.Razor.BuildTask
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;

    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.CSharp;

    using ServiceStack.Razor.BuildTask.Support;
    using ServiceStack.Razor.Compilation;
    using ServiceStack.Razor.Managers.RazorGen;

    public class RazorGeneratorBuildTask : Task
    {
        public static RazorGeneratorBuildTask Instance;

        public override bool Execute()
        {
            if (!this.InputFiles.Any())
                return true;

            Instance = new RazorGeneratorBuildTask();

            if (AppConfigPath == null)
            {
                var configNames = new[] {
                    PathUtils.CombinePaths(ProjectDir, "Web.config"),
                    PathUtils.CombinePaths(ProjectDir, "App.config"),
                    PathUtils.CombinePaths(ProjectDir, "web.config"), //unix
                    PathUtils.CombinePaths(ProjectDir, "app.config"),
                };
                AppConfigPath = configNames.FirstOrDefault(File.Exists);
            }

            // Use the task's parent project's web/app configuration file
            using (AppConfigScope.Change(AppConfigPath))
            {
                var allowedConfigs = ConfigurationManager.AppSettings[ConfigurationAppKeyName] ?? this.AllowedConfigurations;

                // If specified, only generate source code if the Project Configuration matches the given Configuration from the user
                if (!ConfigurationMatches(this.ProjectConfiguration, allowedConfigs))
                    return true;

                var pageBaseTypeName = GetPageBaseTypeName();
                var pathProvider = new RazorBuildPathProvider(this.ProjectDir);
                var transformer = new RazorViewPageTransformer(pageBaseTypeName);

                for (int i = 0; i < this.InputFiles.Length; i++)
                {
                    var file = new RazorBuildTaskFile(this.InputFiles[i].ItemSpec, pathProvider);
                    var pageHost = new RazorPageHost(pathProvider, file, transformer, new CSharpCodeProvider(), new Dictionary<string, string>())
                    {
                        RootNamespace = this.ProjectRootNamespace
                    };

                    var fileName = this.OutputFiles[i].ItemSpec = ToUniqueFilePath(this.OutputFiles[i].ItemSpec, pageHost.DefaultNamespace);
                    var sourceCode = pageHost.GenerateSourceCode();

                    File.WriteAllText(fileName, sourceCode);
                }

                return true;
            }
        }

        /// <summary>
        /// Prevents file path collisions when two file paths in different folders have the same name - eg: _layout.cs & Views\_layout.cs
        /// by using a fully qualified name - eg: $(RootNamespace).Views._layout.cs
        /// </summary>
        /// <param name="filePath">
        /// The output file path.
        /// </param>
        /// <param name="namespace">
        /// The namespace used in file path's source code.
        /// </param>
        /// <returns>
        /// The unique output file path containing the file's namespace
        /// </returns>
        public static string ToUniqueFilePath(string filePath, string @namespace)
        {
            if (!String.IsNullOrEmpty(@namespace))
                @namespace += ".";

            return String.Format(
                @"{0}\{1}{2}{3}",
                Path.GetDirectoryName(filePath),
                @namespace,
                Path.GetFileNameWithoutExtension(filePath),
                Path.GetExtension(filePath));
        }

        /// <summary>
        /// Gets Razor PageBaseType name from the RazorWebPagesSection of Web/App.config.
        /// </summary>
        /// <returns>
        /// The PageBaseType name.
        /// </returns>
        public string GetPageBaseTypeName()
        {
            if (AppConfigPath != null)
            {
                var xml = AppConfigPath.ReadAllText();
                var doc = XElement.Parse(xml);
                var pageBaseType = doc.AnyElement("system.web.webPages.razor")
                    .AnyElement("pages")
                        .AnyAttribute("pageBaseType");

                var razorNamespaces = new HashSet<string>();
                doc.AnyElement("system.web.webPages.razor")
                    .AnyElement("pages")
                        .AnyElement("namespaces")
                            .AllElements("add").ToList()
                                .ForEach(x => razorNamespaces.Add(x.AnyAttribute("namespace").Value));

                WebConfigTransformer.RazorNamespaces = razorNamespaces;

                if (pageBaseType != null && !string.IsNullOrEmpty(pageBaseType.Value))
                {
                    return pageBaseType.Value;
                }
            }

            try
            {
                //Throws runtime exception if can't load correct Microsoft.AspNet.WebPages
                dynamic section = ConfigurationManager.GetSection(RazorWebPagesSectionName);
                if (section != null && section.PageBaseType != null)
                    return section.PageBaseType;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading config in GetPageBaseTypeName():\n" + ex);
            }

            return typeof(ViewPage).Namespace + "." + typeof(ViewPage).Name;
        }

        /// <summary>
        /// Compares if the current MSBuild configuration is in the list of configurations specified by the Host Project.
        /// Returns true if configuration matches or Host Project does not specify a configuration.
        /// </summary>
        /// <param name="currentConfiguration">
        /// The current MSBuild Configuration.
        /// </param>
        /// <param name="allowedConfigurations">
        /// The allowed Configurations.
        /// </param>
        public static bool ConfigurationMatches(string currentConfiguration, string allowedConfigurations)
        {
            if (currentConfiguration.IsNullOrEmpty() && !allowedConfigurations.IsNullOrEmpty())
                return false;

            return allowedConfigurations.IsNullOrEmpty()
                || allowedConfigurations.Replace(" ", String.Empty) // Configuration names do not contain spaces, so safe to remove all
                                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Any(config => currentConfiguration.Equals(config, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets or sets the directory of the Build Task's Host Project (defined as drive + path); includes the trailing backslash '\'.
        /// Required by the Build Task.  Should be passed in via BuildTask.targets file.
        /// </summary>
        [Required]
        public string ProjectDir { get; set; }

        /// <summary>
        /// Gets or sets the name of the current project configuration (for example, "Debug").
        /// Should be passed in via BuildTask.targets file.
        /// </summary>
        public string ProjectConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the absolute path name of the primary output file (eg: assembly) for the Build Task's Host Project.
        /// Required by the Build Task.  Should be passed in via BuildTask.targets file.
        /// </summary>
        [Required]
        public string ProjectTargetPath { get; set; }

        /// <summary>
        /// Gets or sets the Build Task's Host Project's namesapce.
        /// Required by the Build Task.  Should be passed in via BuildTask.targets file.
        /// </summary>
        [Required]
        public string ProjectRootNamespace { get; set; }

        /// <summary>
        /// Gets or sets the Web/App.config path.
        ///  Should be passed in via BuildTask.targets file, if necessary to explicitly specify
        /// </summary>
        public string AppConfigPath { get; set; }

        /// <summary>
        /// Gets or sets the configuration mode(s) the Build Task will generate view source code for.
        /// Can be a comma-separated list (eg: "sandbox, release")
        /// </summary>
        public string AllowedConfigurations { get; set; }

        /// <summary>
        /// Gets or sets the filepaths to the Razor Views to generate source code for.
        /// Required by the Build Task.
        /// </summary>
        [Required]
        public ITaskItem[] InputFiles { get; set; }

        /// <summary>
        /// Gets or sets the file paths to the Razor View source code to be built into the Host Project's assembly.
        /// file paths should be relative to ProjectDir of the Build Task's Host Project
        /// Required by the Build Task.
        /// </summary>
        [Required]
        [Output]
        public ITaskItem[] OutputFiles { get; set; }

        /// <summary>
        /// The Web/App.config section name for Razor page config.
        /// </summary>
        private const string RazorWebPagesSectionName = "system.web.webPages.razor/pages";

        /// <summary>
        /// The Web/App.config AppSettings key to fetch the build configuration(s) allowed for the build task.
        /// </summary>
        private const string ConfigurationAppKeyName = "servicestack:razor:buildtask:allowedconfigurations";
    }
}
