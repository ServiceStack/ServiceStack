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
using ServiceStack.Text;
using ServiceStack.Text.Json;

namespace ServiceStack.Script
{
    public class ScriptLisp : ScriptLanguage, IConfigureScriptContext
    {
        public static ScriptLanguage Language = new ScriptLisp();
        
        public override string Name => "lisp";

        public void Configure(ScriptContext context) => Lisp.Init();

        public override List<PageFragment> Parse(ScriptContext context, ReadOnlyMemory<char> body, ReadOnlyMemory<char> modifiers)
        {
            var quiet = false;
            
            if (!modifiers.IsEmpty)
            {
                quiet = modifiers.EqualsOrdinal("q") || modifiers.EqualsOrdinal("quiet") || modifiers.EqualsOrdinal("silent");
                if (!quiet)
                    throw new NotSupportedException($"Unknown modifier '{modifiers.ToString()}', expected 'code|q', 'code|quiet' or 'code|silent'");
            }

            return new List<PageFragment> { 
                new PageLispStatementFragment(context.ParseLisp(body)) {
                    Quiet = quiet
                } 
            };
        }

        public override async Task<bool> WritePageFragmentAsync(ScriptScopeContext scope, PageFragment fragment, CancellationToken token)
        {
            var page = scope.PageResult;
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
        internal static Lisp.Interpreter GetLispInterpreter(this PageResult pageResult, ScriptScopeContext scope)
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

        private static PageResult GetLispPageResult(ScriptContext context, string lisp, Dictionary<string, object> args)
        {
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
            return pageResult.EvaluateScript();
        }

        public static async Task<string> RenderLispAsync(this ScriptContext context, string lisp, Dictionary<string, object> args=null)
        {
            var pageResult = GetLispPageResult(context, lisp, args);
            return await pageResult.EvaluateScriptAsync();
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
            
            return returnValue;
        }

        public static async Task<T> EvaluateLispAsync<T>(this ScriptContext context, string lisp, Dictionary<string, object> args = null) =>
            (await context.EvaluateLispAsync(lisp, args)).ConvertTo<T>();
        
        public static async Task<object> EvaluateLispAsync(this ScriptContext context, string lisp, Dictionary<string, object> args=null)
        {
            var pageResult = GetLispPageResult(context, lisp, args);

            var ret = await pageResult.EvaluateResultAsync();
            if (!ret.Item1)
                throw new NotSupportedException(ScriptContextUtils.ErrorNoReturn);
            
            return ret.Item2;
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
            if (a is IEnumerable e)
                return e;
            throw new LispEvalException("not IEnumerable", a);
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

        static readonly Sym COND = Keyword.New("cond");
        static readonly Sym LAMBDA = Keyword.New("lambda");
        static readonly Sym FN = Keyword.New("fn");
        static readonly Sym MACRO = Keyword.New("macro");
        static readonly Sym PROGN = Keyword.New("progn");
        static readonly Sym QUASIQUOTE = Keyword.New("quasiquote");
        static readonly Sym QUOTE = Keyword.New("quote");
        static readonly Sym SETQ = Keyword.New("setq");
        static readonly Sym EXPORT = Keyword.New("export");

        static readonly Sym BACK_QUOTE = Sym.New("`");
        static readonly Sym COMMAND_AT = Sym.New(",@");
        static readonly Sym COMMA = Sym.New(",");
        static readonly Sym DOT = Sym.New(".");
        static readonly Sym LEFT_PAREN = Sym.New("(");
        static readonly Sym RIGHT_PAREN = Sym.New(")");
        static readonly Sym SINGLE_QUOTE = Sym.New("'");

        static readonly Sym APPEND = Sym.New("append");
        static readonly Sym CONS = Sym.New("cons");
        static readonly Sym LIST = Sym.New("list");
        static readonly Sym REST = Sym.New("&rest");
        static readonly Sym UNQUOTE = Sym.New("unquote");
        static readonly Sym UNQUOTE_SPLICING = Sym.New("unquote-splicing");

        static readonly Sym LEFT_BRACE = Sym.New("{");
        static readonly Sym RIGHT_BRACE = Sym.New("}");
        static readonly Sym HASH = Sym.New("#");
        static readonly Sym PERCENT = Sym.New("%");
        static readonly Sym NEWMAP = Sym.New("new-map");
        static readonly Sym ARG = Sym.New("_a");

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

        /// <summary>Core of the Lisp interpreter</summary>
        public class Interpreter {
            /// <summary>Table of the global values of symbols</summary>
            internal readonly Dictionary<Sym, object> Globals =
                new Dictionary<Sym, object>();

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

            Func<object, object> resolveFn(object f, Interpreter interp)
            {
                switch (f) {
                    case Closure fnclosure:
                        return x => 
                        {
                            var env = fnclosure.MakeEnv(interp, new Cell(x, null), null);
                            var ret = interp.EvalProgN(fnclosure.Body, env);
                            ret = interp.Eval(ret, env);
                            return ret;
                        };
                        break;
                    case Macro fnmacro:
                        return x => 
                        {
                            var ret = fnmacro.ExpandWith(interp, new Cell(x, null));
                            ret = interp.Eval(ret, null);
                            return ret;
                        };
                        break;
                    case BuiltInFunc fnbulitin:
                        return x => fnbulitin.Body(interp, new[] {new Cell(x, null)});
                    case Delegate fnDel:
                        return x => 
                        {
                            var scriptMethodArgs = new List<object>(EvalArgs(new Cell(x, null), interp));
                            var ret = JsCallExpression.InvokeDelegate(fnDel, null, isMemberExpr: false, scriptMethodArgs);
                            return ret;
                        };
                    default:
                        throw new LispEvalException("not applicable", f);
                }
            }

            List<object> toList(IEnumerable seq) => seq == null
                ? new List<object>()
                : seq.Cast<object>().ToList();

            public void InitGlobals()
            {
                Globals[TRUE] = TRUE;
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
                    return ret;
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
                
                Def("new-map", -1, (I, a) => EvalMapArgs(a[0] as Cell, I));

                Def("to-cons", 1, a => a[0] == null ? null : a[0] is IEnumerable e ? ToCons(e)
                    : throw new LispEvalException("not IEnumerable", a[0]));
                Def("to-list", 1, a => toList(a[0] as IEnumerable));
                Def("to-array", 1, a => toList(a[0] as IEnumerable).ToArray());

                Def("nth", 2, (a) => {
                    if (a[0] == null)
                        return null;
                    if (!(a[1] is int i))
                        throw new LispEvalException("not integer", a[1]);
                    if (a[0] is IList c)
                        return c[i];
                    return a[0].assertEnumerable().Cast<object>().ElementAt(i);
                });

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

                Def("map", 2, (I, a) => a[1]?.assertEnumerable().Map(resolveFn(a[0], I)));

                //Def("map", 2, (I, a) => a[1]?.assertEnumerable().Map(resolveFn(a[0], I)));

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
                    : a[0] == null ? TRUE : throw new ArgumentException("not a valid list"));

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
                Def("string?", 1, a => (a[0] is string) ? TRUE : null);
                Def("number?", 1, a => (DynamicNumber.IsNumber(a[0]?.GetType())) ? TRUE : null);
                Def("eql", 2, a => ((a[0] == null) ? ((a[1] == null) ?
                                                      TRUE : null) :
                                    a[0].Equals(a[1]) ? TRUE : null));
                Def("<", 2, a => a[0] == null || a[1] == null ? a[0] == a[1] 
                    : DynamicNumber.IsNumber(a[0].GetType())
                    ? (object) (DynamicNumber.CompareTo(a[0], a[1]) < 0 ? TRUE : null)
                    : a[0] is IComparable c
                        ? c.CompareTo(a[1]) < 0
                        : throw new LispEvalException("not IComparable", a[0]));

                Def("%", 2, a => 
                    DynamicNumber.Mod(a[0], a[1]));
                Def("mod", 2, a => {
                        var x = a[0];
                        var y = a[1];
                        if ((DynamicNumber.CompareTo(x, 0) < 0 && DynamicNumber.CompareTo(y, 0) > 0)
                            || (DynamicNumber.CompareTo(x, 0) > 0 && DynamicNumber.CompareTo(y, 0) < 0))
                            return DynamicNumber.Mod(x, DynamicNumber.Add(y, y));
                        return DynamicNumber.Mod(x, y);
                    });

                Def("+", -1, a => FoldL((object)0, (Cell) a[0], DynamicNumber.Add));
                Def("*", -1, a => FoldL((object)1, (Cell) a[0], DynamicNumber.Mul));
                Def("-", -2, a => {
                        var x = a[0];
                        var y = (Cell) a[1];
                        if (y == null)
                            return DynamicNumber.Mul(x,-1);
                        return FoldL(x, y, DynamicNumber.Sub);
                    });
                Def("/", -3, a => FoldL(DynamicNumber.Div(a[0], a[1]),
                                        (Cell) a[2],
                                        DynamicNumber.Div));
                
                Def("count", 1, a => (a[0] is IEnumerable e) ? e.Map(x => x).Count() : throw new LispEvalException("not an IEnumerable", a[0]));

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
                
                Def("max", -1, a => {
                    var c = (Cell) a[0];
                    var first = c.Car;
                    if (first is int iVal) 
                        return FoldL(iVal, c, (i, j) => Math.Max(i, (int)j));
                    if (first is double d) 
                        return FoldL(d, c, (i, j) => Math.Max(i, (double)j));
                    if (first is long l)
                        return FoldL(l, c, (i, j) => Math.Max(i, (long)j));
                    if (first is decimal dec)
                        return FoldL(dec, c, (i, j) => Math.Max(i, (decimal)j));
                    if (first is ulong u)
                        return FoldL(u, c, (i, j) => Math.Max(i, (ulong)j));
                    if (first is byte b)
                        return FoldL(b, c, (i, j) => Math.Max(i, (byte)j));
                    if (first is float f)
                        return FoldL(f, c, (i, j) => Math.Max(i, (float)j));
                    throw new LispEvalException("not a number", first);
                });

                Def("min", -1, a => {
                    var c = (Cell) a[0];
                    var first = c.Car;
                    if (first is int iVal) 
                        return FoldL(iVal, c, (i, j) => Math.Min(i, (int)j));
                    if (first is double d) 
                        return FoldL(d, c, (i, j) => Math.Min(i, (double)j));
                    if (first is long l)
                        return FoldL(l, c, (i, j) => Math.Min(i, (long)j));
                    if (first is decimal dec)
                        return FoldL(dec, c, (i, j) => Math.Min(i, (decimal)j));
                    if (first is ulong u)
                        return FoldL(u, c, (i, j) => Math.Min(i, (ulong)j));
                    if (first is byte b)
                        return FoldL(b, c, (i, j) => Math.Min(i, (byte)j));
                    if (first is float f)
                        return FoldL(f, c, (i, j) => Math.Min(i, (float)j));
                    throw new LispEvalException("not a number", first);
                });

                Def("random", 1, a => {
                    var d = (double)a[0];
                    return d % 1 > 0
                        ? new Random().NextDouble() * d
                        : new Random().Next(0, (int)d);
                });
                Def("zerop", 1, a => DynamicDouble.Instance.Convert(a[0]) == 0d ? TRUE : null);

                void print(Interpreter I, string s)
                {
                    if (I.Scope != null)
                        I.Scope.Value.Context.DefaultMethods.write(I.Scope.Value, s);
                    else
                        COut.Write(s);
                }
                Def("print", -1, (I, a) => {
                    var c = (Cell) a[0];
                    foreach (var x in c)
                    {
                        print(I, Str(x, false)); 
                    }
                    return a.lastArg();
                });
                Def("println", -1, (I, a) => {
                    var c = (Cell) a[0];
                    foreach (var x in c)
                    {
                        if (I.Scope != null)
                            I.Scope.Value.Context.DefaultMethods.write(I.Scope.Value, Str(x, false));
                        else
                            COut.WriteLine(Str(a[0], true));
                    }
                    print(I, "\n"); 
                    return a.lastArg();
                });
                
                // html encoded versions
                Def("pr", -1, (I, a) => {
                    var c = (Cell) a[0];
                    var defaultScripts = I.AssertScope().Context.DefaultMethods;
                    foreach (var x in c)
                    {
                        print(I, defaultScripts.htmlEncode(Str(x, false)));
                    }
                    return a.lastArg();
                });
                Def("prn", -1, (I, a) => {
                    var c = (Cell) a[0];
                    var defaultScripts = I.AssertScope().Context.DefaultMethods;
                    foreach (var x in c)
                    {
                        defaultScripts.write(I.AssertScope(), defaultScripts.htmlEncode(Str(x, false)));
                    }
                    print(I, "\n"); 
                    return a.lastArg();
                });
                Def("dump", -1, (I, a) => {
                    var c = (Cell) a[0];
                    var defaultScripts = I.AssertScope().Context.DefaultMethods;
                    foreach (var x in c)
                    {
                        defaultScripts.write(I.AssertScope(), defaultScripts.dump(x).ToRawString());
                    }
                    print(I, "\n"); 
                    return a.lastArg();
                });
                Def("dump-inline", -1, (I, a) => {
                    var c = (Cell) a[0];
                    var defaultScripts = I.AssertScope().Context.DefaultMethods;
                    foreach (var x in c)
                    {
                        defaultScripts.write(I.AssertScope(), defaultScripts.jsv(x).ToRawString());
                    }
                    print(I, "\n"); 
                    return a.lastArg();
                });

                Def("debug", 0, a =>
                    Globals.Keys.Aggregate((Cell) null, (x, y) => new Cell(y, x)));
                
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
                        double x = DynamicDouble.Instance.Convert(Globals[gensymCounterSym]);
                        Globals[gensymCounterSym] = x + 1.0;
                        return new Sym($"G{(int) x}");
                    });

                Def("make-symbol", 1, a => new Sym((string) a[0]));
                Def("intern", 1, a => Sym.New((string) a[0]));
                Def("symbol-name", 1, a => ((Sym) a[0]).Name);

                Def("apply", 2, a =>
                    Eval(new Cell(a[0], MapCar((Cell) a[1], QqQuote)), null));

                Def("exit", 1, a => {
                        Environment.Exit(DynamicInt.Instance.Convert(a[0]));
                        return null;
                    });

                Globals[Sym.New("*version*")] =
                    new Cell(1.2d,
                             new Cell("C# 7", new Cell("Nukata Lisp Light", null)));
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
                                        return JsCallExpression.InvokeDelegate(fnDel, scriptMethod, isMemberExpr: false, scriptMethodArgs);
                                    });
                                }
                                if (symName[0] == ':')
                                {
                                    return (StaticMethodInvoker) (a => {
                                        var ret = scope.Context.DefaultMethods.get(a[0], symName.Substring(1));
                                        return ret;
                                    });
                                }
                                if (symName[0] == '.')
                                {
                                    return (StaticMethodInvoker) (a => {
                                        
                                        var ret = scope.Context.AssertProtectedMethods().call(a[0], symName.Substring(1).Replace('+',','), TypeConstants.EmptyObjectList);
                                        return ret;
                                    });
                                }
                                if (symName.IndexOf('/') >= 0)
                                {
                                    var fnNet = scope.Context.AssertProtectedMethods().Function(
                                        symName.Replace('/', '.').Replace('+',','));
                                    if (fnNet != null)
                                    {
                                        return (StaticMethodInvoker) (a => 
                                            JsCallExpression.InvokeDelegate(fnNet, null, isMemberExpr: false, new List<object>(a)));
                                    }
                                }
                                else if (symName[symName.Length - 1] == '.') // constructor (Type. arg) https://clojure.org/reference/java_interop#_the_dot_special_form
                                {
                                    var typeName = symName.Substring(0, symName.Length - 1);
                                    var fnCtor = scope.Context.AssertProtectedMethods().Constructor(typeName);
                                    if (fnCtor != null)
                                    {
                                        return (StaticMethodInvoker) (a => 
                                            JsCallExpression.InvokeDelegate(fnCtor, null, isMemberExpr: false, new List<object>(a)));
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
                                    return Compile(arg, env, Closure.Make);
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
                                            return ret;
                                        }
                                        if (fnName[0] == ':')
                                        {
                                            if (fnArgs.Length != 1)
                                                throw new NotSupportedException(":index access requires 1 instance target");
                                                
                                            var target = fnArgs[0];
                                            if (target == null)
                                                return null;
                                            var ret = scope.Context.DefaultMethods.get(target, fnName.Substring(1));
                                            return ret;
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
                                            return ret;
                                        }
                                        if (fnName.IndexOf('/') >= 0) // static method https://clojure.org/reference/java_interop#_member_access
                                        {
                                            var fnArgsList = new List<object>(fnArgs);
                                            var fnNet = scope.Context.AssertProtectedMethods().Function(
                                                fnName.Replace('/', '.').Replace('+',','), fnArgsList);
                                                
                                            if (fnNet != null)
                                            {
                                                var ret = JsCallExpression.InvokeDelegate(fnNet, null, isMemberExpr: false, fnArgsList);
                                                return ret;
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
                                    return ret;
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
                    throw ex;
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
                        Cell d = CdrCell(cell);
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

                    body.Walk(c => {
                        if (c.Car == PERCENT)
                            c.Car = ARG;
                        if (c.Cdr == PERCENT)
                            c.Cdr = ARG;
                    });
                    
                    // #(* 2 %) => (fn . ((_a . null) . ((* . (2 . (_a . null))) . null)))
                    
                    return new Cell(FN, new Cell(new Cell(ARG, null), new Cell(body, null)));
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
        /// <remarks>Exceptions are handled here and not thrown.</remarks>
        public static void RunREPL(Interpreter interp, TextReader input = null) {
            if (input == null)
                input = Console.In;

            var reader = new Reader(input.ReadToEnd().AsMemory());
            for (;;) {
                interp.COut.Write("> ");
                try {
                    var sExp = reader.Read();
                    if (sExp == Reader.EOF)
                    {
                        var inputBytes = input.ReadToEnd();
                        if (inputBytes.Length == 0)
                            return;
                        reader = new Reader(inputBytes.AsMemory());
                    }
                    var x = interp.Eval(sExp, null);
                    interp.COut.WriteLine(Str(x));
                } catch (Exception ex) {
                    interp.COut.WriteLine(ex);
                }
            }
        }

        static int MainREPL(string[] args=null) {
            var interp = CreateInterpreter();
            RunREPL(interp);
            interp.COut.WriteLine("Goodbye");
            return 0;
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
(defun nreverse (list)            ; (nreverse '(a b c d)) => (d c b a)
  (cond (list (_nreverse list nil))))

(defun last (list)
  (if (atom (cdr list))
      list
    (last (cdr list))))

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
    (list 'progn (list 'setq k v) ))

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

(defun reduce (f L &rest IV)
(let ((acc (cond (IV (first IV))
                 (t (f)) )))
    (if (end? L)
        acc
        (reduce f (cdr L)  (f acc (car L))) 
    )))

(defun zip (f L1 L2)
  (let ( (to) ) 
    (dolist (a L1) 
      (dolist (b L2)
        (push (f a b) to)))
    (nreverse to)
  ))

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
    (dolist (e L to)
      (push e to))))

(defun flatten (L)
  (mapcan
     (fn (a)
         (cond
           ((atom a) (list a))
           (t (flatten a))))
     L))

(defun elt (L n)
    (if (>= n (length L)) (error ""index out of range""))
    (let ((l L))
        (dotimes (i n)
            (setq l (rest l))
        )
    (first l)))

(defun range (n)
    (let ( (to '()) )  
        (dotimes (i n)
            (push-end i to))
        to))

(defun set-difference (L1 L2)
  (if L2
        (let ((res nil))
          (dolist (e L1)
            (unless (member e L2)
              (push e res)))
          res)
      L1))

(defun union (L1 L2)
  (if L2
        (let ((res nil))
          (dolist (e L1)
            (unless (member e res)
              (push e res)))
          (dolist (e L2)
            (unless (member e res)
              (push e res)))
          res)
      L1))
";

        /// <summary>
        /// Popular Clojure + nicer UX Utils
        /// </summary>
        public const string Extensions = @"

(defmacro dolist-while (spec f &rest body) ; (dolist-while (name list pred [result]) body...)
  (let ((name (car spec))
        (list (gensym)))
    `(let (,name
           (,list ,(cadr spec)))
       (while (and ,list (,f (car ,list)))
         (setq ,name (car ,list))
         ,@body
         (setq ,list (cdr ,list)))
       ,@(if (cddr spec)
             `((setq ,name nil)
               ,(caddr spec))))))

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

(defmacro incf+ (elem)
  `(setq ,elem (+ 1 ,elem)) `,elem)

(defun skip-while (f L)
  (let ( (to) (go) ) 
    (dolist (e L)
      (if (and (not go) (not (f e))) (setq go t)) 
      (if go (push e to))
    )
    (nreverse to)
  ))

(defun take-while (f L)
  (let ( (to) ) 
    (dolist-while (e L) #(f %)
      (push e to))
    (nreverse to)
  ))

(defun assoc-key (k L) (first (assoc k L)))
(defun assoc-value (k L) (second (assoc k L)))

(defun even? (n) (= (% n 2) 0))
(defun odd? (n) (= (% n 2) 1))

(defun flatmap (f L)
  (/flatten (map f L)))

(defun map-index (f L)
  (let ( (i -1) )
    (map (fn(x) (f x (incf i) )) L) ))

(defun filter-index (f L)
  (let ( (i -1) )
    (filter (fn (x) (f x (incf i) )) L) ))

(setq
    first  car
    1st    car
    second cadr
    2nd    cadr
    third  caddr
    3rd    caddr
    next   cdr
    rest   cdr
    skip2  cddr
    inc    1+
    dec    1-

    atom?  atom
    cons?  consp
    list?  listp
    end?   endp
    zero?  zerop
    all    every
    any    some
    lower-case string-downcase 
    upper-case string-upcase

    ; clojure
    defn   defun
    filter remove-if
)
";

    }
}
