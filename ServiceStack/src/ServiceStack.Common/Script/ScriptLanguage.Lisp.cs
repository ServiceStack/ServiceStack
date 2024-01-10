/*
  Copyright (c) 2017 OKI Software Co., Ltd.
  Copyright (c) 2018 SUZUKI Hisao

  Permission is hereby granted, free of charge, to any person obtaining a
  copy of this software and associated documentation files (the "Software"),
  to deal in the Software without restriction, including without limitation
  the rights to use, copy, modify, merge, publish, distribute, sublicense,
  and/or sell copies of the Software, and to permit persons to whom the
  Software is furnished to do so, subject to the following conditions:

  The above copyright notice and this permission notice shall be included in
  all copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
  THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
  DEALINGS IN THE SOFTWARE.
*/

// H29.3/1 - H30.6/27 by SUZUKI Hisao
// lisp.exe: csc /doc:lisp.xml /o lisp.cs
// doc: mdoc update -i lisp.xml -o xml lisp.exe; mdoc export-html -o html xml

// [assembly: AssemblyProduct("Nukata Lisp Light")]
// [assembly: AssemblyVersion("1.2.2.*")]
// [assembly: AssemblyTitle("A Lisp interpreter in C# 7")]
// [assembly: AssemblyCopyright("© 2017 Oki Software Co., Ltd.; " + 
//                              "© 2018 SUZUKI Hisao [MIT License]")]

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.Text.Json;

namespace ServiceStack.Script;

public sealed class ScriptLisp : ScriptLanguage, IConfigureScriptContext
{
    private ScriptLisp() {} // force usage of singleton

    public static readonly ScriptLanguage Language = new ScriptLisp();
        
    public override string Name => "lisp";
        
    public override string LineComment => ";";

    public void Configure(ScriptContext context)
    {
        Lisp.Init();
        context.ScriptMethods.Add(new LispScriptMethods());
        context.ScriptBlocks.Add(new DefnScriptBlock());
    }

    public override List<PageFragment> Parse(ScriptContext context, ReadOnlyMemory<char> body, ReadOnlyMemory<char> modifiers)
    {
        var quiet = false;
            
        if (!modifiers.IsEmpty)
        {
            quiet = modifiers.EqualsOrdinal("q") || modifiers.EqualsOrdinal("quiet") || modifiers.EqualsOrdinal("mute");
            if (!quiet)
                throw new NotSupportedException($"Unknown modifier '{modifiers.ToString()}', expected 'code|q', 'code|quiet' or 'code|mute'");
        }

        return new List<PageFragment> { 
            new PageLispStatementFragment(context.ParseLisp(body)) {
                Quiet = quiet
            } 
        };
    }

    public override async Task<bool> WritePageFragmentAsync(ScriptScopeContext scope, PageFragment fragment, CancellationToken token)
    {
        if (fragment is PageLispStatementFragment blockFragment)
        {
            if (blockFragment.Quiet && scope.OutputStream != Stream.Null)
                scope = scope.ScopeWithStream(Stream.Null);
                
            await WriteStatementAsync(scope, blockFragment.LispStatements, token);
                
            return true;
        }
        return false;
    }
        
    public override async Task<bool> WriteStatementAsync(ScriptScopeContext scope, JsStatement statement, CancellationToken token)
    {
        var page = scope.PageResult;
        if (statement is LispStatements lispStatement)
        {
            var lispCtx = scope.PageResult.GetLispInterpreter(scope);
            page.ResetIterations();

            var len = lispStatement.SExpressions.Length;
                
            foreach (var sExpr in lispStatement.SExpressions)
            {
                var value = lispCtx.Eval(sExpr, null);
                if (value != null && value != JsNull.Value && value != StopExecution.Value && value != IgnoreResult.Value)
                {
                    if (value is Lisp.Sym s)
                        continue;
                        
                    var strValue = page.Format.EncodeValue(value);
                    if (!string.IsNullOrEmpty(strValue))
                    {
                        var bytes = strValue.ToUtf8Bytes();
                        await scope.OutputStream.WriteAsync(bytes, token);
                    }
                        
                    if (len > 1) // don't emit new lines for single expressions
                        await scope.OutputStream.WriteAsync(JsTokenUtils.NewLineUtf8, token);
                }
            }
        }
        else return false;
            
        return true;
    }
}
    
public class LispStatements : JsStatement
{
    public object[] SExpressions { get; }
    public LispStatements(object[] sExpressions) => SExpressions = sExpressions;

    protected bool Equals(LispStatements other) => Equals(SExpressions, other.SExpressions);
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((LispStatements) obj);
    }
    public override int GetHashCode() => (SExpressions != null ? SExpressions.GetHashCode() : 0);
}
    
public class PageLispStatementFragment : PageFragment
{
    public LispStatements LispStatements { get; }
        
    public bool Quiet { get; set; }
        
    public PageLispStatementFragment(LispStatements statements) => LispStatements = statements;

    protected bool Equals(PageLispStatementFragment other) => Equals(LispStatements, other.LispStatements);
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((PageJsBlockStatementFragment) obj);
    }
    public override int GetHashCode() => (LispStatements != null ? LispStatements.GetHashCode() : 0);
}

public class LispScriptMethods : ScriptMethods
{
    public List<string> symbols(ScriptScopeContext scope)
    {
        var interp = scope.GetLispInterpreter();
        return interp.Globals.Keys.Map(x => x.Name).OrderBy(x => x).ToList();
    }

    public List<GistLink> gistindex(ScriptScopeContext scope)
    {
        return Lisp.Interpreter.GetGistIndexLinks(scope);
    }
}
    
/// <summary>
/// Define and export a LISP function to the page
/// Usage: {{#defn calc [a, b] }}
///           (let ( (c (* a b)) )
///             (+ a b c)
///           )
///        {{/defn}}
/// </summary>
public class DefnScriptBlock : ScriptBlock
{
    public override string Name => "defn";
    public override ScriptLanguage Body => ScriptLanguage.Verbatim;
        
    public override Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken token)
    {
        // block.Argument key is unique to exact memory fragment, not string equality
        // Parse sExpression once for all Page Results
        var sExpr = (List<object>) scope.Context.CacheMemory.GetOrAdd(block.Argument, key => {

            var literal = block.Argument.Span.ParseVarName(out var name);
            var strName = name.ToString();

            var strFragment = (PageStringFragment) block.Body[0];
            var lispDefnAndExport =
                $@"(defn {block.Argument} {strFragment.Value}) (export {strName} (to-delegate {strName}))";
            return Lisp.Parse(lispDefnAndExport);
        });
            
        var interp = scope.PageResult.GetLispInterpreter(scope);
        interp.Eval(sExpr);
            
        return TypeConstants.EmptyTask;
    }
}

/// <summary>Exception in evaluation</summary>
public class LispEvalException: Exception 
{
    /// <summary>Stack trace of Lisp evaluation</summary>
    public List<string> Trace { get; } = new List<string>();

    /// <summary>Construct with a base message, cause, and
    /// a flag whether to quote strings in the cause.</summary>
    public LispEvalException(string msg, object x, bool quoteString=true)
        : base(msg + ": " + Lisp.Str(x, quoteString)) {}

    /// <summary>Return a string representation which contains
    /// the message and the stack trace.</summary>
    public override string ToString()
    {
        var sb = StringBuilderCache.Allocate().Append($"EvalException: {Message}");
        foreach (var line in Trace)
            sb.Append($"\n\t{line}");
        return StringBuilderCache.ReturnAndFree(sb);
    }
}

public static class ScriptLispUtils
{
    public static Lisp.Interpreter GetLispInterpreter(this ScriptScopeContext scope) =>
        scope.PageResult.GetLispInterpreter(scope);
    public static Lisp.Interpreter GetLispInterpreter(this PageResult pageResult, ScriptScopeContext scope)
    {
        if (!pageResult.Args.TryGetValue(nameof(ScriptLisp), out var oInterp))
        {
            var interp = Lisp.CreateInterpreter();
            pageResult.Args[nameof(ScriptLisp)] = interp;
            interp.Scope = scope;
            return interp;
        }
        else
        {
            var interp = (Lisp.Interpreter) oInterp;
            interp.Scope = scope;
            return interp;
        }
    }
        
    public static SharpPage LispSharpPage(this ScriptContext context, string lisp) 
        => context.Pages.OneTimePage(lisp, context.PageFormats[0].Extension,p => p.ScriptLanguage = ScriptLisp.Language);

    private static void AssertLisp(this ScriptContext context)
    {
        if (!context.ScriptLanguages.Contains(ScriptLisp.Language))
            throw new NotSupportedException($"ScriptLisp.Language is not registered in {context.GetType().Name}.{nameof(context.ScriptLanguages)}");
    }

    private static PageResult GetLispPageResult(ScriptContext context, string lisp, Dictionary<string, object> args)
    {
        context.AssertLisp();
        PageResult pageResult = null;
        try
        {
            var page = context.LispSharpPage(lisp);
            pageResult = new PageResult(page);
            args.Each((x, y) => pageResult.Args[x] = y);
            return pageResult;
        }
        catch (Exception e)
        {
            if (ScriptContextUtils.ShouldRethrow(e))
                throw;
            throw ScriptContextUtils.HandleException(e, pageResult ?? new PageResult(context.EmptyPage));
        }
    }

    public static string RenderLisp(this ScriptContext context, string lisp, Dictionary<string, object> args=null)
    {
        var pageResult = GetLispPageResult(context, lisp, args);
        return pageResult.RenderScript();
    }

    public static async Task<string> RenderLispAsync(this ScriptContext context, string lisp, Dictionary<string, object> args=null)
    {
        var pageResult = GetLispPageResult(context, lisp, args);
        return await pageResult.RenderScriptAsync();
    }

    public static LispStatements ParseLisp(this ScriptContext context, string lisp) =>
        context.ParseLisp(lisp.AsMemory());

    public static LispStatements ParseLisp(this ScriptContext context, ReadOnlyMemory<char> lisp)
    {
        var sExpressions = Lisp.Parse(lisp);
        return new LispStatements(sExpressions.ToArray());
    }

    public static T EvaluateLisp<T>(this ScriptContext context, string lisp, Dictionary<string, object> args = null) =>
        context.EvaluateLisp(lisp, args).ConvertTo<T>();
        
    public static object EvaluateLisp(this ScriptContext context, string lisp, Dictionary<string, object> args=null)
    {
        var pageResult = GetLispPageResult(context, lisp, args);

        if (!pageResult.EvaluateResult(out var returnValue))
            throw new NotSupportedException(ScriptContextUtils.ErrorNoReturn);
            
        return ScriptLanguage.UnwrapValue(returnValue);
    }

    public static async Task<T> EvaluateLispAsync<T>(this ScriptContext context, string lisp, Dictionary<string, object> args = null) =>
        (await context.EvaluateLispAsync(lisp, args)).ConvertTo<T>();
        
    public static async Task<object> EvaluateLispAsync(this ScriptContext context, string lisp, Dictionary<string, object> args=null)
    {
        var pageResult = GetLispPageResult(context, lisp, args);

        var ret = await pageResult.EvaluateResultAsync();
        if (!ret.Item1)
            throw new NotSupportedException(ScriptContextUtils.ErrorNoReturn);
            
        return ScriptLanguage.UnwrapValue(ret.Item2);
    }

    public static string EnsureReturn(string lisp)
    {
        // if code doesn't contain a return, wrap and return the expression
        if ((lisp ?? throw new ArgumentNullException(nameof(lisp))).IndexOf("(return",StringComparison.Ordinal) == -1)
            lisp = "(return (let () " + lisp + " ))";

        return lisp;
    }
}

internal static class Utils
{
    internal static object lispBool(this bool t) => t ? Lisp.TRUE : null;
    internal static object fromLisp(this object o) => o == Lisp.TRUE ? true : o;
    internal static object lastArg(this object[] a)
    {
        var last = a[a.Length - 1];
        return last is Lisp.Cell lastCell ? lastCell.Car : last;
    }
    internal static IEnumerable assertEnumerable(this object a)
    {
        if (a == null)
            return TypeConstants.EmptyObjectArray;
        if (a is IEnumerable e)
            return e;
        throw new LispEvalException("not IEnumerable", a);
    }

    internal static int compareTo(this object a, object b)
    {
        return a == null || b == null
            ? (a == b ? 0 : a == null ? -1 : 1)
            : DynamicNumber.IsNumber(a.GetType())
                ? DynamicNumber.CompareTo(a, b)
                : a is IComparable c
                    ? (int) c.CompareTo(b)
                    : throw new LispEvalException("not IComparable", a);
    }

    public static Lisp.Cell unwrapDataListArgs(this Lisp.Cell arg)
    {
        if (arg.Car is Lisp.Cell c && c.Car == Lisp.LIST) // allow clojure data list [] for fn args list by unwrapping (list ...) => ...
            arg.Car = c.Cdr;
        return arg;
    }

    internal static object unwrapScriptValue(this object o)
    {
        if (o is Task t)
            o = t.GetResult();
        if (o is bool b)
            return b ? Lisp.TRUE : null;
        return ScriptLanguage.UnwrapValue(o);
    }
}

/// <summary>
///  A Lisp interpreter written in C# 7
/// </summary><remarks>
///  This is ported from Nuka Lisp in Dart
///  (https://github.com/nukata/lisp-in-dart) except for bignum.
///  It is named after ex-Nukata Town in Japan.
/// </remarks>
public static class Lisp
{
    /// <summary>
    /// Allow loading of remote scripts
    ///  - https://example.org/lib.l
    ///  - gist:{gist-id}
    ///  - gist:{gist-id}/file.l
    ///  - index:{name}
    ///  - index:{name}/file.l
    /// </summary>
    public static bool AllowLoadingRemoteScripts { get; set; } = true;

    /// <summary>
    /// Gist where to resolve `index:{name}` references from
    /// </summary>
    public static string IndexGistId { get; set; } = "3624b0373904cfb2fc7bb3c2cb9dc1a3";

    private static Interpreter GlobalInterpreter;

    static Lisp()
    {
        Reset();
    }

    /// <summary>
    /// Reset Global Symbols back to default
    /// </summary>
    public static void Reset()
    {
        //Create and cache pre-loaded global symbol table once.
        GlobalInterpreter = new Interpreter();
        Run(GlobalInterpreter, new Reader(InitScript.AsMemory()));
    }

    /// <summary>
    /// Load Lisp into Global Symbols, a new CreateInterpreter() starts with a copy of global symbols
    /// </summary>
    public static void Import(string lisp) => Import(lisp.AsMemory());

    /// <summary>
    /// Load Lisp into Global Symbols, a new CreateInterpreter() starts with a copy of global symbols
    /// </summary>
    public static void Import(ReadOnlyMemory<char> lisp)
    {
        Run(GlobalInterpreter, new Reader(lisp));
    }

    public static void Set(string symbolName, object value)
    {
        GlobalInterpreter.Globals[Sym.New(symbolName)] = value;
    }
        
    public static void Init() {} // Force running static initializers

    /// <summary>Create a Lisp interpreter initialized pre-configured with Global Symbols.</summary>
    public static Interpreter CreateInterpreter() {
        Init();
        var interp = new Interpreter(GlobalInterpreter);
        return interp;
    }

    /// <summary>Cons cell</summary>
    public sealed class Cell : IEnumerable  
    {
        /// <summary>Head part of the cons cell</summary>
        public object Car;
        /// <summary>Tail part of the cons cell</summary>
        public object Cdr;

        /// <summary>Construct a cons cell with its head and tail.</summary>
        public Cell(object car, object cdr) {
            Car = car;
            Cdr = cdr;
        }

        /// <summary>Make a simple string representation.</summary>
        /// <remarks>Do not invoke this for any circular list.</remarks>
        public override string ToString() =>
            $"({Car ?? "null"} . {Cdr ?? "null"})";

        /// <summary>Length as a list</summary>
        public int Length => FoldL(0, this, (i, e) => i + 1);
            
        public IEnumerator GetEnumerator()
        {
            var j = this;
            do { 
                yield return j.Car;
            } while ((j = CdrCell(j)) != null);
        }

        public void Walk(Action<Cell> fn)
        {
            fn(this);
            if (Car is Cell l)
                l.Walk(fn);
            if (Cdr is Cell r)
                r.Walk(fn);
        }
    }


    // MapCar((a b c), fn) => (fn(a) fn(b) fn(c))
    static Cell MapCar(Cell j, Func<object, object> fn) {
        if (j == null)
            return null;
        object a = fn(j.Car);
        object d = j.Cdr;
        if (d is Cell dc)
            d = MapCar(dc, fn);
        if (j.Car == a && j.Cdr == d)
            return j;
        return new Cell(a, d);
    }

    // FoldL(x, (a b c), fn) => fn(fn(fn(x, a), b), c)
    static T FoldL<T> (T x, Cell j, Func<T, object, T> fn) {
        while (j != null) {
            x = fn(x, j.Car);
            j = (Cell) j.Cdr;
        }
        return x;
    }
        
    // Supports both Cell + IEnumerable
    static T FoldL<T> (T x, IEnumerable j, Func<T, object, T> fn) {
        foreach (var e in j)
            x = fn(x, e);
        return x;
    }

    /// <summary>Lisp symbol</summary>
    public class Sym {
        /// <summary>The symbol's name</summary>
        public string Name { get; }
            
        /// <summary>Construct a symbol that is not interned.</summary>
        public Sym(string name) {
            Name = name;
        }

        /// <summary>Return the symbol's name</summary>
        public override string ToString() => Name;
        /// <summary>Return the hashcode of the symbol's name</summary>
        public override int GetHashCode() => Name.GetHashCode();

        /// <summary>Table of interned symbols</summary>
        protected static readonly Dictionary<string, Sym> Table =
            new Dictionary<string, Sym>();

        /// <summary>Return an interned symbol for the name.</summary>
        /// <remarks>If the name is not interned yet, such a symbol
        /// will be constructed with <paramref name="make"/>.</remarks>
        protected static Sym New(string name, Func<string, Sym> make) {
            lock (Table) {
                if (! Table.TryGetValue(name, out Sym result)) {
                    result = make(name);
                    Table[name] = result;
                }
                return result;
            }
        }

        /// <summary>Construct an interned symbol.</summary>
        public static Sym New(string name) => New(name, s => new Sym(s));

        /// <summary>Is it interned?</summary>
        public bool IsInterned {
            get {
                lock (Table) {
                    return Table.TryGetValue(Name, out Sym s) &&
                           Object.ReferenceEquals(this, s);
                }
            }
        }
    }


    // Expression keyword
    sealed class Keyword: Sym {
        Keyword(string name): base(name) {}
        internal static new Sym New(string name)
            => New(name, s => new Keyword(s));
    }

    /// <summary>The symbol of <c>t</c></summary>
    public static readonly Sym TRUE = Sym.New("t");
    public static readonly Sym BOOL_TRUE = Sym.New("true");
    public static readonly Sym BOOL_FALSE = Sym.New("false");
    static readonly Sym VERBOSE = Sym.New("verbose");

    static readonly Sym COND = Keyword.New("cond");
    static readonly Sym LAMBDA = Keyword.New("lambda");
    static readonly Sym FN = Keyword.New("fn");
    static readonly Sym MACRO = Keyword.New("macro");
    static readonly Sym PROGN = Keyword.New("progn");
    static readonly Sym QUASIQUOTE = Keyword.New("quasiquote");
    static readonly Sym QUOTE = Keyword.New("quote");
    static readonly Sym SETQ = Keyword.New("setq");
    static readonly Sym EXPORT = Keyword.New("export");
        
    static readonly Sym BOUND = Sym.New("bound?");

    static readonly Sym BACK_QUOTE = Sym.New("`");
    static readonly Sym COMMAND_AT = Sym.New(",@");
    static readonly Sym COMMA = Sym.New(",");
    static readonly Sym DOT = Sym.New(".");
    static readonly Sym LEFT_PAREN = Sym.New("(");
    static readonly Sym RIGHT_PAREN = Sym.New(")");
    static readonly Sym SINGLE_QUOTE = Sym.New("'");

    static readonly Sym APPEND = Sym.New("append");
    static readonly Sym CONS = Sym.New("cons");
    internal static readonly Sym LIST = Sym.New("list");
    static readonly Sym REST = Sym.New("&rest");
    static readonly Sym UNQUOTE = Sym.New("unquote");
    static readonly Sym UNQUOTE_SPLICING = Sym.New("unquote-splicing");

    static readonly Sym LEFT_BRACE = Sym.New("{");
    static readonly Sym RIGHT_BRACE = Sym.New("}");
    static readonly Sym HASH = Sym.New("#");
    static readonly Sym PERCENT = Sym.New("%");
    static readonly Sym NEWMAP = Sym.New("new-map");
    static readonly Sym ARG = Sym.New("_a");

    static readonly Sym LEFT_BRACKET = Sym.New("[");
    static readonly Sym RIGHT_BRACKET = Sym.New("]");

    //------------------------------------------------------------------

    // Get cdr of list x as a Cell or null.
    static Cell CdrCell(Cell x) {
        var k = x.Cdr;
        if (k == null) {
            return null;
        } else {
            if (k is Cell c)
                return c;
            else
                throw new LispEvalException("proper list expected", x);
        }
    }


    /// <summary>Common base class of Lisp functions</summary>
    public abstract class LispFunc {
        /// <summary>Number of arguments, made negative if the function
        /// has &amp;rest</summary>
        public int Carity { get; }

        int Arity => (Carity < 0) ? -Carity : Carity;
        bool HasRest => (Carity < 0);

        // Number of fixed arguments
        int FixedArgs => (Carity < 0) ? -Carity - 1 : Carity;

        /// <summary>Construct with Carity.</summary>
        protected LispFunc(int carity) {
            Carity = carity;
        }

        /// <summary>Make a frame for local variables from a list of
        /// actual arguments.</summary>
        public object[] MakeFrame(Cell arg) {
            var frame = new object[Arity];
            int n = FixedArgs;
            int i;
            for (i = 0; i < n && arg != null; i++) {
                // Set the list of fixed arguments.
                frame[i] = arg.Car;
                arg = CdrCell(arg);
            }
            if (i != n || (arg != null && !HasRest))
                throw new LispEvalException("arity not matched", this);
            if (HasRest)
                frame[n] = arg;
            return frame;
        }

        /// <summary>Evaluate each expression in a frame.</summary>
        public void EvalFrame(object[] frame, Interpreter interp, Cell env) {
            int n = FixedArgs;
            for (int i = 0; i < n; i++)
                frame[i] = interp.Eval(frame[i], env);
            if (HasRest) {
                if (frame[n] is Cell j) {
                    Cell z = null;
                    Cell y = null;
                    do {
                        var e = interp.Eval(j.Car, env);
                        Cell x = new Cell(e, null);
                        if (z == null)
                            z = x;
                        else
                            y.Cdr = x;
                        y = x;
                        j = CdrCell(j);
                    } while (j != null);
                    frame[n] = z;
                }
            }
        }
    }


    // Common base class of functions which are defined with Lisp expressions
    abstract class DefinedFunc: LispFunc {
        // Lisp list as the function body
        public readonly Cell Body;

        protected DefinedFunc(int carity, Cell body): base(carity) {
            Body = body;
        }
    }


    // Common function type which represents any factory method of DefinedFunc
    delegate DefinedFunc FuncFactory(int carity, Cell body, Cell env);


    // Compiled macro expression
    sealed class Macro: DefinedFunc {
        Macro(int carity, Cell body): base(carity, body) {}
        public override string ToString() => $"#<macro:{Carity}:{Str(Body)}>";

        // Expand the macro with a list of actual arguments.
        public object ExpandWith(Interpreter interp, Cell arg) {
            object[] frame = MakeFrame(arg);
            Cell env = new Cell(frame, null);
            object x = null;
            for (Cell j = Body; j != null; j = CdrCell(j))
                x = interp.Eval(j.Car, env);
            return x;
        }

        public static DefinedFunc Make(int carity, Cell body, Cell env) {
            Debug.Assert(env == null);
            return new Macro(carity, body);
        }
    }


    // Compiled lambda expression (Within another function)
    sealed class Lambda: DefinedFunc {
        Lambda(int carity, Cell body): base(carity, body) {}
        public override string ToString() => $"#<lambda:{Carity}:{Str(Body)}>";

        public static DefinedFunc Make(int carity, Cell body, Cell env) {
            Debug.Assert(env == null);
            return new Lambda(carity, body);
        }
    }


    // Compiled lambda expression (Closure with environment)
    sealed class Closure: DefinedFunc {
        // The environment of the closure
        public readonly Cell Env;

        Closure(int carity, Cell body, Cell env): base(carity, body) {
            Env = env;
        }

        public Closure(Lambda x, Cell env): this(x.Carity, x.Body, env) {}

        public override string ToString() =>
            $"#<closure:{Carity}:{Str(Env)}:{Str(Body)}>";

        // Make an environment to evaluate the body from a list of actual args.
        public Cell MakeEnv(Interpreter interp, Cell arg, Cell interpEnv) {
            object[] frame = MakeFrame(arg);
            EvalFrame(frame, interp, interpEnv);
            return new Cell(frame, Env); // Prepend the frame to this Env.
        }

        public static DefinedFunc Make(int carity, Cell body, Cell env) =>
            new Closure(carity, body, env);
    }


    /// <summary>Function type which represents any built-in function body
    /// </summary>
    public delegate object BuiltInFuncBody(Interpreter interp, object[] frame);

    /// <summary>Built-in function</summary>
    public sealed class BuiltInFunc: LispFunc {
        /// <summary>Name of this function</summary>
        public string Name { get; }
        /// <summary>C# function as the body of this function</summary>
        public BuiltInFuncBody Body { get; }

        /// <summary>Construct with Name, Carity and Body.</summary>
        public BuiltInFunc(string name, int carity, BuiltInFuncBody body)
            : base(carity) {
            Name = name;
            Body = body;
        }

        /// <summary>Return a string representation in Lisp.</summary>
        public override string ToString() => $"#<{Name}:{Carity}>";

        /// <summary>Invoke the built-in function with a list of
        /// actual arguments.</summary>
        public object EvalWith(Interpreter interp, Cell arg, Cell interpEnv) {
            object[] frame = MakeFrame(arg);
            EvalFrame(frame, interp, interpEnv);
            try {
                return Body(interp, frame);
            } catch (LispEvalException) {
                throw;
            } catch (Exception ex) {
                throw new LispEvalException($"{ex} -- {Name}", frame);
            }
        }
    }


    // Bound variable in a compiled lambda/macro expression
    sealed class Arg {
        public readonly int Level;
        public readonly int Offset;
        public readonly Sym Symbol;

        public Arg(int level, int offset, Sym symbol) {
            Level = level;
            Offset = offset;
            Symbol = symbol;
        }
            
        public override string ToString() => $"#{Level}:{Offset}:{Symbol}";

        // Set a value x to the location corresponding to the variable in env.
        public void SetValue(object x, Cell env) {
            for (int i = 0; i < Level; i++)
                env = (Cell) env.Cdr;
            object[] frame = (object[]) env.Car;
            frame[Offset] = x;
        }

        // Get a value from the location corresponding to the variable in env.
        public object GetValue(Cell env) {
            for (int i = 0; i < Level; i++)
                env = (Cell) env.Cdr;
            object[] frame = (object[]) env.Car;
            if (frame == null || Offset >= frame.Length)
                throw new IndexOutOfRangeException();
            return frame[Offset];
        }
    }


    // Exception which indicates on absence of a variable
    sealed class NotVariableException: LispEvalException {
        public NotVariableException(object x): base("variable expected", x) {}
    }

    //------------------------------------------------------------------
    public static Cell ToCons(IEnumerable seq)
    {
        if (!(seq is IEnumerable e)) 
            return null;
                
        Cell j = null;
        foreach (var item in e.Cast<object>().Reverse())
        {
            j = new Cell(item, j);
        }
        return j;
    }

    static bool isTrue(object test) => test != null && !(test is bool b && !b);

    /// <summary>Core of the Lisp interpreter</summary>
    public class Interpreter
    {
        private static int totalEvaluations = 0;
        public static int TotalEvaluations => Interlocked.CompareExchange(ref totalEvaluations, 0, 0);
            
        public int Evaluations { get; set; }

        /// <summary>Table of the global values of symbols</summary>
        internal readonly Dictionary<Sym, object> Globals = new Dictionary<Sym, object>();

        public object GetSymbolValue(string name) => Globals.TryGetValue(Sym.New(name), out var value)
            ? value.fromLisp()
            : null;

        public void SetSymbolValue(string name, object value) => Globals[Sym.New(name)] = value.unwrapScriptValue();

        /// <summary>Standard out</summary>
        public TextWriter COut { get; set; } = Console.Out;

        /// <summary>Set each built-in function/variable as the global value
        /// of symbol.</summary>
        public Interpreter() {
            InitGlobals();
        }

        public Interpreter(Interpreter globalInterp)
        {
            Globals = new Dictionary<Sym, object>(globalInterp.Globals); // copy existing globals
        }

        public string ReplEval(ScriptContext context, Stream outputStream, string lisp, Dictionary<string, object> args=null)
        {
            var returnResult = ScriptLispUtils.EnsureReturn(lisp);
            var page = new PageResult(context.LispSharpPage(returnResult)) {
                Args = {
                    [nameof(ScriptLisp)] = this
                }
            };
            args?.Each(x => page.Args[x.Key] = x.Value);
                
            this.Scope = new ScriptScopeContext(page, outputStream, args);

            var output = page.RenderScript();
            if (page.ReturnValue != null)
            {
                var ret = ScriptLanguage.UnwrapValue(page.ReturnValue.Result);
                if (ret == null)
                    return output;
                if (ret is Cell c)
                    return Str(c);
                if (ret is Sym sym)
                    return Str(sym);
                if (ret is string s)
                    return s;
                    
                if (Globals.TryGetValue(VERBOSE, out var verbose) && isTrue(verbose))
                    return ret.Dump();
                    
                return ret.ToSafeJsv();
            }
            return output;
        }

        Func<object, object> resolve1ArgFn(object f, Interpreter interp)
        {
            switch (f) {
                case Closure fnclosure:
                    return x => interp.invoke(fnclosure, x);
                case Macro fnmacro:
                    return x => interp.invoke(fnmacro, x);
                case BuiltInFunc fnbulitin:
                    return x => interp.invoke(fnbulitin, x);
                case Delegate fndel:
                    return x => interp.invoke(fndel, x);
                default:
                    throw new LispEvalException("not applicable", f);
            }
        }

        Func<object, object, object> resolve2ArgFn(object f, Interpreter interp)
        {
            switch (f) {
                case Closure fnclosure:
                    return (x,y) => interp.invoke(fnclosure, x,y);
                case Macro fnmacro:
                    return (x,y) => interp.invoke(fnmacro, x, y);
                case BuiltInFunc fnbulitin:
                    return (x,y) => interp.invoke(fnbulitin, x, y);
                case Delegate fndel:
                    return (x,y) => interp.invoke(fndel, x, y);
                default:
                    throw new LispEvalException("not applicable", f);
            }
        }

        Func<object, bool> resolvePredicate(object f, Interpreter interp)
        {
            var fn = resolve1ArgFn(f, interp);
            return x => isTrue(fn(x));
        }

        object invoke(Closure fnclosure, params object[] args)
        {
            var env = fnclosure.MakeEnv(this, ToCons(args), null);
            var ret = EvalProgN(fnclosure.Body, env);
            ret = Eval(ret, env);
            return ret;
        }

        object invoke(Macro fnmacro, params object[] args)
        {
            var ret = fnmacro.ExpandWith(this, ToCons(args));
            ret = Eval(ret, null);
            return ret;
        }

        object invoke(BuiltInFunc fnbulitin, params object[] args) => fnbulitin.Body(this, args);

        object invoke(Delegate fndel, params object[] args)
        {
            var scriptMethodArgs = new List<object>(EvalArgs(ToCons(args), this));
            var ret = JsCallExpression.InvokeDelegate(fndel, null, isMemberExpr: false, scriptMethodArgs);
            return ret.unwrapScriptValue();
        }

        List<object> toList(IEnumerable seq) => seq == null
            ? new List<object>()
            : seq.Cast<object>().ToList();

        class ObjectComparer : IComparer<object>
        {
            private readonly IComparer comparer;
            public ObjectComparer(IComparer comparer) => this.comparer = comparer;
            public int Compare(object x, object y) => comparer.Compare(x, y);

            public static IComparer<object> GetComparer(object x, Interpreter I)
            {
                if (x is IComparer<object> objComparer)
                    return objComparer;
                if (x is IComparer comparer)
                    return new ObjectComparer(comparer);
                if (x is Func<object, object, int> fnCompareTo)
                    return new FnComparer(fnCompareTo);
                if (x is Func<object, object, bool> fnEquals)
                    return new FnComparer(fnEquals);
                if (x is Closure fnclosure)
                    return new FnComparer(I, fnclosure);
                if (x is Macro fnmacro)
                    return new FnComparer(I, fnmacro);
                if (x is Delegate fndel)
                    return new FnComparer(fndel);
                throw new LispEvalException("Not a IComparer", x);
            }

            public static IEqualityComparer<object> GetEqualityComparer(object x, Interpreter I)
            {
                if (x is IEqualityComparer<object> objComparer)
                    return objComparer;
                if (x is Func<object, object, bool> fnEquals)
                    return new FnComparer(fnEquals);
                return (IEqualityComparer<object>) GetComparer(x, I);
            }
        }

        class FnComparer : IComparer, IComparer<object>, IEqualityComparer<object>
        {
            private Interpreter I;
            private readonly Closure fnclosure;
            private readonly Macro fnmacro;
            private readonly Delegate fndel;
            private readonly Func<object, object, int> fnCompareTo;
            private readonly Func<object, object, bool> fnCompareEquals;

            public FnComparer(Interpreter i) => I = i;
            public FnComparer(Interpreter I, Closure fnclosure) : this(I) => this.fnclosure = fnclosure;
            public FnComparer(Interpreter I, Macro fnmacro) : this(I) => this.fnmacro = fnmacro;
            public FnComparer(Func<object, object, int> fn) => this.fnCompareTo = fn;
            public FnComparer(Func<object, object, bool> fnCompareEquals) => this.fnCompareEquals = fnCompareEquals;
            public FnComparer(Delegate fndel) => this.fndel = fndel;

            public int Compare(object x, object y) =>
                fnclosure != null
                    ? DynamicInt.Instance.Convert(I.invoke(fnclosure, x, y))
                    : fnmacro != null
                        ? DynamicInt.Instance.Convert(I.invoke(fnclosure, x, y))
                        : fnCompareTo != null
                            ? fnCompareTo(x, y)
                            : DynamicInt.Instance.Convert(I.invoke(fndel, x, y));

            public new bool Equals(object x, object y) =>
                fnclosure != null
                    ? I.invoke(fnclosure, x, y).ConvertTo<bool>()
                    : fnmacro != null
                        ? I.invoke(fnclosure, x, y).ConvertTo<bool>()
                        : fnCompareEquals != null
                            ? fnCompareEquals(x, y)
                            : I.invoke(fndel, x, y).ConvertTo<bool>();

            public int GetHashCode(object obj) => obj.GetHashCode();
        }

        static ReadOnlyMemory<char> DownloadCachedUrl(ScriptScopeContext scope, string url, string cachePrefix, bool force=false)
        {
            var cachedContents = GetCachedContents(scope, url, cachePrefix, out var vfsCache, out var cachedPath);
            if (!force && cachedContents != null) 
                return cachedContents.Value;

            var text = url.GetStringFromUrl(requestFilter: req => req.With(c => c.UserAgent = "Script" + nameof(Lisp)));
            WriteCacheFile(scope, vfsCache, cachedPath, text.AsMemory());
            return text.AsMemory();
        }

        /// <summary>
        /// Cache final output from load reference 
        /// </summary>
        private static ReadOnlyMemory<char> WriteCacheFile(ScriptScopeContext scope, IVirtualPathProvider vfsCache, string cachedPath, ReadOnlyMemory<char> text)
        {
            if (vfsCache is IVirtualFiles vfsWrite)
            {
                try
                {
                    vfsWrite.WriteFile(cachedPath, text);
                }
                catch (Exception e)
                {
                    scope.Context.Log.Error($"Could not write cached file '{cachedPath}'", e);
                }
            }
            return text;
        }

        private static ReadOnlyMemory<char>? GetCachedContents(ScriptScopeContext scope, string url, string cachePrefix, out IVirtualPathProvider vfsCache, out string cachedPath)
        {
            vfsCache = scope.Context.CacheFiles ?? scope.Context.VirtualFiles;
            var fileName = VirtualPathUtils.SafeFileName(url);
            cachedPath = $".lisp/{cachePrefix}{fileName}";
            var cachedFile = vfsCache.GetFile(cachedPath);
            return cachedFile?.ReadAllTextAsMemory();
        }

        private static GithubGist DownloadCachedGist(ScriptScopeContext scope, string gistId, bool force=false)
        {
            var gistUrl = GitHubGateway.ApiBaseUrl.CombineWith($"gists/{gistId}");
            var gistJson = DownloadCachedUrl(scope, gistUrl, "gist_", force);
            var gist = JsonSerializer.DeserializeFromSpan<GithubGist>(gistJson.Span);
            return gist;
        }

        private static string GetGistContents(ScriptScopeContext scope, GistFile gistFile) => IsTruncated(gistFile)
            ? DownloadCachedUrl(scope, gistFile.Raw_Url, "gist_file_").ToString()
            : gistFile.Content;

        /// <summary>
        /// Load examples:
        ///   - file.l
        ///   - virtual/path/file.l
        ///   - index:lib-calc
        ///   - index:lib-calc/lib1.l
        ///   - gist:{gist-id}
        ///   - gist:{gist-id}/single-file.l
        ///   - https://mydomain.org/file.l
        /// </summary>
        static ReadOnlyMemory<char> lispContents(ScriptScopeContext scope, string path)
        {
            var isUrl = path.IndexOf("://", StringComparison.Ordinal) >= 0;
            if (path.StartsWith("index:") || path.StartsWith("gist:") || isUrl)
            {
                if (!AllowLoadingRemoteScripts)
                    throw new NotSupportedException($"Lisp.AllowLoadingRemoteScripts has been disabled");
                scope.Context.AssertProtectedMethods();

                if (isUrl)
                {
                    if (!path.StartsWith("https://"))
                        throw new NotSupportedException("https:// is required for loading remote scripts");

                    var textContents = DownloadCachedUrl(scope, path, "url_");
                    return textContents;
                }

                if (path.StartsWith("gist:"))
                {
                    var cachedContents = GetCachedContents(scope, path, "gist_", out var vfsCache, out var cachedPath);
                    if (cachedContents != null)
                        return cachedContents.Value;
                        
                    var gistId = path.RightPart(':');
                    var specificFile = gistId.IndexOf('/') >= 0
                        ? gistId.RightPart('/')
                        : null;
                    gistId = gistId.LeftPart('/');

                    var gist = DownloadCachedGist(scope, gistId);

                    if (specificFile != null)
                    {
                        if (!gist.Files.TryGetValue(specificFile, out var gistFile))
                            throw new NotSupportedException($"File '{specificFile}' does not exist in gist '{gistId}'");

                        var contents = GetGistContents(scope, gistFile);
                        return WriteCacheFile(scope, vfsCache, cachedPath, contents.AsMemory());
                    }

                    var sb = StringBuilderCache.Allocate();
                    foreach (var entry in gist.Files)
                    {
                        var contents = GetGistContents(scope, entry.Value);
                        sb.AppendLine(contents);
                    }
                    return WriteCacheFile(scope, vfsCache, cachedPath, StringBuilderCache.ReturnAndFree(sb).AsMemory());
                }

                if (path.StartsWith("index:"))
                {
                    var cachedContents = GetCachedContents(scope, path, "index_", out var vfsCache, out var cachedPath);
                    if (cachedContents != null)
                        return cachedContents.Value;
                        
                    if (IndexGistId == null)
                        throw new NotSupportedException("IndexGistId is unspecified");

                    var indexName = path.RightPart(':');
                    indexName = path.RightPart(':');
                    var specificFile = indexName.IndexOf('/') >= 0
                        ? indexName.RightPart('/')
                        : null;
                    indexName = indexName.LeftPart('/');
                        
                    var indexLinks = GetGistIndexLinks(scope);
                    var indexLink = indexLinks.FirstOrDefault(x => x.Name == indexName);

                    // If can't find named link index.md could be stale, re-download and cache
                    if (indexLink == null)
                    {
                        indexLinks = GetGistIndexLinks(scope, force: true);
                        indexLink = indexLinks.FirstOrDefault(x => x.Name == indexName);
                            
                        if (indexLink == null)
                            throw new NotSupportedException($"Could not resolve '{indexName}' from Gist Index '{IndexGistId}'");
                    }

                    if (!indexLink.Url.StartsWith("https://gist.github.com/"))
                        throw new NotSupportedException($"{indexName} '{indexLink.Url}' is not a Gist URL");

                    var gistId = indexLink.Url.LastRightPart('/');
                    var gist = DownloadCachedGist(scope, gistId);
                        
                    if (specificFile != null)
                    {
                        if (!gist.Files.TryGetValue(specificFile, out var gistFile))
                            throw new NotSupportedException($"File '{specificFile}' does not exist in gist '{gistId}'");

                        var contents = GetGistContents(scope, gistFile);
                        return WriteCacheFile(scope, vfsCache, cachedPath, contents.AsMemory());
                    }

                    var sb = StringBuilderCache.Allocate();
                    foreach (var entry in gist.Files)
                    {
                        var contents = GetGistContents(scope, entry.Value);
                        sb.AppendLine(contents);
                    }
                    return WriteCacheFile(scope, vfsCache, cachedPath, StringBuilderCache.ReturnAndFree(sb).AsMemory());
                }
            }
                
            var file = scope.Context.VirtualFiles.GetFile(path);
            if (file == null)
                throw new NotSupportedException($"File does not exist '{path}'");

            var lisp = file.ReadAllTextAsMemory();
            return lisp;
        }

        internal static List<GistLink> GetGistIndexLinks(ScriptScopeContext scope, bool force=false)
        {
            var gistIndex = DownloadCachedGist(scope, IndexGistId, force);
            if (!gistIndex.Files.TryGetValue("index.md", out var indexGistFile))
                throw new NotSupportedException($"IndexGistId '{IndexGistId}' does not contain index.md");

            var indexGistContents = GetGistContents(scope, indexGistFile);
            var indexLinks = GistLink.Parse(indexGistContents);
            return indexLinks;
        }

        private static bool IsTruncated(GistFile f) => (string.IsNullOrEmpty(f.Content) || f.Content.Length < f.Size) && f.Truncated;

        private static long gensymCounter = 0;

        public void InitGlobals()
        {
            Globals[TRUE] = Globals[BOOL_TRUE] = TRUE;
            Globals[BOOL_FALSE] = null;

            Def("load", 1, (I, a) => {
                var scope = I.AssertScope();

                var path = a[0] is string s
                    ? s
                    : a[0] is Sym sym
                        ? sym.Name + ".l"
                        : throw new LispEvalException("not a string or symbol name", a[0]);

                var cacheKey = nameof(Lisp) + ":load:" + path;
                var importSymbols = (Dictionary<Sym, object>) scope.Context.Cache.GetOrAdd(cacheKey, k => {

                    var span = lispContents(scope, path);
                    var interp = new Interpreter(I); // start from copy of these symbols
                    Run(interp, new Reader(span));

                    var globals = GlobalInterpreter.Globals;  // only cache + import new symbols not in Global Interpreter
                    var newSymbols = new Dictionary<Sym, object>();
                    foreach (var entry in interp.Globals)
                    {
                        if (!globals.ContainsKey(entry.Key))
                            newSymbols[entry.Key] = entry.Value;
                    }
                    return newSymbols;
                });

                foreach (var importSymbol in importSymbols)
                {
                    I.Globals[importSymbol.Key] = importSymbol.Value;
                }
                return null;
            });
                
            Def("load-src", 1, (I, a) => {
                var scope = I.AssertScope();

                var path = a[0] is string s
                    ? s
                    : a[0] is Sym sym
                        ? sym.Name + ".l"
                        : throw new LispEvalException("not a string or symbol name", a[0]);
                    
                var span = lispContents(scope, path);
                return span.ToString();
            });

            Def("error", 1, a => throw new Exception(((string)a[0])));

            Def("not", 1, a => a[0] == null || a[0] is false);
                
            Def("return", 1, (I, a) => {
                var scope = I.AssertScope();
                var ret = a == null 
                    ? null
                    : a[0] is Cell c 
                        ? EvalArgs(c, I)
                        : a[0];
                scope.ReturnValue(ret.fromLisp());
                return null;
            });
                
            Def("F", -1, (I, a) => {
                var scope = I.AssertScope();
                var args = EvalArgs(a[0] as Cell, I);
                if (!(args[0] is string fnName)) 
                    throw new LispEvalException($"F requires a string Function Reference", args[0]);

                var fnArgs = new List<object>();
                for (var i=1; i<args.Length; i++)
                    fnArgs.Add(args[i]);
                var fn = scope.Context.AssertProtectedMethods().F(fnName, fnArgs);
                var ret = JsCallExpression.InvokeDelegate(fn, null, isMemberExpr: false, fnArgs);
                return ret.unwrapScriptValue();
            });
                
            Def("C", -1, (I, a) => {
                var scope = I.AssertScope();
                var args = EvalArgs(a[0] as Cell, I);
                if (!(args[0] is string fnName)) 
                    throw new LispEvalException($"C requires a string Constructor Reference", args[0]);

                var fn = scope.Context.AssertProtectedMethods().C(fnName);
                var fnArgs = new List<object>();
                for (var i=1; i<args.Length; i++)
                    fnArgs.Add(args[i]);
                var ret = JsCallExpression.InvokeDelegate(fn, null, isMemberExpr: false, fnArgs);
                return ret;
            });
                
            Def("new", -1, (I, a) => {
                var scope = I.AssertScope();
                var args = EvalArgs(a[0] as Cell, I);
                var fnArgs = new List<object>();
                for (var i=1; i<args.Length; i++)
                    fnArgs.Add(args[i]);

                if (args[0] is string typeName)
                {
                    var ret = scope.Context.AssertProtectedMethods().@new(typeName, fnArgs);
                    return ret;
                }
                if (args[0] is Type type)
                {
                    var ret = scope.Context.AssertProtectedMethods().createInstance(type, fnArgs);
                    return ret;
                }
                throw new LispEvalException("new requires Type Name or Type", a[0]);
            });
                
            Def("to-delegate", 1, (I, a) => {
                var f = a[0];
                switch (f) {
                    case Closure fnclosure:
                        return (StaticMethodInvoker)(p => I.invoke(fnclosure, p));
                    case Macro fnmacro:
                        return (StaticMethodInvoker)(p => I.invoke(fnmacro, p));
                    case BuiltInFunc fnbulitin:
                        return (StaticMethodInvoker)(p => I.invoke(fnbulitin, p));
                    case Delegate fndel:
                        return (StaticMethodInvoker)(p => I.invoke(fndel, p));
                    default:
                        throw new LispEvalException("not applicable", f);
                }
            });
                
            Def("to-cons", 1, a => a[0] == null ? null : a[0] is IEnumerable e ? ToCons(e)
                : throw new LispEvalException("not IEnumerable", a[0]));
            Def("to-array", 1, a => toList(a[0] as IEnumerable).ToArray());
            Def("to-list", 1, a => toList(a[0] as IEnumerable));
            Def("to-dictionary", 2, (I, a) => EnumerableUtils.ToList(a[1].assertEnumerable()).ToDictionary(resolve1ArgFn(a[0], I)));

            Def("new-map", -1, (I, a) => EvalMapArgs(a[0] as Cell, I));

            // can use (:key x) as indexer instead, e.g. (:i array) (:"key" map) (:Prop obj) or (.Prop obj) 
            Def("nth", 2, a => {
                if (a[0] == null)
                    return null;
                if (!(a[1] is int i))
                    throw new LispEvalException("not integer", a[1]);
                if (a[0] is IList c)
                    return c[i];
                return a[0].assertEnumerable().Cast<object>().ElementAt(i);
            });

            Def("first", 1, a => EnumerableUtils.FirstOrDefault(a[0].assertEnumerable()));
            Def("second", 1, a => EnumerableUtils.ElementAt(a[0].assertEnumerable(), 1));
            Def("third", 1, a => EnumerableUtils.ElementAt(a[0].assertEnumerable(), 2));
            Def("rest", 1, body: a => a[0] is Cell c ? c.Cdr : EnumerableUtils.NullIfEmpty(EnumerableUtils.Skip(a[0].assertEnumerable(), 1)));
            Def("skip", 2, a => EnumerableUtils.Skip(a[1].assertEnumerable(), DynamicInt.Instance.Convert(a[0])));
            Def("take", 2, a => EnumerableUtils.Take(a[1].assertEnumerable(), DynamicInt.Instance.Convert(a[0])));
            Def("enumerator", 1, a => a[0] == null ? TypeConstants.EmptyObjectArray.GetEnumerator() : a[0].assertEnumerable().GetEnumerator());
            Def("enumeratorNext", 1, a => {
                if (!(a[0] is IEnumerator e))
                    throw new LispEvalException("not IEnumerator", a[0]);
                return e.MoveNext().lispBool();
            });
            Def("enumeratorCurrent", 1, a => {
                if (!(a[0] is IEnumerator e))
                    throw new LispEvalException("not IEnumerator", a[0]);
                return e.Current;
            });
            Def("dispose", 1, a => {
                using (a[0] as IDisposable) {}
                return null;
            });

            Def("map", 2, (I, a) => a[1]?.assertEnumerable().Map(resolve1ArgFn(a[0], I)));
            Def("map-where", 3, (I, a) => EnumerableUtils.ToList(a[2]?.assertEnumerable()).Where(resolvePredicate(a[0], I)).Map(resolve1ArgFn(a[1], I)));
            Def("where", 2, (I, a) => EnumerableUtils.ToList(a[1]?.assertEnumerable()).Where(resolvePredicate(a[0], I)).ToList());

            Def("dorun", 2, (I, a) => {
                var converter = resolve1ArgFn(a[0], I);
                foreach (var x in a[1]?.assertEnumerable())
                {
                    converter(x);
                } 
                return null;
            });

            Def("do", -1, (I, a) => enumerableArg(a).Cast<object>().Last());

            Def("reduce", -2, (I, a) => {
                var fn = resolve2ArgFn(a[0], I);
                var varArgs = EnumerableUtils.ToList(a[1].assertEnumerable());
                if (varArgs.Count == 1) // (reduce fn L)
                {
                    var list = EnumerableUtils.ToList(varArgs[0].assertEnumerable());
                    return list.Aggregate(fn);
                }
                else // (reduce fn L seed)
                {
                    var list = EnumerableUtils.ToList(varArgs[0].assertEnumerable());
                    var seed = varArgs[1];
                    return list.Aggregate(seed, fn);
                }
            });
                
            Def("flatten", -1, (I,a) => I.AssertScope().Context.DefaultMethods.flatten(a[0] as IEnumerable ?? a));

            Def("sort", 1, (I, a) => {
                var arr = a[0].assertEnumerable().Cast<object>().ToArray();
                Array.Sort(arr, (x,y) => x.compareTo(y));
                return arr;
            });

            Def("sort-by", -2, (I, a) => {
                var keyFn = resolve1ArgFn(a[0], I);
                var varArgs = EnumerableUtils.ToList(a[1].assertEnumerable());
                if (varArgs.Count == 1) // (sort-by keyfn list)
                {
                    var list = EnumerableUtils.ToList(varArgs[0].assertEnumerable()).ToArray();
                    Array.Sort(list, (x,y) => keyFn(x).compareTo(keyFn(y)));
                    return list;
                }
                else // (sort-by keyfn comparer list)
                {
                    if (!(varArgs[0] is IComparer comparer))
                        throw new LispEvalException("not IComparable", varArgs[1]);

                    var results = EnumerableUtils.ToList(varArgs[1].assertEnumerable()).OrderBy(keyFn, new ObjectComparer(comparer));
                    return results;
                }
            });
                
            Def("order-by", 2, (I, a) => {
                var keyFns = EnumerableUtils.ToList(a[0].assertEnumerable());
                    
                var list = a[1].assertEnumerable().Cast<object>();
                if (keyFns.Count == 0)
                    return list;

                IOrderedEnumerable<object> seq = null;

                for (var i = 0; i < keyFns.Count; i++)
                {
                    var keyFn = keyFns[i];
                    if (keyFn is Dictionary<string, object> obj)
                    {
                        var fn = obj.TryGetValue("key", out var oKey)
                            ? resolve1ArgFn(oKey, I)
                            : x => x;
                        var comparer = obj.TryGetValue("comparer", out var oComparer)
                            ? ObjectComparer.GetComparer(oComparer, I)
                            : Comparer<object>.Default;
                        var desc = obj.TryGetValue("desc", out var oDesc) 
                                   && oDesc != null && (oDesc == TRUE || (bool) oDesc);
                            
                        if (seq == null)
                            seq = desc 
                                ? list.OrderByDescending(fn, comparer)
                                : list.OrderBy(fn, comparer);
                        else
                            seq = desc
                                ? seq.ThenByDescending(fn, comparer)
                                : seq.ThenBy(fn, comparer); 
                    }
                    else
                    {
                        var fn = resolve1ArgFn(keyFn, I);
                        if (seq == null)
                            seq = list.OrderBy(fn);
                        else
                            seq = seq.ThenBy(fn);
                    }
                }

                return EnumerableUtils.ToList(seq);
            });

            Def("group-by", -2, (I, a) => {
                    
                var keyFn = resolve1ArgFn(a[0], I);
                var varArgs = EnumerableUtils.ToList(a[1].assertEnumerable());

                if (varArgs.Count == 1) // (group-by #(mod % 5) numbers)
                {
                    var list = EnumerableUtils.ToList(varArgs[0].assertEnumerable());
                    var ret = list.GroupBy(keyFn);
                    return ret;
                }
                if (varArgs.Count == 2 && varArgs[0] is Dictionary<string, object> obj)
                {
                    var mapFn = obj.TryGetValue("map", out var oKey)
                        ? resolve1ArgFn(oKey, I)
                        : x => x;
                    var comparer = obj.TryGetValue("comparer", out var oComparer)
                        ? ObjectComparer.GetEqualityComparer(oComparer, I)
                        : EqualityComparer<object>.Default;
                        
                    var list = EnumerableUtils.ToList(varArgs[1].assertEnumerable());
                    var ret = list.GroupBy(
                        keyFn, 
                        mapFn, 
                        comparer);
                    return ret;
                }
                    
                throw new LispEvalException("syntax: (group-by keyFn list) (group-by keyFn { :map mapFn :comparer comparer } list)", varArgs.Last());
            });
                
            Def("sum", 1, a => {
                object acc = 0;
                foreach (var num in a[0].assertEnumerable())
                    acc = DynamicNumber.Add(acc, num);
                return acc;
            });

            Def("str", -1, a => {
                var sb = StringBuilderCache.Allocate();
                var c = (Cell) a[0];
                foreach (var x in c)
                {
                    sb.Append(Str(x, false));
                }
                return StringBuilderCache.ReturnAndFree(sb);
            });
                
            Def("car", 1, a => (a[0] as Cell)?.Car);
            Def("cdr", 1, a => (a[0] as Cell)?.Cdr);
                
            Def("cons", 2, a => new Cell(a[0], a[1]));
            Def("atom", 1, a => (a[0] is Cell) ? null : TRUE);
            Def("eq", 2, a => (a[0] == a[1]) ? TRUE : null);

            Def("seq?", 1, a => a[0] is IEnumerable e ? TRUE : null);
            Def("consp", 1, a => a[0] is Cell c ? TRUE : null);
            Def("endp", 1, a => (a[0] is Cell c) 
                ? (c.Car == null ? TRUE : null)
                : a[0] == null 
                    ? TRUE 
                    : EnumerableUtils.FirstOrDefault(a[0].assertEnumerable()) == null ? TRUE : null);

            Def("list", -1, a => a[0]);
            Def("rplaca", 2, a => { ((Cell) a[0]).Car = a[1]; return a[1]; });
            Def("rplacd", 2, a => { ((Cell) a[0]).Cdr = a[1]; return a[1]; });
            Def("length", 1, a => {
                if (a[0] == null)
                    return 0;
                return DefaultScripts.Instance.length(a[0]);
            });

            Def("string", 1, a => $"{a[0]}");
            Def("string-downcase", 1, a => 
                (a[0] is string s) ? s.ToLower() : a[0] != null ? throw new Exception("not a string") : "");
            Def("string-upcase", 1, a => (a[0] is string s) ? s.ToUpper() : a[0] != null ? throw new LispEvalException("not a string", a[0]) : "");
            Def("string?", 1, a => a[0] is string ? TRUE : null);
            Def("number?", 1, a => DynamicNumber.IsNumber(a[0]?.GetType()) ? TRUE : null);
            Def("instance?", 2, (I, a) => I.AssertScope().Context.DefaultMethods.instanceOf(a[1], a[0] is Sym s ? s.Name : a[0]) ? TRUE : null);
            Def("eql", 2, a => a[0] == null 
                ? a[1] == null 
                    ? TRUE : null 
                : a[0].Equals(a[1]) 
                    ? TRUE : null);
            Def("<", 2, a => a[0].compareTo(a[1]) < 0 ? TRUE : null);

            Def("%", 2, a =>  DynamicNumber.Mod(a[0], a[1]));
            Def("mod", 2, a => {
                var x = a[0];
                var y = a[1];
                if ((DynamicNumber.CompareTo(x, 0) < 0 && DynamicNumber.CompareTo(y, 0) > 0)
                    || (DynamicNumber.CompareTo(x, 0) > 0 && DynamicNumber.CompareTo(y, 0) < 0))
                    return DynamicNumber.Mod(x, DynamicNumber.Add(y, y));
                return DynamicNumber.Mod(x, y);
            });

            Def("+", -1, a => FoldL((object)0, a[0] as IEnumerable ?? a, DynamicNumber.Add));
            Def("*", -1, a => FoldL((object)1, a[0] as IEnumerable ?? a, DynamicNumber.Mul));
            Def("-", -1, a => {
                var e = a[0] as IEnumerable ?? a;
                var rest = EnumerableUtils.SplitOnFirst(e, out var first);
                if (rest.Count == 0)
                    return DynamicNumber.Mul(first,-1);
                return FoldL(first, rest, DynamicNumber.Sub);
            });
            Def("/", -1, a => {
                var e = a[0] as IEnumerable ?? a;
                var rest = EnumerableUtils.SplitOnFirst(e, out var first);
                if (rest.Count == 0)
                    return DynamicNumber.Div(1, first);
                return FoldL(first, rest, DynamicNumber.Div);
            });
                
            Def("count", 1, a => EnumerableUtils.Count(a[0].assertEnumerable()));

            Def("remove", 2, a => {
                var oNeedle = a[0];
                if (oNeedle is string needle)
                    return a[1].ToString().Replace(needle,"");
                else if (a[1] is Cell c)
                {
                    var j = c;
                    while (j != null) {
                        var prev = j;
                        j = (Cell) j.Cdr;
                        if (j != null && Equals(j.Car,oNeedle))
                            prev.Cdr = j.Cdr;
                    }
                    return c;
                }
                if (a[1] is IEnumerable e)
                {
                    var to = new List<object>();
                    var find = a[1];
                    foreach (var x in e)
                    {
                        if (x == find)
                            continue;
                        to.Add(x);
                    }
                    return to; 
                }
                    
                throw new LispEvalException("not IEnumerable", a[1]);
            });

            Def("glob", 2, (I, a) => {
                var search = a[0];
                if (!(search is string pattern))
                    throw new LispEvalException("syntax: (glob <search> <list>)", a[0]);
                var to = new List<object>();
                foreach (var item in a[1].assertEnumerable())
                {
                    if (item.ToString().Glob(pattern))
                        to.Add(item);
                }
                return to;
            });

            Def("subseq", -2, a => {
                var c = (Cell) a[1];
                var startPos = c.Car != null ? DynamicInt.Instance.Convert(c.Car) : 0;
                var endPos = c.Cdr is Cell c2 ? DynamicInt.Instance.Convert(c2.Car) : -1;
                if (a[0] is string s)
                    return endPos >= 0 ? s.Substring(startPos, endPos - startPos) : s.Substring(startPos);
                if (a[0] is IEnumerable e)
                    return (endPos >= 0 ? e.Map(x => x).Skip(startPos).Take(endPos - startPos) : e.Map(x => x).Skip(startPos)); 
                    
                throw new Exception("not an IEnumerable");
            });
                
            object fnMathDivisor(object[] a, Func<object,object> fn) {
                var x = DynamicDouble.Instance.Convert(a[0]);
                var y = (Cell) a[1];
                if (y == null)
                    return fn(x);
                if (y.Cdr == null)
                    return fn(x / DynamicDouble.Instance.Convert(y.Car));
                throw new ArgumentException("one or two arguments expected");
            }
            Def("truncate", -2, a => fnMathDivisor(a, x => Math.Truncate(DynamicDouble.Instance.Convert(x))));
            Def("ceiling", -2, a => fnMathDivisor(a, x => Math.Ceiling(DynamicDouble.Instance.Convert(x))));
            Def("floor", -2, a => fnMathDivisor(a, x => Math.Floor(DynamicDouble.Instance.Convert(x))));
            Def("round", -2, a => fnMathDivisor(a, x => Math.Round(DynamicDouble.Instance.Convert(x))));

            Def("abs", 1, a => Math.Abs(DynamicDouble.Instance.Convert(a[0])));
            Def("sin", 1, a => Math.Sin(DynamicDouble.Instance.Convert(a[0])));
            Def("cos", 1, a => Math.Cos(DynamicDouble.Instance.Convert(a[0])));
            Def("tan", 1, a => Math.Tan(DynamicDouble.Instance.Convert(a[0])));
            Def("exp", 1, a => Math.Exp(DynamicDouble.Instance.Convert(a[0])));
            Def("expt", 2, a => Math.Pow(DynamicDouble.Instance.Convert(a[0]), DynamicDouble.Instance.Convert(a[1])));
            Def("sqrt", 1, a => Math.Sqrt(DynamicDouble.Instance.Convert(a[0])));
            Def("isqrt", 1, a => (int)Math.Sqrt(DynamicDouble.Instance.Convert(a[0])));

            Def("logand", -1, a => (a[0] is Cell c) 
                ? FoldL(DynamicLong.Instance.Convert(c.Car), (Cell) a[0], (i, j) => 
                    DynamicLong.Instance.Convert(i) & DynamicLong.Instance.Convert(j)) 
                : -1);
            Def("logior", -1, a => (a[0] is Cell c) 
                ? FoldL(DynamicLong.Instance.Convert(c.Car), (Cell) a[0], (i, j) => 
                    DynamicLong.Instance.Convert(i) | DynamicLong.Instance.Convert(j)) 
                : -1);
            Def("logxor", -1, a => (a[0] is Cell c) 
                ? FoldL(DynamicLong.Instance.Convert(c.Car), (Cell) a[0], (i, j) => 
                    DynamicLong.Instance.Convert(i) ^ DynamicLong.Instance.Convert(j)) 
                : -1);
                
            Def("min", -1, a => {
                var e = a[0] as IEnumerable ?? a;
                var rest = EnumerableUtils.SplitOnFirst(e, out var first);
                return FoldL(first, rest, DynamicNumber.Min);
            });
            Def("max", -1, a => {
                var e = a[0] as IEnumerable ?? a;
                var rest = EnumerableUtils.SplitOnFirst(e, out var first);
                return FoldL(first, rest, DynamicNumber.Max);
            });
            Def("average", -1, a => {
                var e = a[0] is Cell c ? (c.Car is Cell ca ? ca : c) : a[0] as IEnumerable ?? a;
                var rest = EnumerableUtils.SplitOnFirst(e, out var first);
                var ret = FoldL(first, rest, DynamicNumber.Add);
                return DynamicDouble.Instance.div(ret, rest.Count + 1);
            });

            Def("random", 1, a => {
                var d = (double)a[0];
                return d % 1 > 0
                    ? new Random().NextDouble() * d
                    : new Random().Next(0, (int)d);
            });
            Def("zerop", 1, a => DynamicDouble.Instance.Convert(a[0]) == 0d ? TRUE : null);

            object print(Interpreter I, string s)
            {
                if (I.Scope != null)
                    I.Scope.Value.Context.DefaultMethods.write(I.Scope.Value, s);
                else
                    COut.Write(s);
                return null;
            }

            IEnumerable enumerableArg(object[] a) => a.Length > 0 && a[0] is Cell cell ? (IEnumerable) cell : a;
                
            Def("print", -1, (I, a) => {
                var c = enumerableArg(a);
                foreach (var x in c)
                {
                    print(I, Str(x, false)); 
                }
                return I.Scope != null ? null : a.lastArg();
            });
            Def("println", -1, (I, a) => {
                var c = enumerableArg(a);
                foreach (var x in c)
                {
                    if (I.Scope != null)
                        I.Scope.Value.Context.DefaultMethods.write(I.Scope.Value, Str(x, false));
                    else
                        COut.WriteLine(Str(a[0], true));
                }
                print(I, "\n"); 
                return I.Scope != null ? null : a.lastArg();
            });
                
            // println with spaces
            Def("printlns", -1, (I, a) => {
                var c = enumerableArg(a);
                foreach (var x in c)
                {
                    if (I.Scope != null)
                        I.Scope.Value.Context.DefaultMethods.write(I.Scope.Value, Str(x, false) + " ");
                    else
                        COut.WriteLine(Str(a[0] + " ", true));
                }
                print(I, "\n"); 
                return I.Scope != null ? null : a.lastArg();
            });

            Def("htmldump", 1, (I, a) => print(I, I.AssertScope().Context.HtmlMethods.htmlDump(a[0]).ToRawString()));
            Def("textdump", 1, (I, a) => print(I, I.AssertScope().Context.DefaultMethods.textDump(a[0]).ToRawString()));

            // html encodes
            Def("pr", -1, (I, a) => {
                var c = enumerableArg(a);
                foreach (var x in c)
                    print(I, I.Scope.Value.Context.DefaultMethods.htmlEncode(Str(x, false))); 
                return I.Scope != null ? null : a.lastArg();
            });
            Def("prn", -1, (I, a) => {
                var c = enumerableArg(a);
                foreach (var x in c)
                {
                    if (I.Scope != null)
                        I.Scope.Value.Context.DefaultMethods.write(I.Scope.Value, I.Scope.Value.Context.DefaultMethods.htmlEncode(Str(x, false)));
                    else
                        COut.WriteLine(Str(a[0], true));
                }
                print(I, "\n"); 
                return I.Scope != null ? null : a.lastArg();
            });
                
            Def("dump", -1, (I, a) => {
                var c = enumerableArg(a);
                var defaultScripts = I.AssertScope().Context.DefaultMethods;
                foreach (var x in c)
                {
                    defaultScripts.write(I.AssertScope(), defaultScripts.dump(x).ToRawString());
                }
                print(I, "\n"); 
                return null;
            });
            Def("dump-inline", -1, (I, a) => {
                var c = enumerableArg(a);
                var defaultScripts = I.AssertScope().Context.DefaultMethods;
                foreach (var x in c)
                {
                    defaultScripts.write(I.AssertScope(), defaultScripts.jsv(x).ToRawString());
                }
                print(I, "\n"); 
                return null;
            });

            Def("debug", 0, a =>
                Globals.Keys.Aggregate((Cell) null, (x, y) => new Cell(y, x)));
                
            //use symbols script method
            //Def("symbols", 0, a =>  Globals.Keys.Map(x => x.Name).OrderBy(x => x).ToArray());
                
            //Use gistindex script method
            //Def("gist-index", 0, (I, a) => GetGistIndexLinks(I.AssertScope()));
                
            Def("prin1", 1, (I, a) => {
                print(I, Str(a[0], true)); 
                return a[0];
            });
            Def("princ", 1, (I, a) => {
                print(I, Str(a[0], false)); 
                return a[0];
            });
            Def("terpri", 0, (I, a) => {
                print(I, "\n"); 
                return TRUE;
            });

            var gensymCounterSym = Sym.New("*gensym-counter*");
            Globals[gensymCounterSym] = 1.0;
            Def("gensym", 0, a => {
                var x = Interlocked.Increment(ref gensymCounter);
                Globals[gensymCounterSym] = x;
                return new Sym($"G{(int) x}");
            });

            Def("make-symbol", 1, a => new Sym((string) a[0]));
            Def("intern", 1, a => Sym.New((string) a[0]));
            Def("symbol-name", 1, a => ((Sym) a[0]).Name);
            Def("symbol-type", 1, a => {
                var sym = a[0] as Sym ?? (a[0] is string s
                    ? Sym.New(s)
                    : throw new LispEvalException("Expected Symbol or Symbol Name", a[0]));
                if (Globals.TryGetValue(sym, out var value) && value != null)
                    return value.GetType().Name;
                return "nil";
            });

            Def("apply", 2, a => a[1] is Cell c 
                ? Eval(new Cell(a[0], MapCar(c, QqQuote)), null)
                : Eval(new Cell(a[0], MapCar(ToCons(a[1].assertEnumerable()), QqQuote)), null));

            Def("exit", 1, a => {
                Environment.Exit(DynamicInt.Instance.Convert(a[0]));
                return null;
            });

            Globals[Sym.New("*version*")] =
                new Cell(Env.VersionString,
                    new Cell("#Script Lisp", new Cell("based on Nukata Lisp Light v1.2", null)));
        }

        /// <summary>Define a built-in function by a name, an arity,
        /// and a body.</summary>
        public void Def(string name, int carity, BuiltInFuncBody body) {
            Globals[Sym.New(name)] = new BuiltInFunc(name, carity, body);
        }
        public void Def(string name, int carity, Func<object[], object> body) {
            Globals[Sym.New(name)] = new BuiltInFunc(name, carity, (I, a) => body(a));
        }

        public object Eval(IEnumerable<object> sExpressions) => Eval(sExpressions, null);

        public object Eval(IEnumerable<object> sExpressions, Cell env)
        {
            object lastResult = null;
            foreach (var sExpression in sExpressions)
            {
                lastResult = Eval(sExpression, env);
            }
            return lastResult;
        }

        public ScriptScopeContext? Scope { get; set; }

        public ScriptScopeContext AssertScope()
        {
            if (Scope != null)
                return Scope.Value;
                
            throw new NotSupportedException("Lisp Interpreter not configured with Required ScriptScopeContext");
        }

        public object Eval(object x) => Eval(x, null);
            
        /// <summary>Evaluate a Lisp expression in an environment.</summary>
        public object Eval(object x, Cell env) {
            try
            {
                ScriptScopeContext scope = default;
                var hasScope = false;
                if (Scope != null)
                {
                    hasScope = true;
                    scope = Scope.Value;
                    if (scope.PageResult.HaltExecution)
                        return null;
                    scope.PageResult.AssertNextEvaluation();
                }

                Evaluations++;
                Interlocked.Increment(ref totalEvaluations);

                for (;;)
                {
                    object value;
                    switch (x) {
                        case Arg xarg:
                            return xarg.GetValue(env);
                        case Sym xsym:
                            if (Globals.TryGetValue(xsym, out value))
                                return value;
                            if (hasScope)
                            {
                                var symName = xsym.Name;
                                if (scope.TryGetValue(symName, out value))
                                    return value;
                                
                                const int argsCount = 1;
                                // ScriptMethod or arg Delegate
                                var isScriptMethod = symName[0] == '/'; 
                                if ((isScriptMethod || symName[0].IsValidVarNameChar()) && 
                                    scope.TryGetMethod(symName.Substring(1), argsCount, out var fnDel, out var scriptMethod, out var requiresScope))
                                {
                                    return (StaticMethodInvoker)(a => 
                                    {
                                        var scriptMethodArgs = requiresScope
                                            ? new List<object> {scope}
                                            : new List<object>();
                                        scriptMethodArgs.AddRange(a);
                                        return JsCallExpression.InvokeDelegate(fnDel, scriptMethod, isMemberExpr: false, scriptMethodArgs).unwrapScriptValue();
                                    });
                                }
                                if (symName[0] == ':')
                                {
                                    return (StaticMethodInvoker) (a => {
                                        var name = symName.Substring(1);
                                        var key = int.TryParse(name, out var index)
                                            ? (object) index
                                            : name;
                                        var ret = scope.Context.DefaultMethods.get(a[0], key);
                                        return ret.unwrapScriptValue();
                                    });
                                }
                                if (symName[0] == '.')
                                {
                                    return (StaticMethodInvoker) (a => {
                                        
                                        var ret = scope.Context.AssertProtectedMethods().call(a[0], symName.Substring(1).Replace('+',','), TypeConstants.EmptyObjectList);
                                        return ret.unwrapScriptValue();
                                    });
                                }
                                if (symName.IndexOf('/') >= 0)
                                {
                                    var fnNet = scope.Context.AssertProtectedMethods().Function(
                                        symName.Replace('/', '.').Replace('+',','));
                                    if (fnNet != null)
                                    {
                                        return (StaticMethodInvoker) (a => 
                                            JsCallExpression.InvokeDelegate(fnNet, null, isMemberExpr: false, new List<object>(a)).unwrapScriptValue());
                                    }
                                }
                                else if (symName[symName.Length - 1] == '.') // constructor (Type. arg) https://clojure.org/reference/java_interop#_the_dot_special_form
                                {
                                    var typeName = symName.Substring(0, symName.Length - 1);
                                    var fnCtor = scope.Context.AssertProtectedMethods().Constructor(typeName);
                                    if (fnCtor != null)
                                    {
                                        return (StaticMethodInvoker) (a => 
                                            JsCallExpression.InvokeDelegate(fnCtor, null, isMemberExpr: false, new List<object>(a)).unwrapScriptValue());
                                    }
                                    throw new NotSupportedException(ProtectedScripts.TypeNotFoundErrorMessage(typeName));
                                }
                            }
                            throw new LispEvalException("void variable", x);
                        case Cell xcell:
                            var fn = xcell.Car;
                            Cell arg = CdrCell(xcell);
                            if (fn is Keyword) {
                                if (fn == QUOTE) {
                                    if (arg != null && arg.Cdr == null)
                                        return arg.Car;
                                    throw new LispEvalException("bad quote", x);
                                } else if (fn == PROGN) {
                                    x = EvalProgN(arg, env);
                                } else if (fn == COND) {
                                    x = EvalCond(arg, env);
                                } else if (fn == SETQ) {
                                    return EvalSetQ(arg, env);
                                } else if (fn == EXPORT) {
                                    return EvalExport(arg, env, scope);
                                } else if (fn == LAMBDA || fn == FN) {
                                    return Compile(arg.unwrapDataListArgs(), env, Closure.Make);
                                } else if (fn == MACRO) {
                                    if (env != null)
                                        throw new LispEvalException("nested macro", x);
                                    return Compile(arg, null, Macro.Make);
                                } else if (fn == QUASIQUOTE) {
                                    if (arg != null && arg.Cdr == null)
                                        x = QqExpand(arg.Car);
                                    else
                                        throw new LispEvalException ("bad quasiquote",
                                            x);
                                } else {
                                    throw new LispEvalException("bad keyword", fn);
                                }
                            } else { // Application of a function
                                if (fn is Sym fnsym) 
                                {
                                    if (fnsym == BOUND)
                                    {
                                        foreach (var name in arg)
                                        {
                                            if (!(name is Sym checksym))
                                                throw new LispEvalException("not Sym", name);

                                            var ret = Globals.ContainsKey(checksym) || hasScope && scope.TryGetValue(checksym.Name, out _);
                                            if (!ret)
                                                return null;
                                        }
                                        return TRUE;
                                    }
                                    
                                    // Expand fn = Eval(fn, env) here for speed.
                                    if (Globals.TryGetValue(fnsym, out value))
                                    {
                                        fn = value;
                                    }
                                    else if (hasScope)
                                    {
                                        var fnName = fnsym.Name;
                                        var fnArgs = EvalArgs(arg, this, env);
                                        var argLength = arg?.Length ?? 0;

                                        // ScriptMethod or arg Delegate
                                        var isScriptMethod = fnName[0] == '/'; 
                                        if ((isScriptMethod || fnName[0].IsValidVarNameChar()) && scope.TryGetMethod(isScriptMethod ? fnName.Substring(1) : fnName, argLength,
                                                out var fnDel, out var scriptMethod, out var requiresScope))
                                        {
                                            var scriptMethodArgs = requiresScope
                                                ? new List<object> {scope}
                                                : new List<object>();
                                            scriptMethodArgs.AddRange(fnArgs);

                                            var ret = JsCallExpression.InvokeDelegate(fnDel, scriptMethod, isMemberExpr: false, scriptMethodArgs);
                                            return ret.unwrapScriptValue();
                                        }
                                        if (isScriptMethod)
                                            throw new NotSupportedException($"Could not resolve #Script method '{fnName.Substring(1)}'");
                                        
                                        if (fnName[0] == ':')
                                        {
                                            var name = fnArgs.Length == 1
                                                ? fnName.Substring(1)
                                                : fnArgs.Length == 2 && fnArgs[0] is string s
                                                    ? s
                                                    : throw new NotSupportedException(":index access requires 1 instance target or a string key");      
                                                
                                            var target = fnArgs[fnArgs.Length - 1];
                                            if (target == null)
                                                return null;
                                            var key = int.TryParse(name, out var index)
                                                ? (object) index
                                                : name;
                                            var ret = scope.Context.DefaultMethods.get(target, key);
                                            return ret.unwrapScriptValue();
                                        }
                                        if (fnName[0] == '.') // member method https://clojure.org/reference/java_interop#_member_access
                                        {
                                            if (fnArgs.Length == 0)
                                                throw new NotSupportedException(".memberAccess requires an instance target");
                                                
                                            var target = fnArgs[0];
                                            if (target == null)
                                                return null;
                                            var methodArgs = new List<object>();
                                            for (var i=1; i<fnArgs.Length; i++)
                                                methodArgs.Add(fnArgs[i]);
                                            
                                            var ret = scope.Context.AssertProtectedMethods().call(target, fnName.Substring(1), methodArgs);
                                            return ret.unwrapScriptValue();
                                        }
                                        if (fnName.IndexOf('/') >= 0) // static method https://clojure.org/reference/java_interop#_member_access
                                        {
                                            var fnArgsList = new List<object>(fnArgs);
                                            var fnNet = scope.Context.AssertProtectedMethods().Function(
                                                fnName.Replace('/', '.').Replace('+',','), fnArgsList);
                                                
                                            if (fnNet != null)
                                            {
                                                var ret = JsCallExpression.InvokeDelegate(fnNet, null, isMemberExpr: false, fnArgsList);
                                                return ret.unwrapScriptValue();
                                            }
                                        }
                                        else if (fnName[fnName.Length - 1] == '.') // constructor (Type. arg) https://clojure.org/reference/java_interop#_the_dot_special_form
                                        {
                                            var typeName = fnName.Substring(0, fnName.Length - 1);
                                            var ret = scope.Context.AssertProtectedMethods().@new(typeName, new List<object>(fnArgs));
                                            if (ret == null)
                                                throw new NotSupportedException(ProtectedScripts.TypeNotFoundErrorMessage(typeName));
                                                
                                            return ret;
                                        }
                                    }
                                    if (fn == null) 
                                        throw new LispEvalException("undefined", fnsym); 
                                } else {
                                    fn = Eval(fn, env);
                                }
                                switch (fn) {
                                    case Closure fnclosure:
                                        env = fnclosure.MakeEnv(this, arg, env);
                                        x = EvalProgN(fnclosure.Body, env);
                                        break;
                                    case Macro fnmacro:
                                        x = fnmacro.ExpandWith(this, arg);
                                        break;
                                    case BuiltInFunc fnbulitin:
                                        return fnbulitin.EvalWith(this, arg, env);
                                    case Delegate fnDel:
                                        var scriptMethodArgs = new List<object>(EvalArgs(arg, this, env));
                                        var ret = JsCallExpression.InvokeDelegate(fnDel, null, isMemberExpr: false, scriptMethodArgs);
                                        return ret.unwrapScriptValue();
                                    default:
                                        throw new LispEvalException("not applicable", fn);
                                }
                            }
                            break;
                        case Lambda xlambda:
                            return new Closure(xlambda, env);
                        default:
                            return x; // numbers, strings, null etc.
                    }
                }
            } catch (LispEvalException ex) {
                if (ex.Trace.Count < 10)
                    ex.Trace.Add(Str(x));
                throw;
            }
        }

        public static object[] EvalArgs(Cell arg, Interpreter interp, Cell env=null)
        {
            if (arg == null)
                return TypeConstants.EmptyObjectArray;
                
            var n = arg.Length;
            var frame = new object[n];

            int i;
            for (i = 0; i < n && arg != null; i++) {
                // Set the list of fixed arguments.
                frame[i] = arg.Car;
                arg = CdrCell(arg);
            }

            for (i = 0; i < n; i++)
                frame[i] = interp.Eval(frame[i], env);
                
            return frame;
        }

        public static Dictionary<string, object> EvalMapArgs(Cell arg, Interpreter interp, Cell env=null)
        {
            if (arg == null)
                return TypeConstants.EmptyObjectDictionary;
                
            var to = new Dictionary<string, object>();
            var n = arg.Length;
            var frame = new object[n];

            int i;
            for (i = 0; i < n && arg != null; i++) {
                // Set the list of fixed arguments.
                frame[i] = arg.Car;
                arg = CdrCell(arg);
            }

            for (i = 0; i < n; i++)
            {
                if (!(frame[i] is Cell c))
                    throw new LispEvalException("Expected Cell Key/Value Pair", frame[i]);

                var key = c.Car is Sym sym
                    ? sym.Name
                    : c.Car is string s
                        ? s
                        : throw new LispEvalException("Map Key ", c.Car);

                if (!(c.Cdr is Cell v))
                    throw new LispEvalException("Expected Cell Value", c.Cdr);
                    
                var value = interp.Eval(v.Car, env);
                to.Add(key, value);
            }
                
            return to;
        }

        // (progn E1 ... En) => Evaluate E1, ... except for En and return it.
        object EvalProgN(Cell j, Cell env) {
            if (j == null)
                return null;
            for (;;) {
                var x = j.Car;
                j = CdrCell(j);
                if (j == null)
                    return x; // The tail expression to be evaluated later
                Eval(x, env);
            }
        }

        // Evaluate a conditional expression and return the selection.
        object EvalCond(Cell j, Cell env) {
            for (; j != null; j = CdrCell(j)) {
                var clause = j.Car;
                if (clause != null) {
                    if (clause is Cell k) {
                        var result = Eval(k.Car, env);
                        var f = result is bool b && !b;
                        if (result != null && !f) { // If the condition holds //DB: added false check
                            Cell body = CdrCell(k);
                            if (body == null)
                                return QqQuote(result);
                            else
                                return EvalProgN(body, env);
                        }
                    } else {
                        throw new LispEvalException("cond test expected", clause);
                    }
                }
            }
            return null;        // No clause holds.
        }

        // (setq V1 E1 ..) => Evaluate Ei and assign it to Vi; return the last.
        object EvalSetQ(Cell j, Cell env) {
            object result = null;
            for (; j != null; j = CdrCell(j)) {
                var lval = j.Car;
                if (lval == TRUE)
                    throw new LispEvalException("not assignable", lval);
                j = CdrCell(j);
                if (j == null)
                    throw new LispEvalException("right value expected", lval);
                result = Eval(j.Car, env);
                switch (lval) {
                    case Arg arg:
                        arg.SetValue(result, env);
                        break;
                    case Sym sym when !(sym is Keyword):
                        Globals[sym] = result;
                        break;
                    default:
                        throw new NotVariableException(lval);
                }
            }
            return result;
        }

        // (export V1 E1 ..) => Evaluate Ei and assign it to Vi in PageResult.Args; return null.
        object EvalExport(Cell j, Cell env, ScriptScopeContext scope) {
            if (scope.PageResult == null)
                throw new NotSupportedException("scope is undefined");
            object result = null;
            for (; j != null; j = CdrCell(j)) {
                var lval = j.Car;
                if (lval == TRUE)
                    throw new LispEvalException("not assignable", lval);
                j = CdrCell(j);
                if (j == null)
                    throw new LispEvalException("right value expected", lval);
                result = Eval(j.Car, env);
                switch (lval) {
                    case Arg arg:
                        arg.SetValue(result, env);
                        break;
                    case Sym sym when !(sym is Keyword):
                        scope.PageResult.Args[sym.Name] = result;
                        break;
                    default:
                        throw new NotVariableException(lval);
                }
            }
            return null;
        }

        // { :k1 v1 :k2 v2 } => Evaluate Object Dictionary (comma separators optional)
        object EvalMap(Cell j, Cell env) {
            object result = null;
            for (; j != null; j = CdrCell(j)) {
                var lval = j.Car;
                if (lval == TRUE)
                    throw new LispEvalException("not assignable", lval);
                j = CdrCell(j);
                if (j == null)
                    throw new LispEvalException("right value expected", lval);
                result = Eval(j.Car, env);
                switch (lval) {
                    case Arg arg:
                        arg.SetValue(result, env);
                        break;
                    case Sym sym when !(sym is Keyword):
                        Scope.Value.PageResult.Args[sym.Name] = result;
                        break;
                    default:
                        throw new NotVariableException(lval);
                }
            }
            return null;
        }

        // Compile a Lisp list (macro ...) or (lambda ...).
        DefinedFunc Compile(Cell arg, Cell env, FuncFactory make) {
            if (arg == null)
                throw new LispEvalException("arglist and body expected", arg);
            var table = new Dictionary<Sym, Arg>();
            bool hasRest = MakeArgTable(arg.Car, table);
            int arity = table.Count;
            Cell body = CdrCell(arg);
            body = ScanForArgs(body, table) as Cell;
            body = ExpandMacros(body, 20, env) as Cell; // Expand up to 20 nestings.
            body = CompileInners(body) as Cell;
            return make(hasRest ? -arity : arity, body, env);
        }

        // Expand macros and quasi-quotations in an expression.
        object ExpandMacros(object j, int count, Cell env) {
            if ((j is Cell cell) && count > 0) {
                var k = cell.Car;
                if (k == QUOTE || k == LAMBDA || k == FN || k == MACRO) {
                    return cell;
                } else if (k == QUASIQUOTE) {
                    Cell d = CdrCell(cell);
                    if (d != null && d.Cdr == null) {
                        var z = QqExpand(d.Car);
                        return ExpandMacros(z, count, env);
                    }
                    throw new LispEvalException("bad quasiquote", cell);
                } else {
                    if (k is Sym sym)
                        k = Globals.ContainsKey(sym) ? Globals[sym] : null;
                    if (k is Macro macro) {
                        Cell d = CdrCell(cell);
                        var z = macro.ExpandWith(this, d);
                        return ExpandMacros(z, count - 1, env);
                    } else {
                        return MapCar(cell, x => ExpandMacros(x, count, env));
                    }
                }
            } else {
                return j;
            }
        }

        // Replace inner lambda-expressions with Lambda instances.
        object CompileInners(object j) {
            if (j is Cell cell) {
                var k = cell.Car;
                if (k == QUOTE) {
                    return cell;
                } else if (k == LAMBDA || k == FN) {
                    Cell d = CdrCell(cell).unwrapDataListArgs();
                    return Compile(d, null, Lambda.Make);
                } else if (k == MACRO) {
                    throw new LispEvalException("nested macro", cell);
                } else {
                    return MapCar(cell, CompileInners);
                }
            } else {
                return j;
            }
        }
    }

    //------------------------------------------------------------------

    // Make an argument-table; return true if there is a rest argument.
    static bool MakeArgTable(object arg, IDictionary<Sym, Arg> table) {
        if (arg == null) {
            return false;
        } else if (arg is Cell argcell) {
            int offset = 0;     // offset value within the call-frame
            bool hasRest = false;
            for (; argcell != null; argcell = CdrCell(argcell)) {
                var j = argcell.Car;
                if (hasRest)
                    throw new LispEvalException("2nd rest", j);
                if (j == REST) { // &rest var
                    argcell = CdrCell(argcell);
                    if (argcell == null)
                        throw new NotVariableException(argcell);
                    j = argcell.Car;
                    if (j == REST)
                        throw new NotVariableException(j);
                    hasRest = true;
                }
                Sym sym = j as Sym;
                if (sym == null) {
                    Arg jarg = j as Arg;
                    if (jarg != null)
                        sym = jarg.Symbol;
                    else
                        throw new NotVariableException(j);
                }
                if (sym == TRUE)
                    throw new LispEvalException("not assignable", sym);
                if (table.ContainsKey(sym))
                    throw new LispEvalException("duplicated argument name", sym);
                table[sym] = new Arg(0, offset, sym);
                offset++;
            }
            return hasRest;
        } else {
            throw new LispEvalException("arglist expected", arg);
        }
    }

    // Scan 'j' for formal arguments in 'table' and replace them with Args.
    // And scan 'j' for free Args not in 'table' and promote their levels.
    static object ScanForArgs(object j, IDictionary<Sym, Arg> table) {
        switch (j) {
            case Sym sym:
                return ((table.TryGetValue(sym, out Arg a)) ? a :
                    j);
            case Arg arg:
                return ((table.TryGetValue(arg.Symbol, out Arg k)) ? k :
                    new Arg(arg.Level + 1, arg.Offset, arg.Symbol));
            case Cell cell:
                if (cell.Car == QUOTE)
                    return j;
                else if (cell.Car == QUASIQUOTE)
                    return new Cell(QUASIQUOTE, 
                        ScanForQQ(cell.Cdr, table, 0));
                else
                    return MapCar(cell, x => ScanForArgs(x, table));
            default:
                return j;
        }
    }

    // Scan for quasi-quotes and ScanForArgs them depending on the nesting
    // level.
    static object ScanForQQ(object j, IDictionary<Sym, Arg> table, int level) {
        if (j is Cell cell) {
            var car = cell.Car;
            var cdr = cell.Cdr;
            if (car == QUASIQUOTE) {
                return new Cell(car, ScanForQQ(cdr, table, level + 1));
            } else if (car == UNQUOTE || car == UNQUOTE_SPLICING) {
                var d = ((level == 0) ? ScanForArgs(cdr, table) :
                    ScanForQQ(cdr, table, level - 1));
                if (d == cdr)
                    return j;
                return new Cell(car, d);
            } else {
                return MapCar(cell, x => ScanForQQ(x, table, level));
            }
        } else {
            return j;
        }
    }

    //------------------------------------------------------------------
    // Quasi-Quotation

    /// <summary>Expand <c>x</c> of any quqsi-quotation <c>`x</c> into
    /// the equivalent S-expression.</summary>
    public static object QqExpand(object x) =>
        QqExpand0(x, 0);        // Begin with the nesting level 0.

    /// <summary>Quote <c>x</c> so that the result evaluates to <c>x</c>.
    /// </summary>
    public static object QqQuote(object x) =>
        (x is Sym || x is Cell) ? new Cell(QUOTE, new Cell(x, null)) : x;

    static object QqExpand0(object x, int level) {
        if (x is Cell cell) {
            if (cell.Car == UNQUOTE) { // ,a
                if (level == 0)
                    return CdrCell(cell).Car; // ,a => a
            }
            Cell t = QqExpand1(cell, level);
            if ((t.Car is Cell k) && t.Cdr == null) {
                if (k.Car == LIST || k.Car == CONS)
                    return k;
            }
            return new Cell(APPEND, t);
        } else {
            return QqQuote(x);
        }
    }

    // Expand x of `x so that the result can be used as an argument of append.
    // Example 1: (,a b) => h=(list a) t=((list 'b)) => ((list a 'b))
    // Example 2: (,a ,@(cons 2 3)) => h=(list a) t=((cons 2 3))
    //                              => ((cons a (cons 2 3)))
    static Cell QqExpand1(object x, int level) {
        if (x is Cell cell) {
            if (cell.Car == UNQUOTE) { // ,a
                if (level == 0)
                    return CdrCell(cell); // ,a => (a)
                level--;
            } else if (cell.Car == QUASIQUOTE) { // `a
                level++;
            }
            var h = QqExpand2(cell.Car, level);
            Cell t = QqExpand1(cell.Cdr, level); // != null
            if (t.Car == null && t.Cdr == null) {
                return new Cell(h, null);
            } else if (h is Cell hcell) {
                if (hcell.Car == LIST) {
                    if (t.Car is Cell tcar) {
                        if (tcar.Car == LIST) {
                            var hh = QqConcat(hcell, tcar.Cdr);
                            return new Cell(hh, t.Cdr);
                        }
                    }
                    if (hcell.Cdr != null) {
                        var hh = QqConsCons(CdrCell(hcell), t.Car);
                        return new Cell(hh, t.Cdr);
                    }
                }
            }
            return new Cell(h, t);
        } else {
            return new Cell(QqQuote(x), null);
        }
    }

    // (1 2), (3 4) => (1 2 3 4)
    static object QqConcat(Cell x, object y) =>
        (x == null) ? y :
            new Cell(x.Car, QqConcat(CdrCell(x), y));

    // (1 2 3), "a" => (cons 1 (cons 2 (cons 3 "a")))
    static object QqConsCons(Cell x, object y) =>
        (x == null) ? y :
            new Cell(CONS,
                new Cell(x.Car,
                    new Cell(QqConsCons(CdrCell(x), y), null)));

    // Expand x.car of `x so that the result can be used as an arg of append.
    // Example: ,a => (list a); ,@(foo 1 2) => (foo 1 2); b => (list 'b)
    static object QqExpand2(object y, int level) { // Let y be x.car.
        if (y is Cell cell) {
            if (cell.Car == UNQUOTE) { // ,a
                if (level == 0)
                    return new Cell(LIST, cell.Cdr); // ,a => (list a)
                level--;
            } else if (cell.Car == UNQUOTE_SPLICING) { // ,@a
                if (level == 0)
                    return CdrCell(cell).Car; // ,@a => a
                level--;
            } else if (cell.Car == QUASIQUOTE) { // `a
                level++;
            }
        }
        return new Cell(LIST, new Cell(QqExpand0(y, level), null));
    }

    /// <summary>
    /// Returns List of SExpression's
    /// </summary>
    public static List<object> Parse(string lisp) => Parse(lisp.AsMemory());

    /// <summary>
    /// Returns List of SExpression's
    /// </summary>
    public static List<object> Parse(ReadOnlyMemory<char> lisp)
    {
        var to = new List<object>();
        var reader = new Reader(lisp);
        while (true)
        {
            var sExp = reader.Read();
            if (sExp == Reader.EOF)
                return to;
            to.Add(sExp);
        }
    }

    //    public static object Run(Interp interp, TextReader input) => Run(interp, new Reader(input));

    /// <summary>Run Read-Eval Loop.</summary>
    private static object Run(Interpreter interp, Reader reader)
    {
        object lastResult = Reader.EOF;
        for (;;)
        {
            var sExp = reader.Read();
            if (sExp == Reader.EOF)
                return lastResult;
            lastResult = interp.Eval(sExp, null);
        }
    }

    //------------------------------------------------------------------

    /// <summary>Reader of Lisp expressions</summary>
    public class Reader
    {
        object Token;

        IEnumerator<string> Tokens = ((IEnumerable<string>) TypeConstants.EmptyStringArray).GetEnumerator();

        int LineNo = 0;

        /// <summary>Token of "End Of File"</summary>
        public static object EOF = new Sym("#EOF");

        private ReadOnlyMemory<char> source = default;
        public Reader(ReadOnlyMemory<char> source) => this.source = source;

        /// <summary>Read a Lisp expression and return it.</summary>
        /// <remarks>Return EOF if the input runs out.</remarks>
        public object Read()
        {
            try
            {
                ReadToken();
                return ParseExpression();
            }
            catch (FormatException ex)
            {
                throw new LispEvalException("syntax error",
                    $"{ex.Message} -- {LineNo}: {Line.ToString()}",
                    false);
            }
        }

        object ParseExpression()
        {
            if (Token == LEFT_PAREN)
            {
                // (a b c)
                ReadToken();
                return ParseListBody();
            }
            if (Token == LEFT_BRACKET)
            {
                // [a b c] => (list a b c)
                ReadToken();
                var sExpr = ParseDataListBody();
                return new Cell(LIST, sExpr);
            }
            else if (Token == SINGLE_QUOTE)
            {
                // 'a => (quote a)
                ReadToken();
                return new Cell(QUOTE,
                    new Cell(ParseExpression(), null));
            }
            else if (Token == BACK_QUOTE)
            {
                // `a => (quasiquote a)
                ReadToken();
                return new Cell(QUASIQUOTE,
                    new Cell(ParseExpression(), null));
            }
            else if (Token == COMMA)
            {
                // ,a => (unquote a)
                ReadToken();
                return new Cell(UNQUOTE,
                    new Cell(ParseExpression(), null));
            }
            else if (Token == COMMAND_AT)
            {
                // ,@a => (unquote-splicing a)
                ReadToken();
                return new Cell(UNQUOTE_SPLICING,
                    new Cell(ParseExpression(), null));
            }
            else if (Token == LEFT_BRACE)
            {
                // { :a 1 :b 2 :c 3 }    => (new-map '("a" 1) '("b" 2) '("c" 3) )
                // { "a" 1 "b" 2 "c" 3 } => (new-map '("a" 1) '("b" 2) '("c" 3) )
                ReadToken();

                var sExpr = ParseMapBody();
                return new Cell(NEWMAP, sExpr);
            }
            else if (Token == HASH) // #(+ 1 %) Clojure's anonymous function syntax https://clojure.org/guides/weird_characters#_anonymous_function
            {
                ReadToken(); // #
                // (a b c)
                ReadToken(); // (
                var body = ParseListBody();

                var maxArg = 0;
                body.Walk(c => {
                    if (c.Car is Sym l && l.Name[0] == '%')
                    {
                        if (l == PERCENT)
                        {
                            c.Car = ARG;
                        }
                        else
                        {
                            var numStr = l.Name.Substring(1);
                            if (!int.TryParse(numStr, out var lnum))
                                throw new FormatException("Not a numeric placeholder: " + l);
                            c.Car = Sym.New(ARG + numStr);
                            maxArg = Math.Max(maxArg, lnum);
                        }
                    }
                    if (c.Cdr is Sym r && r.Name[0] == '%')
                    {
                        if (r == PERCENT)
                        {
                            c.Cdr = ARG;
                        }
                        else
                        {
                            var numStr = r.Name.Substring(1);
                            if (!int.TryParse(numStr, out var rnum))
                                throw new FormatException("Not a numeric placeholder: " + r);
                            c.Car = Sym.New(ARG + numStr);
                            maxArg = Math.Max(maxArg, rnum);
                        }
                    }
                });
                    
                // #(* 2 %)     => (fn . ((_a . null) . ((* . (2 . (_a . null))) . null)))
                // #(* 2 %1 %2) => (fn . ((_a1 . (_a2 . null)) . ((* . (2 . (_a1 . (_a2 . null)))) . null)))

                var args = new Cell(ARG, null);
                if (maxArg > 0)
                {
                    var argsList = maxArg.Times(i => Sym.New("_a" + (i + 1)));
                    args = ToCons(argsList);
                }
                return new Cell(FN, new Cell(args, new Cell(body, null)));
            }
            else if (Token == DOT || Token == RIGHT_PAREN)
            {
                throw new FormatException($"unexpected {Token}");
            }
            else
            {
                return Token;
            }
        }

        Cell ParseMapBody()
        {
            if (Token == EOF)
                throw new FormatException("unexpected EOF");
            else if (Token == RIGHT_BRACE)
                return null;
            else
            {
                var symKey = ParseExpression();

                var keyString = symKey is Sym sym
                    ? (sym.Name[0] == ':' ? sym.Name.Substring(1) : throw new LispEvalException("Expected Key Symbol with ':' prefix", symKey))
                    : symKey is string s
                        ? s
                        : throw new LispEvalException("Expected Key Symbol or String", symKey);

                ReadToken();

                var e2 = ParseExpression();

                ReadToken();

                if (Token == COMMA)
                    ReadToken();

                var e3 = ParseMapBody();

                return new Cell(
                    new Cell(LIST, new Cell(keyString, new Cell(e2, null))), 
                    e3);
            }
        }

        Cell ParseDataListBody()
        {
            if (Token == EOF)
                throw new FormatException("unexpected EOF");
            else if (Token == RIGHT_BRACKET)
                return null;
            else
            {
                var e1 = ParseExpression();
                ReadToken();
                    
                if (Token == COMMA)
                    ReadToken();
                    
                object e2;
                if (Token == DOT)
                {
                    // (a . b)
                    ReadToken();
                    e2 = ParseExpression();
                    ReadToken();
                    if (Token != RIGHT_BRACKET)
                        throw new FormatException($"\")\" expected: {Token}");
                }
                else
                {
                    e2 = ParseDataListBody();
                }

                return new Cell(e1, e2);
            }
        }

        Cell ParseListBody()
        {
            if (Token == EOF)
                throw new FormatException("unexpected EOF");
            else if (Token == RIGHT_PAREN)
                return null;
            else
            {
                var e1 = ParseExpression();
                ReadToken();
                object e2;
                if (Token == DOT)
                {
                    // (a . b)
                    ReadToken();
                    e2 = ParseExpression();
                    ReadToken();
                    if (Token != RIGHT_PAREN)
                        throw new FormatException($"\")\" expected: {Token}");
                }
                else
                {
                    e2 = ParseListBody();
                }

                return new Cell(e1, e2);
            }
        }

        private IEnumerator<object> TokenObjects;
        private static char[] TokenDelims = {
            '"', ',', '(', ')', '`', '\'', '~', 
            '{','}',  //clojure map
            '#',      //clojure fn
            '[',']',  //clojure data list [e1 e2] => (list e1 e2)
        };

        static void AddToken(List<object> tokens, string s)
        {
            if (s == "nil")
                tokens.Add(null);
            else if (DynamicNumber.TryParse(s, out var num))
                tokens.Add(num);
            else
                tokens.Add(Sym.New(s));
        }

        private ReadOnlyMemory<char> Line;

        private int cursorPos = 0;

        void ReadToken()
        {
            while (TokenObjects == null || !TokenObjects.MoveNext())
            {
                LineNo++;

                if (!source.TryReadLine(out Line, ref cursorPos))
                {
                    Token = EOF;
                    return;
                }

                var tokens = new List<object>();

                var literal = Line;
                while (!literal.IsEmpty)
                {
                    literal = literal.AdvancePastWhitespace();
                    if (literal.IsEmpty)
                        break;

                    var c = literal.Span[0];
                    if (c == ';') // line comment
                        break;

                    if (Array.IndexOf(TokenDelims, c) >= 0)
                    {
                        if (c == ',' && literal.Span.SafeCharEquals(1, '@'))
                        {
                            tokens.Add(Sym.New(literal.Slice(0, 2).ToString()));
                            literal = literal.Advance(2);
                        }
                        else if (c == '"')
                        {
                            var endPos = literal.Span.IndexOfQuotedString('"', out var hasEscapeChars);
                            if (endPos == -1)
                                throw new FormatException($"unterminated string: {literal.DebugLiteral()}");

                            var rawString = literal.Slice(1, endPos - 1);
                            tokens.Add(hasEscapeChars
                                ? JsonTypeSerializer.Unescape(rawString.Span).ToString()
                                : rawString.ToString());
                            literal = literal.Advance(endPos + 1);
                        }
                        else
                        {
                            AddToken(tokens, literal.Slice(0, 1).ToString());
                            literal = literal.Advance(1);
                        }
                    }
                    else
                    {
                        var i = 1;
                        while (i < literal.Length && Array.IndexOf(TokenDelims, c = literal.Span[i]) == -1 &&
                               !c.IsWhiteSpace())
                            i++;

                        AddToken(tokens, literal.Slice(0, i).ToString());
                        literal = literal.Advance(i);
                    }
                }

                TokenObjects = tokens.GetEnumerator();
            }

            Token = TokenObjects.Current;
        }
    }

    //------------------------------------------------------------------

    /// <summary>Make a string representation of Lisp expression.</summary>
    /// <param name="x">Lisp expression</param>
    /// <param name="quoteString">flag whether to quote string</param>
    public static string Str(object x, bool quoteString=true) {
        // 4 is the threshold of ellipsis for circular lists
        return Str4(x, quoteString, 4, null);
    }

    // Mapping from a quote symbol to its string representation
    static readonly Dictionary<Sym, string> Quotes = new Dictionary<Sym, string> {
        [QUOTE] = "'",
        [QUASIQUOTE] = "`",
        [UNQUOTE] = ",",
        [UNQUOTE_SPLICING] = ",@"
    };

    static string Str4(object x, bool quoteString, int count,
        HashSet<Cell> printed) {
        switch (x) {
            case null:
                return "nil";
            case Cell cell:
                if ((cell.Car is Sym csym) && Quotes.ContainsKey(csym)) {
                    if ((cell.Cdr is Cell xcdr) && xcdr.Cdr == null)
                        return Quotes[csym]
                               + Str4(xcdr.Car, true, count, printed);
                }
                return "(" + StrListBody(cell, count, printed) + ")";
            case string st:
                if (! quoteString)
                    return st;
                var bf = new StringBuilder();
                bf.Append('"');
                foreach (char ch in st) {
                    switch (ch) {
                        case '\b': bf.Append(@"\b"); break;
                        case '\t': bf.Append(@"\t"); break;
                        case '\n': bf.Append(@"\n"); break;
                        case '\v': bf.Append(@"\v"); break;
                        case '\f': bf.Append(@"\f"); break;
                        case '\r': bf.Append(@"\r"); break;
                        case '"':  bf.Append("\\\""); break;
                        case '\\': bf.Append(@"\\"); break;
                        default: bf.Append(ch); break;
                    }
                }
                bf.Append('"');
                return bf.ToString();
            case Sym sym:
                if (sym == TRUE) return bool.TrueString;
                return (sym.IsInterned) ? sym.Name : $"#:{x}";
            default:
                return x.ToString();
        }
    }

    // Make a string representation of list omitting its "(" and ")".
    static string StrListBody(Cell x, int count, HashSet<Cell> printed) {
        if (printed == null)
            printed = new HashSet<Cell>();
        var s = new List<string>();
        object y;
        for (y = x; y is Cell cell; y = cell.Cdr) {
            if (printed.Add(cell)) {
                count = 4;
            } else {
                count--;
                if (count < 0) {
                    s.Add("..."); // an ellipsis for a circular list
                    return String.Join(" ", s);
                }
            }
            s.Add(Str4(cell.Car, true, count, printed));
        }
        if (y != null) {
            s.Add(".");
            s.Add(Str4(y, true, count, printed));
        }
        for (y = x; y is Cell cell; y = cell.Cdr)
            printed.Remove(cell);
        return String.Join(" ", s);
    }

    //------------------------------------------------------------------

    /// <summary>Run Read-Eval-Print Loop.</summary>
    /// <remarks>This never ends, use Ctrl+C to Exit. Exceptions are handled here and not thrown.</remarks>
    public static void RunRepl(ScriptContext context)
    {
        //remove sandbox restrictions
        bool breakLoop = false;
        context.MaxQuota = int.MaxValue;
        context.MaxEvaluations = long.MaxValue;
            
        var interp = CreateInterpreter();
            
        var sw = new StreamWriter(Console.OpenStandardOutput()) {
            AutoFlush = true
        };
        Console.SetOut(sw);
        using (sw)
        {
            while (!breakLoop)
            {
                interp.COut.Write("> ");
                try
                {
                    var sb = new StringBuilder();

                    var line = Console.ReadLine();
                    if (line == "clear")
                    {
                        Console.Clear();
                        continue;
                    }
                    if (line == "quit" || line == "exit")
                    {
                        Console.WriteLine($"Goodbye.\n\n");
                        return;
                    }
                        
                    sb.AppendLine(line);
                    while (Console.KeyAvailable) 
                        sb.AppendLine(Console.ReadLine());

                    if (sb.ToString().Trim().Length == 0)
                        continue;

                    var response = interp.ReplEval(context, sw.BaseStream, sb.ToString());
                    Console.WriteLine(response);
                } 
                catch (Exception ex) 
                {
                    interp.COut.WriteLine(ex.InnerException ?? ex);
                }
            }
        }
    }

    /// <summary>Lisp initialization script</summary>
    public static string InitScript = Prelude + LispCore + Extensions;
    
    public const string Prelude = @"
(setq defmacro
      (macro (name args &rest body)
             `(progn (setq ,name (macro ,args ,@body))
                     ',name)))

(defmacro defun (name args &rest body)
  `(progn (setq ,name (lambda ,args ,@body))
          ',name))

(defun caar (x) (car (car x)))
(defun cadr (x) (car (cdr x)))
(defun cdar (x) (cdr (car x)))
(defun cddr (x) (cdr (cdr x)))
(defun caaar (x) (car (car (car x))))
(defun caadr (x) (car (car (cdr x))))
(defun cadar (x) (car (cdr (car x))))
(defun caddr (x) (car (cdr (cdr x))))
(defun cdaar (x) (cdr (car (car x))))
(defun cdadr (x) (cdr (car (cdr x))))
(defun cddar (x) (cdr (cdr (car x))))
(defun cdddr (x) (cdr (cdr (cdr x))))
;(defun not (x) (eq x nil)) ; replaced with native: null || false
(defun cons? (x) (not (atom x)))
(defun identity (x) x)

(setq
=      eql
null   not
setcar rplaca
setcdr rplacd)

(defun > (x y) (< y x))
(defun >= (x y) (not (< x y)))
(defun <= (x y) (not (< y x)))
(defun /= (x y) (not (= x y)))
(defun not= (x y) (not (= x y)))

(defun equal (x y)
  (cond ((atom x) (eql x y))
        ((atom y) nil)
        ((equal (car x) (car y)) (equal (cdr x) (cdr y)))))

(defmacro if (test then &rest else)
  `(cond (,test ,then)
         ,@(cond (else `((t ,@else))))))

(defmacro when (test &rest body)
  `(cond (,test ,@body)))

(defmacro let (args &rest body)
  ((lambda (vars vals)
     (defun vars (x)
       (cond (x (cons (if (atom (car x))
                          (car x)
                        (caar x))
                      (vars (cdr x))))))
     (defun vals (x)
       (cond (x (cons (if (atom (car x))
                          nil
                        (cadar x))
                      (vals (cdr x))))))
     `((lambda ,(vars args) ,@body) ,@(vals args)))
   nil nil))

(defmacro letrec (args &rest body)      ; (letrec ((v e) ...) body...)
  (let (vars setqs)
    (defun vars (x)
      (cond (x (cons (caar x)
                     (vars (cdr x))))))
    (defun sets (x)
      (cond (x (cons `(setq ,(caar x) ,(cadar x))
                     (sets (cdr x))))))
    `(let ,(vars args) ,@(sets args) ,@body)))

(defun _append (x y)
  (if (null x)
      y
    (cons (car x) (_append (cdr x) y))))
(defmacro append (x &rest y)
  (if (null y)
      x
    `(_append ,x (append ,@y))))

(defmacro and (x &rest y)
  (if (null y)
      x
    `(cond (,x (and ,@y)))))

(defun mapcar (f x)
  (and x (cons (f (car x)) (mapcar f (cdr x)))))

(defmacro or (x &rest y)
  (if (null y)
      x
    `(cond (,x)
           ((or ,@y)))))

(defun listp (x)
  (or (null x) (cons? x)))    ; NB (list? (lambda (x) (+ x 1))) => nil

(defun memq (key x)
  (cond ((null x) nil)
        ((eq key (car x)) x)
        (t (memq key (cdr x)))))

(defun member (key x)
  (cond ((null x) nil)
        ((equal key (car x)) x)
        (t (member key (cdr x)))))

(defun assq (key alist)
  (cond (alist (let ((e (car alist)))
                 (if (and (cons? e) (eq key (car e)))
                     e
                   (assq key (cdr alist)))))))

(defun assoc (key alist)
  (cond (alist (let ((e (car alist)))
                 (if (and (cons? e) (equal key (car e)))
                     e
                   (assoc key (cdr alist)))))))

(defun _nreverse (x prev)
  (let ((next (cdr x)))
    (setcdr x prev)
    (if (null next)
        x
      (_nreverse next x))))
(defun nreverse (L)            ; (nreverse '(a b c d)) => (d c b a)
  (cond (L (_nreverse L nil))))

(defun last (L)
  (if (atom (cdr L))
      L
    (last (cdr L))))

(defun nconc (&rest lists)
  (if (null (cdr lists))
      (car lists)
    (if (null (car lists))
        (apply nconc (cdr lists))
      (setcdr (last (car lists))
              (apply nconc (cdr lists)))
      (car lists))))

(defmacro while (test &rest body)
  (let ((loop (gensym)))
    `(letrec ((,loop (lambda () (cond (,test ,@body (,loop))))))
       (,loop))))

(defmacro dolist (spec &rest body) ; (dolist (name list [result]) body...)
  (let ((name (car spec))
        (list (gensym)))
    `(let (,name
           (,list ,(cadr spec)))
       (while ,list
         (setq ,name (car ,list))
         ,@body
         (setq ,list (cdr ,list)))
       ,@(if (cddr spec)
             `((setq ,name nil)
               ,(caddr spec))))))

(defmacro dotimes (spec &rest body) ; (dotimes (name count [result]) body...)
  (let ((name (car spec))
        (count (gensym)))
    `(let ((,name 0)
           (,count ,(cadr spec)))
       (while (< ,name ,count)
         ,@body
         (setq ,name (+ ,name 1)))
       ,@(if (cddr spec)
             `(,(caddr spec))))))
    ";

    /// <summary>
    /// Lisp Common Utils 
    /// </summary>
    public const string LispCore = @"
(defmacro def (k v) 
    (list 'progn (list 'setq k v) nil ))

(defmacro incf (elem &rest num)
  (cond
    ((not num) 
        `(setq ,elem (+ 1 ,elem)) )
    (t `(setq ,elem (+ ,@num ,elem))) ))

(defmacro decf (elem &rest num)
  (cond
    ((not num) 
        `(setq ,elem (- ,elem 1)) )
    (t `(setq ,elem (- ,elem ,@num))) ))

(defun 1+ (n) (+ n 1))
(defun 1- (n) (- n 1))

(defun mapcan (f L)
  (apply nconc (mapcar f L)))

(defun mapc (f L)
  (mapcar f L) L)

(defmacro when (condition &rest body)
  `(if ,condition (progn ,@body)))
(defmacro unless (condition &rest body)
  `(if (not ,condition) (progn ,@body)))

(defmacro push-end (e L)              ; JS [].push
  `(setq ,L (append ,L (list ,e))) )
(defmacro push (e L)                  ; JS [].unshift
  `(setq ,L (cons ,e ,L)))
(defmacro pop (L)                     ; JS [].shift
  `(let ( (v (first ,L)) )
      (setq ,L (rest ,L)) 
    v))

(defun nthcdr (n L)
  (if (zero? n)
      L
      (nthcdr (- n 1) (cdr L))))

(defun butlast (L)
    (reverse (nthcdr 1 (reverse L))))
(defun nbutlast (L)
    (nreverse (nthcdr 1 (nreverse L))))

(defun remove-if (f L)
  (mapcan (fn (e) (if (f e) (list e) nil)) L) )

(defun some (f L)
    (let ((to nil))
      (while (and L (not (setq to (f (pop L))))))
      to))

(defun every (f L)
    (let ((to nil))
      (while (and L (setq to (f (pop L)))))
      to))

(defun reverse (L)
  (let ((to '()))
    (doseq (e L to)
      (push e to))
    to))

(defun elt (L n)
    (if (>= n (length L)) (error ""index out of range""))
    (let ((l L))
        (dotimes (i n)
            (setq l (rest l))
        )
    (first l)))

(defun range (&rest args)
    (let ( (to '()) )
        (cond 
            ((= (length args) 1) (dotimes (i (car args))
                (push i to)))
            ((= (length args) 2) (dotimes (i (- (cadr args) (car args)))
                (push (+ i (car args)) to))))
    (nreverse to)))

(defun set-difference (L1 L2)
  (if L2
        (let ((res nil))
          (doseq (e L1)
            (unless (member e L2)
              (push e res)))
          res)
      L1))

(defun union (L1 L2)
  (if L2
        (let ((res nil))
          (doseq (e L1)
            (unless (member e res)
              (push e res)))
          (doseq (e L2)
            (unless (member e res)
              (push e res)))
          res)
      L1))
";

    /// <summary>
    /// Popular Clojure + nicer UX Utils
    /// </summary>
    public const string Extensions = @"

(defmacro defn (name args &rest body)
  `(progn (setq ,name (lambda ,args ,@body))
          ',name))

(defmacro doseq (spec &rest body) ; (doseq (name seq [result]) body...)
  (let ( (name (first spec)) 
         (seq (second spec)) 
         (enum (gensym))  )
    `(let ( (,name) (,enum (enumerator ,seq)) )
       (while (enumeratorNext ,enum)
         (setq ,name (enumeratorCurrent ,enum))
         ,@body)
       (dispose ,enum)
  )))

(defmacro doseq-while (spec f &rest body) ; (doseq (name seq [result]) body...)
  (let ( (name (first spec)) 
         (seq (second spec)) 
         (enum (gensym))  )
    `(let ( (,name) (,enum (enumerator ,seq)) )
       (while (and (enumeratorNext ,enum) (,f (enumeratorCurrent ,enum)))
         (setq ,name (enumeratorCurrent ,enum))
         ,@body)
       (dispose ,enum)
  )))

(defmacro f++ (elem)
  `(1- (setq ,elem (+ 1 ,elem))))

(defun zip (f L1 L2)
  (let ( (to) ) 
    (doseq (a L1) 
      (doseq (b L2)
        (push (f a b) to)))
    (nreverse to)
  ))

(defun zip-where (fpred fmap L1 L2)
  (let ( (to) ) 
    (doseq (a L1) 
      (doseq (b L2)
        (if (fpred a b) 
            (push (fmap a b) to)) ))
    (nreverse to)
  ))

(defun skip-while (f L)
  (let ( (to) (go) ) 
    (doseq (e L)
      (if (not (f e)) (setq go t)) 
      (if go (push e to))
    )
    (nreverse to)
  ))

(defun take-while (f L)
  (let ( (to) ) 
    (doseq-while (e L) #(f %)
      (push e to))
    (nreverse to)
  ))

(defun assoc-key (k L) (first (assoc k L)))
(defun assoc-value (k L) (second (assoc k L)))

(defn even?  [n] (= (% n 2) 0))
(defn odd?   [n] (= (% n 2) 1))
(defn empty? [x] (not (and x (seq? x) (> (count x) 0) )))

(defun flatmap (f L)
  (flatten (map f L)))

(defun map-index (f L)
  (let ( (i -1) )
    (map (fn [x] (f x (incf i) )) L) ))

(defun filter-index (f L)
  (let ( (i -1) )
    (filter (fn [x] (f x (incf i) )) L) ))

(defun where-index (f L)
  (let ( (i -1) )
    (where (fn [x] (f x (incf i) )) L) ))

(defn globln [a L] (/joinln (glob a L)))

(setq
    1st     first
    2nd     second
    3rd     third
    next    rest
    inc     1+
    dec     1-
    it      identity
    atom?   atom
    cons?   consp
    list?   listp
    end?    endp
    zero?   zerop
    every?  every
    some?   some
    all?    every
    any?    some
    prs     printlns
    lower-case string-downcase 
    upper-case string-upcase

    ; clojure
    defn   defun
    filter remove-if
)
";

}