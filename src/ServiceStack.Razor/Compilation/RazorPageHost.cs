using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Razor;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.Razor.Text;
using ServiceStack.Common.Extensions;
using ServiceStack.DataAnnotations;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.MiniProfiler;
using ServiceStack.Text;

namespace ServiceStack.Razor.Compilation
{
    public class RazorPageHost : RazorEngineHost, IRazorHost
    {
        private static ILog log = LogManager.GetLogger(typeof(RazorPageHost));

        private static readonly IEnumerable<string> _defaultImports = new[] {
            "System",
            "System.Collections.Generic",
            "System.IO",
            "System.Linq",
            "System.Net",
            "System.Text",
            "ServiceStack.Text",
            "ServiceStack.Html",
        };

        private readonly IRazorCodeTransformer _codeTransformer;
        private readonly CodeDomProvider _codeDomProvider;
        private readonly IDictionary<string, string> _directives;
        private string _defaultClassName;

        public IVirtualPathProvider PathProvider { get; protected set; }
        public IVirtualFile File { get; protected set; }

        public RazorPageHost(IVirtualPathProvider pathProvider,
                              IVirtualFile file,
                              IRazorCodeTransformer codeTransformer,
                              CodeDomProvider codeDomProvider,
                              IDictionary<string, string> directives)
            : base(new CSharpRazorCodeLanguage())
        {
            this.PathProvider = pathProvider;
            this.File = file;

            if (codeTransformer == null)
            {
                throw new ArgumentNullException("codeTransformer");
            }
            if (this.PathProvider == null)
            {
                throw new ArgumentNullException("pathProvider");
            }
            if (this.File == null)
            {
                throw new ArgumentNullException("file");
            }
            if (codeDomProvider == null)
            {
                throw new ArgumentNullException("codeDomProvider");
            }
            _codeTransformer = codeTransformer;
            _codeDomProvider = codeDomProvider;
            _directives = directives;
            base.DefaultNamespace = "ASP";
            EnableLinePragmas = true;

            base.GeneratedClassContext = new GeneratedClassContext(
                executeMethodName: GeneratedClassContext.DefaultExecuteMethodName,
                writeMethodName: GeneratedClassContext.DefaultWriteMethodName,
                writeLiteralMethodName: GeneratedClassContext.DefaultWriteLiteralMethodName,
                writeToMethodName: "WriteTo",
                writeLiteralToMethodName: "WriteLiteralTo",
                templateTypeName: typeof(HelperResult).FullName,
                defineSectionMethodName: "DefineSection",
                beginContextMethodName: "BeginContext",
                endContextMethodName: "EndContext"
                )
                {
                    ResolveUrlMethodName = "Href",
                };

            base.DefaultBaseClass = typeof(ViewPage).FullName;
            foreach (var import in _defaultImports)
            {
                base.NamespaceImports.Add(import);
            }
        }

        public override string DefaultClassName
        {
            get
            {
                return _defaultClassName ?? GetClassName();
            }
            set
            {
                if (!String.Equals(value, "__CompiledTemplate", StringComparison.OrdinalIgnoreCase))
                {
                    //  By default RazorEngineHost assigns the name __CompiledTemplate. We'll ignore this assignment
                    _defaultClassName = value;
                }
            }
        }

        public ParserBase Parser { get; set; }

        public RazorCodeGenerator CodeGenerator { get; set; }

        public bool EnableLinePragmas { get; set; }

        public GeneratorResults Generate()
        {
            lock (this)
            {
                _codeTransformer.Initialize(this, _directives);

                // Create the engine
                var engine = new RazorTemplateEngine(this);

                // Generate code 
                GeneratorResults results = null;
                try
                {
                    using (var stream = File.OpenRead())
                    using (var reader = new StreamReader(stream, Encoding.Default, detectEncodingFromByteOrderMarks: true))
                    {
                        results = engine.GenerateCode(reader, className: DefaultClassName, rootNamespace: DefaultNamespace, sourceFileName: this.File.RealPath);
                    }
                }
                catch (Exception e)
                {
                    throw new HttpParseException(e.Message, e, this.File.VirtualPath, null, 1);
                }

                //Throw the first parser message to generate the YSOD
                //TODO: Is there a way to output all errors at once?
                if  (results.ParserErrors.Count > 0)
                {
                    var error = results.ParserErrors[0];
                    throw new HttpParseException(error.Message, null, this.File.VirtualPath, null, error.Location.LineIndex + 1);
                }

                return results;
            }
        }

        public Dictionary<string, string> DebugSourceFiles = new Dictionary<string, string>();

        public virtual Type Compile()
        {
            byte[] bytes = null;
            return CompileAssembly(false, ref bytes);
        }

        protected Type CompileAssembly( bool loadBytes, ref byte[] assemblyBytes)
        {
            Type forceLoadOfRuntimeBinder = typeof(Microsoft.CSharp.RuntimeBinder.Binder);
            if (forceLoadOfRuntimeBinder == null)
            {
                log.Warn("Force load of .NET 4.0+ RuntimeBinder in Microsoft.CSharp.dll");
            }

            var razorResults = Generate();

            var @params = new CompilerParameters
                {
                    GenerateInMemory = !loadBytes,
                    GenerateExecutable = false,
                    IncludeDebugInformation = false,
                    CompilerOptions = "/target:library /optimize",
                    TempFiles = { KeepFiles = true }
                };

            var assemblies = CompilerServices
                .GetLoadedAssemblies()
                .Where(a => !a.IsDynamic)
                .Select(a => a.Location)
                .ToArray();

            @params.ReferencedAssemblies.AddRange(assemblies);

            //Compile the code
            var results = _codeDomProvider.CompileAssemblyFromDom(@params, razorResults.GeneratedCode);

            var tempFilesMarkedForDeletion = new TempFileCollection(null); 
            @params.TempFiles
                   .OfType<string>()
                   .ForEach(file => tempFilesMarkedForDeletion.AddFile(file, false));

            using (tempFilesMarkedForDeletion)
            {
                if (results.Errors != null && results.Errors.HasErrors)
                {
                    //check if source file exists, read it.
                    //HttpCompileException is sealed by MS. So, we'll
                    //just add a property instead of inheriting from it.
                    var sourceFile = results.Errors
                                            .OfType<CompilerError>()
                                            .First(ce => !ce.IsWarning)
                                            .FileName;

                    var sourceCode = "";
                    if (!string.IsNullOrEmpty(sourceFile) && System.IO.File.Exists(sourceFile))
                    {
                        sourceCode = System.IO.File.ReadAllText(sourceFile);
                    }
                    else
                    {
                        foreach (string tempFile in @params.TempFiles)
                        {
                            if (tempFile.EndsWith(".cs"))
                            {
                                sourceCode = System.IO.File.ReadAllText(tempFile);
                            }
                        }
                    }
                    throw new HttpCompileException(results, sourceCode);
                }

                if (loadBytes)
                    assemblyBytes = System.IO.File.ReadAllBytes(results.PathToAssembly);

                // Ensures the assembly is loaded into memory now, as it will be deleted from the temp directory
                // when not compiling into memory once this using statement exits.
                var compiledTypes = results.CompiledAssembly.GetTypes();

#if DEBUG
                foreach (string tempFile in @params.TempFiles)
                {
                    if (tempFile.EndsWith(".cs"))
                    {
                        var sourceCode = System.IO.File.ReadAllText(tempFile);
                        //sourceCode.Print();
                    }
                }
#endif

                return compiledTypes.First();
            }
        }

        public string GenerateSourceCode()
        {
            var razorResults = Generate();

            using (var writer = new StringWriter())
            {
                var options = new CodeGeneratorOptions
                    {
                        BlankLinesBetweenMembers = false,
                        BracingStyle = "C"
                    };

                //Generate the code
                writer.WriteLine("#pragma warning disable 1591");
                _codeDomProvider.GenerateCodeFromCompileUnit(razorResults.GeneratedCode, writer, options);
                writer.WriteLine("#pragma warning restore 1591");

                writer.Flush();

                // Perform output transformations and return
                string codeContent = writer.ToString();
                codeContent = _codeTransformer.ProcessOutput(codeContent);
                return codeContent;
            }
        }

        public override void PostProcessGeneratedCode(CodeGeneratorContext context)
        {
            _codeTransformer.ProcessGeneratedCode(context.CompileUnit, context.Namespace, context.GeneratedClass, context.TargetMethod);
        }

        public override RazorCodeGenerator DecorateCodeGenerator(RazorCodeGenerator incomingCodeGenerator)
        {
            var codeGenerator = CodeGenerator ?? base.DecorateCodeGenerator(incomingCodeGenerator);
            codeGenerator.GenerateLinePragmas = EnableLinePragmas;
            return codeGenerator;
        }

        protected virtual string GetClassName()
        {
            string filename = Path.GetFileNameWithoutExtension(this.File.VirtualPath);
            return "__" + ParserHelpers.SanitizeClassName(filename);
        }

        public override ParserBase DecorateCodeParser(ParserBase incomingCodeParser)
        {
            if (incomingCodeParser is System.Web.Razor.Parser.CSharpCodeParser)
                return new ServiceStackCSharpCodeParser();

            return base.DecorateCodeParser(incomingCodeParser);
        }
    }

    public class ServiceStackCSharpRazorCodeGenerator : CSharpRazorCodeGenerator
    {
        private const string DefaultModelTypeName = "dynamic";
        private const string HiddenLinePragma = "#line hidden";

        public ServiceStackCSharpRazorCodeGenerator(string className, string rootNamespaceName, string sourceFileName, RazorEngineHost host)
            : base(className, rootNamespaceName, sourceFileName, host)
        {
        }

        protected override void Initialize(CodeGeneratorContext context)
        {
            base.Initialize(context);

            context.GeneratedClass.Members.Insert(0, new CodeSnippetTypeMember(HiddenLinePragma));
        }
    }

    public class ServiceStackCSharpCodeParser : System.Web.Razor.Parser.CSharpCodeParser
    {
        private const string ModelKeyword = "model";
        private const string GenericTypeFormatString = "{0}<{1}>";
        private SourceLocation? _endInheritsLocation;
        private bool _modelStatementFound;

        public ServiceStackCSharpCodeParser()
        {
            MapDirectives(ModelDirective, ModelKeyword);
        }

        protected override void InheritsDirective()
        {
            // Verify we're on the right keyword and accept
            AssertDirective(SyntaxConstants.CSharp.InheritsKeyword);
            AcceptAndMoveNext();
            _endInheritsLocation = CurrentLocation;

            InheritsDirectiveCore();
            CheckForInheritsAndModelStatements();
        }

        private void CheckForInheritsAndModelStatements()
        {
            if (_modelStatementFound && _endInheritsLocation.HasValue)
            {
                Context.OnError(_endInheritsLocation.Value, String.Format(CultureInfo.CurrentCulture, MvcResources.MvcRazorCodeParser_CannotHaveModelAndInheritsKeyword, ModelKeyword));
            }
        }

        protected virtual void ModelDirective()
        {
            // Verify we're on the right keyword and accept
            AssertDirective(ModelKeyword);
            AcceptAndMoveNext();

            SourceLocation endModelLocation = CurrentLocation;

            BaseTypeDirective(string.Format(CultureInfo.CurrentCulture,
                              MvcResources.MvcRazorCodeParser_ModelKeywordMustBeFollowedByTypeName, ModelKeyword),
                CreateModelCodeGenerator);

            if (_modelStatementFound)
            {
                Context.OnError(endModelLocation, String.Format(CultureInfo.CurrentCulture,
                    MvcResources.MvcRazorCodeParser_OnlyOneModelStatementIsAllowed, ModelKeyword));
            }

            _modelStatementFound = true;

            CheckForInheritsAndModelStatements();
        }

        private SpanCodeGenerator CreateModelCodeGenerator(string model)
        {
            return new SetModelTypeCodeGenerator(model, GenericTypeFormatString);
        }

        protected override void LayoutDirective()
        {
            AssertDirective(SyntaxConstants.CSharp.LayoutKeyword);
            AcceptAndMoveNext();
            BaseTypeDirective(MvcResources.MvcRazorCodeParser_OnlyOneModelStatementIsAllowed.Fmt("layout"), CreateLayoutCodeGenerator);
        }

        private SpanCodeGenerator CreateLayoutCodeGenerator(string layoutPath)
        {
            return new SetLayoutCodeGenerator(layoutPath);
        }

        public class SetLayoutCodeGenerator : SpanCodeGenerator
        {
            public SetLayoutCodeGenerator(string layoutPath)
            {
                LayoutPath = layoutPath != null ? layoutPath.Trim(' ', '"') : null;
            }

            public string LayoutPath { get; set; }

            public override void GenerateCode(Span target, CodeGeneratorContext context)
            {
                if (!context.Host.DesignTimeMode && !String.IsNullOrEmpty(context.Host.GeneratedClassContext.LayoutPropertyName))
                {
                    context.GeneratedClass.CustomAttributes.Add(
                        new CodeAttributeDeclaration(typeof(MetaAttribute).FullName,
                                new CodeAttributeArgument(new CodePrimitiveExpression("Layout")),
                                new CodeAttributeArgument(new CodePrimitiveExpression(LayoutPath))
                    ));

                    context.TargetMethod.Statements.Add(
                        new CodeAssignStatement(
                            new CodePropertyReferenceExpression(null, context.Host.GeneratedClassContext.LayoutPropertyName),
                            new CodePrimitiveExpression(LayoutPath)));
                }
            }

            public override string ToString()
            {
                return "Layout: " + LayoutPath;
            }

            public override bool Equals(object obj)
            {
                var other = obj as SetLayoutCodeGenerator;
                return other != null && String.Equals(other.LayoutPath, LayoutPath, StringComparison.Ordinal);
            }

            public override int GetHashCode()
            {
                return LayoutPath.GetHashCode();
            }
        }

        internal class SetModelTypeCodeGenerator : SetBaseTypeCodeGenerator
        {
            private readonly string _genericTypeFormat;

            public SetModelTypeCodeGenerator(string modelType, string genericTypeFormat)
                : base(modelType)
            {
                _genericTypeFormat = genericTypeFormat;
            }

            protected override string ResolveType(CodeGeneratorContext context, string baseType)
            {
                var typeString = string.Format(
                    CultureInfo.InvariantCulture, _genericTypeFormat, context.Host.DefaultBaseClass, baseType);
                return typeString;
            }

            public override bool Equals(object obj)
            {
                var other = obj as SetModelTypeCodeGenerator;
                return other != null &&
                       base.Equals(obj) &&
                       String.Equals(_genericTypeFormat, other._genericTypeFormat, StringComparison.Ordinal);
            }

            public override int GetHashCode()
            {
                return (base.GetHashCode() + _genericTypeFormat).GetHashCode();
            }

            public override string ToString()
            {
                return "Model:" + BaseType;
            }
        }
    }

}
