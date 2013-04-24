using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Razor;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser;
using System.Web.Razor.Parser.SyntaxTree;
using ServiceStack.Common.Extensions;
using ServiceStack.IO;
using ServiceStack.MiniProfiler;

namespace ServiceStack.Razor2.Compilation
{
    public class RazorPageHost : RazorEngineHost, IRazorHost
    {
        private static readonly IEnumerable<string> _defaultImports = new[] {
            "System",
            "System.Collections.Generic",
            "System.IO",
            "System.Linq",
            "System.Net",
            "System.Text"
        };

        private readonly IRazorCodeTransformer _codeTransformer;
        private readonly CodeDomProvider _codeDomProvider;
        private readonly IDictionary<string, string> _directives;
        private string _defaultClassName;

        public IVirtualPathProvider PathProvider { get; protected set; }
        public IVirtualFile File { get; protected set; }

        public RazorPageHost( IVirtualPathProvider pathProvider,
                              IVirtualFile file,
                              IRazorCodeTransformer codeTransformer,
                              CodeDomProvider codeDomProvider,
                              IDictionary<string, string> directives )
            : base( RazorCodeLanguage.GetLanguageByExtension( ".cshtml" ) )
        {
            this.PathProvider = pathProvider;
            this.File = file;

            if ( codeTransformer == null )
            {
                throw new ArgumentNullException( "codeTransformer" );
            }
            if ( this.PathProvider == null )
            {
                throw new ArgumentNullException( "pathProvider" );
            }
            if ( this.File == null )
            {
                throw new ArgumentNullException( "file" );
            }
            if ( codeDomProvider == null )
            {
                throw new ArgumentNullException( "codeDomProvider" );
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
                    ResolveUrlMethodName = "Href"
                };

            base.DefaultBaseClass = typeof(ViewPage).FullName;
            foreach ( var import in _defaultImports )
            {
                base.NamespaceImports.Add( import );
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
            _codeTransformer.Initialize( this, _directives );

            // Create the engine
            var engine = new RazorTemplateEngine( this );

            // Generate code 
            GeneratorResults results = null;
            try
            {
                using( var stream = File.OpenRead() )
                using( var reader = new StreamReader( stream, Encoding.Default, detectEncodingFromByteOrderMarks: true ) )
                {
                    results = engine.GenerateCode( reader, className: DefaultClassName, rootNamespace: DefaultNamespace, sourceFileName: this.File.RealPath );
                }
            }
            catch( Exception e )
            {
                OnGenerateError( 4, e.Message, 1, 1 );
                //Returning null signifies that generation has failed
                return null;
            }

            // Output errors
            foreach( RazorError error in results.ParserErrors )
            {
                OnGenerateError( 4, error.Message, (uint)error.Location.LineIndex + 1, (uint)error.Location.CharacterIndex + 1 );
            }

            return results;
        }

        public Type Compile()
        {
            var razorResults = Generate();

            var @params = new CompilerParameters
                {
                    GenerateInMemory = true,
                    GenerateExecutable = false,
                    IncludeDebugInformation = false,
                    CompilerOptions = "/target:library /optimize",
                    TempFiles = { KeepFiles = true }
                };

            var assemblies = CompilerServices
                .GetLoadedAssemblies()
                .Where( a => !a.IsDynamic )
                .Select( a => a.Location )
                .ToArray();

            @params.ReferencedAssemblies.AddRange( assemblies );

            //Compile the code
            var results = _codeDomProvider.CompileAssemblyFromDom( @params, razorResults.GeneratedCode );

            OnCodeCompletion();

            var tempFilesMarkedForDeletion = new TempFileCollection( null );
            @params.TempFiles
                   .OfType<string>()
                   .ForEach( file => tempFilesMarkedForDeletion.AddFile( file, false ) );

            using( tempFilesMarkedForDeletion )
            {
                if ( results.Errors != null && results.Errors.HasErrors )
                {
                    //check if source file exists, read it.
                    //HttpCompileException is sealed by MS. So, we'll
                    //just add a property instead of inheriting from it.
                    var sourceFile = results.Errors
                                            .OfType<CompilerError>()
                                            .First( ce => !ce.IsWarning )
                                            .FileName;

                    var sourceCode = "";
                    if ( System.IO.File.Exists( sourceFile ) )
                    {
                        sourceCode = System.IO.File.ReadAllText( sourceFile );
                    }
                    throw new HttpCompileException( results, sourceCode );
                }

                return results.CompiledAssembly.GetTypes().First();
            }
        }

        public string GenerateSourceCode()
        {
            var razorResults = Generate();

            using ( var writer = new StringWriter() )
            {
                var options = new CodeGeneratorOptions
                    {
                        BlankLinesBetweenMembers = false,
                        BracingStyle = "C"
                    };

                //Generate the code
                writer.WriteLine( "#pragma warning disable 1591" );
                _codeDomProvider.GenerateCodeFromCompileUnit( razorResults.GeneratedCode, writer, options );
                writer.WriteLine( "#pragma warning restore 1591" );

                OnCodeCompletion();
                writer.Flush();

                // Perform output transformations and return
                string codeContent = writer.ToString();
                codeContent = _codeTransformer.ProcessOutput( codeContent );
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

        public override ParserBase DecorateCodeParser(ParserBase incomingCodeParser)
        {
            return Parser ?? base.DecorateCodeParser(incomingCodeParser);
        }

        private void OnGenerateError(uint errorCode, string errorMessage, uint lineNumber, uint columnNumber)
        {
            throw new HttpCompileException(errorMessage);
        }

        private void OnCodeCompletion()
        {

        }

        protected virtual string GetClassName()
        {
            string filename = Path.GetFileNameWithoutExtension( this.File.VirtualPath );
            return "__"+ParserHelpers.SanitizeClassName( filename );
        }
    }
}
