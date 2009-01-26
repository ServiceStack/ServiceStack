using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.Translators.Generator.Support;

namespace ServiceStack.Translators.Generator
{
	class Program
	{
		static readonly ILog log = LogManager.GetLogger(typeof(Program));

		[CommandLineSwitch("f", "Overwrite existing files")]
		public bool ForceOverwrite { get; set; }

		[CommandLineSwitch("assembly", "Absolute Assembly Path")]
		public string ModelAssemblyPath { get; set; }

		[CommandLineSwitch("out", "Generated output dir")]
		public string OutputDir { get; set; }

		static int Main(string[] args)
		{
			var program = new Program();
			program.Run();
			return 0;
		}

		public void Run()
		{
			try
			{
				Console.WriteLine("Generating translators...");
				LogManager.LogFactory = new ConsoleLogFactory();

				var parser = new Parser(System.Environment.CommandLine, this);
				parser.Parse();

				var modelAssembly = Assembly.LoadFile(ModelAssemblyPath);

				var modelAssemblyTypes = modelAssembly.GetTypes();
				log.DebugFormat("Found {0} types in Assembly", modelAssemblyTypes.Count());
				foreach (var type in modelAssemblyTypes)
				{
					var attrs = type.GetCustomAttributes(typeof(TranslateModelAttribute), false).ToList();
					var extensionAttrs = type.GetCustomAttributes(typeof(TranslateModelExtentionAttribute), false).ToList();

					if (attrs.Count == 0 && extensionAttrs.Count == 0) continue;

					var outPath = Path.Combine(this.OutputDir, type.Name + ".generated.cs");

					if (!DoGenerateTranslator(outPath)) continue;

					if (attrs.Count > 0)
					{
						var attr = (TranslateModelAttribute)attrs[0];
						var generator = new TranslatorClassGenerator(CodeLang.CSharp);
						generator.Write(type, outPath, attr);
					}
					if (extensionAttrs.Count > 0)
					{
						var generator = new ExtensionTranslatorClassGenerator(CodeLang.CSharp);
						generator.Write(type, this.OutputDir);
					}
				}

			}
			catch (Exception ex)
			{
				Console.Error.Write(ex.GetType() + ": " + ex.Message + "\n" + ex.StackTrace);
				throw;
			}
		}

		private bool DoGenerateTranslator(string outPath)
		{
			if (File.Exists(outPath))
			{
				if (!this.ForceOverwrite)
				{
					log.DebugFormat("Skipping existing file '{0}'", outPath);
					return false;
				}
				else
				{
					log.InfoFormat("Overwriting existing file '{0}'", outPath);
				}
			}

			log.InfoFormat("Creating file '{0}'...", outPath);
			return true;
		}
	}
}
