using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Web.Compilation;
using System.Web.Razor;

namespace ServiceStack.Razor
{
    [BuildProviderAppliesTo(BuildProviderAppliesTo.Code | BuildProviderAppliesTo.Web)]
    public class CSharpRazorBuildProvider : BuildProvider
    {
        private readonly RazorEngineHost host;

        private readonly CompilerType compilerType;

        private CodeCompileUnit generatedCode;

        public CSharpRazorBuildProvider()
        {
            this.compilerType = this.GetDefaultCompilerTypeForLanguage("C#");

            this.host = new RazorEngineHost(new CSharpRazorCodeLanguage());
        }

        public override CompilerType CodeCompilerType
        {
            get { return this.compilerType; }
        }

        public override void GenerateCode(AssemblyBuilder assemblyBuilder)
        {
            assemblyBuilder.AddCodeCompileUnit(this, this.GetGeneratedCode());
            assemblyBuilder.GenerateTypeFactory(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", this.host.DefaultNamespace, this.host.DefaultClassName));
        }

        public override Type GetGeneratedType(CompilerResults results)
        {
            return results.CompiledAssembly.GetType(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", this.host.DefaultNamespace, this.host.DefaultClassName));
        }

        private CodeCompileUnit GetGeneratedCode()
        {
            if (this.generatedCode == null)
            {
                var engine = new RazorTemplateEngine(this.host);
                GeneratorResults results;
                using (var reader = this.OpenReader())
                {
                    results = engine.GenerateCode(reader);
                }

                if (!results.Success)
                {
                    throw new InvalidOperationException(results.ToString());
                }

                this.generatedCode = results.GeneratedCode;
            }

            return this.generatedCode;
        }
    }
}