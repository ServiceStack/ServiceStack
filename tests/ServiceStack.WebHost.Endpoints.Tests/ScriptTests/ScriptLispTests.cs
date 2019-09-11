using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ScriptLispTests
    {
        [Test]
        public void Can_eval_fib_lisp()
        {
            var lisp = @"
(defun fib (n)
  (if (< n 2)
      1
    (+ (fib (- n 1))
       (fib (- n 2)) )
  ))
";

            try
            {
                var lispCtx = Lisp.CreateInterpreter();

                var sExpressions = Lisp.Parse(lisp);
                var x = lispCtx.Eval(sExpressions);
                $"{x}".Print();

                sExpressions = Lisp.Parse("(fib 15)");
                x = lispCtx.Eval(sExpressions);
                
                $"{x}".Print();
                Assert.That((int)x, Is.EqualTo(987));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Test]
        public void Can_eval_lisp_in_lisp()
        {
            var lisp = @"
;;; A circular Lisp interpreter in Common/Emacs/Nukata Lisp
;;;    by SUZUKI Hisao on H28.8/10, H29.3/13
;;;    cf. Zick Standard Lisp (https://github.com/zick/ZickStandardLisp)

(progn
  ;; Expr: (EXPR environment (symbol...) expression...)
  ;; Subr: (SUBR . function)
  ;; Environment: ((symbol . value)...)
  ;; N.B. Expr has its own environment since this Lisp is lexically scoped.

  ;; Language-specific Hacks
  (setq funcall (lambda (f x) (f x)))  ; for Nukata Lisp and this Lisp
  (setq max-lisp-eval-depth 10000)     ; for Emacs Lisp
  (setq max-specpdl-size 7000)         ; for Emacs Lisp

  ;; The global environment of this Lisp
  (setq global-env
        (list '(*version* . (1.2 ""Lisp"" ""circlisp""))
              (cons 'car
                    (cons 'SUBR (lambda (x) (car (car x)))))
              (cons 'cdr
                    (cons 'SUBR (lambda (x) (cdr (car x)))))
              (cons 'cons
                    (cons 'SUBR (lambda (x) (cons (car x) (cadr% x)))))
              (cons 'eq
                    (cons 'SUBR (lambda (x) (eq (car x) (cadr% x)))))
              (cons 'atom
                    (cons 'SUBR (lambda (x) (atom (car x)))))
              (cons 'rplaca
                    (cons 'SUBR (lambda (x) (rplaca (car x) (cadr% x)))))
              (cons 'rplacd
                    (cons 'SUBR (lambda (x) (rplacd (car x) (cadr% x)))))
              (cons 'list
                    (cons 'SUBR (lambda (x) x)))
              (cons '+
                    (cons 'SUBR (lambda (x) (+ (car x) (cadr% x)))))
              (cons '*
                    (cons 'SUBR (lambda (x) (* (car x) (cadr% x)))))
              (cons '-
                    (cons 'SUBR (lambda (x) (- (car x) (cadr% x)))))
              (cons 'truncate
                    (cons 'SUBR (lambda (x) (truncate (car x) (cadr% x)))))
              (cons 'mod
                    (cons 'SUBR (lambda (x) (mod (car x) (cadr% x)))))
              (cons '=
                    (cons 'SUBR (lambda (x) (= (car x) (cadr% x)))))
              (cons '<
                    (cons 'SUBR (lambda (x) (< (car x) (cadr% x)))))
              (cons 'print
                    (cons 'SUBR (lambda (x) (print (car x)))))
              (cons 'apply
                    (cons 'SUBR (lambda (x) (apply% (car x) (cadr% x)))))
              (cons 'eval
                    (cons 'SUBR (lambda (x) (eval% (car x) global-env))))))

  (defun caar% (x) (car (car x)))
  (defun cadr% (x) (car (cdr x)))
  (defun cddr% (x) (cdr (cdr x)))
  (defun caddr% (x) (car (cdr (cdr x))))
  (defun cdddr% (x) (cdr (cdr (cdr x))))
  (defun cadddr% (x) (car (cdr (cdr (cdr x)))))

  (defun assq% (key alist)              ; cf. Emacs/Nukata Lisp
    (if alist
        (if (eq key (caar% alist))
            (car alist)
          (assq% key (cdr alist)))
      nil))

  (defun pairlis% (keys data alist)     ; cf. Common Lisp
    (if keys
        (cons (cons (car keys) (car data))
              (pairlis% (cdr keys) (cdr data) alist))
      alist))

  ;; Define symbol as value in the global environment.
  (defun global-def (sym val)
    (rplacd global-env
            (cons (car global-env)
                  (cdr global-env)))
    (rplaca global-env
            (cons sym val)))

  (defun eval% (e env)
    (if (atom e)
        ((lambda (var)
           (if var
               (cdr var)
             e))
         (assq% e env))
      (if (eq (car e) 'quote)           ; (quote e)
          (cadr% e)
        (if (eq (car e) 'if)            ; (if e e e)
            (if (eval% (cadr% e) env)
                (eval% (caddr% e) env)
              (eval% (cadddr% e) env))
          (if (eq (car e) 'progn)       ; (progn e...)
              (eval-progn (cdr e) env nil)
            (if (eq (car e) 'lambda)    ; (lambda (v...) e...)
                (make-closure env (cdr e))
              (if (eq (car e) 'defun)   ; (defun f (v...) e...)
                  (global-def (cadr% e)
                              (make-closure env (cddr% e)))
                (if (eq (car e) 'setq)  ; (setq v e)
                    ((lambda (var value)
                       (if var
                           (rplacd var value)
                         (global-def (cadr% e) value))
                       value)
                     (assq% (cadr% e) env)
                     (eval% (caddr% e) env))
                  (apply% (eval% (car e) env) ; (f e...)
                          (evlis (cdr e) env))))))))))

  ;; (make-closure env '((v...) e...)) => (EXPR env (v...) e...)
  (defun make-closure (env ve)
    (cons 'EXPR
          (cons env ve)))

  ;; (eval-progn '((+ 1 2) 3 (+ 4 5)) global-env nil) => 9
  (defun eval-progn (x env result)
    (if x
        (if (cdr x)
            (eval-progn (cdr x)
                        env
                        (eval% (car x) env))
          (eval% (car x) env))
      result))

  ;; (evlis '((+ 1 2) 3 (+ 4 5)) global-env) => (3 3 9)
  (defun evlis (x env)
    (if x
        (cons (eval% (car x) env)
              (evlis (cdr x) env))
      nil))

  (defun apply% (fun arg)
    (if (eq (car fun) 'EXPR)            ; (EXPR env (v...) e...)
        (eval-progn (cdddr% fun)
                    (pairlis% (caddr% fun)
                              arg
                              (cadr% fun))
                    nil)
      (if (eq (car fun) 'SUBR)          ; (SUBR . f)
          (funcall (cdr fun) arg)
        fun)))

  (defun global-eval (e)
    (eval% e global-env))

  (global-eval (quote

;; -- WRITE YOUR EXPRESSION HERE --
(progn
  (defun fib (n)
    (if (< n 2)
        1
      (+ (fib (- n 1))
         (fib (- n 2)))))
  (print (fib 10)))
;; --------------------------------
)))
";
            
            try
            {
                var lispCtx = Lisp.CreateInterpreter();

                var sExpressions = Lisp.Parse(lisp);
                var x = lispCtx.Eval(sExpressions);
                $"{x}".Print();
                Assert.That((int)x, Is.EqualTo(89));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Test]
        public void Can_min_max_int_long_double_values()
        {
            var lispCtx = Lisp.CreateInterpreter();

            Assert.That((int)lispCtx.Eval(Lisp.Parse("(min 1 2)")), Is.EqualTo(1));
            Assert.That((int)lispCtx.Eval(Lisp.Parse("(max 1 2)")), Is.EqualTo(2));

            Assert.That((double)lispCtx.Eval(Lisp.Parse("(min 1.0 2.0)")), Is.EqualTo(1.0));
            Assert.That((double)lispCtx.Eval(Lisp.Parse("(max 1.0 2.0)")), Is.EqualTo(2.0));

            Assert.That((long)lispCtx.Eval(Lisp.Parse($"(min {int.MaxValue + 1L} {int.MaxValue + 2L})")), Is.EqualTo(int.MaxValue + 1L));
            Assert.That((long)lispCtx.Eval(Lisp.Parse($"(max {int.MaxValue + 1L} {int.MaxValue + 2L})")), Is.EqualTo(int.MaxValue + 2L));
        }

        private static ScriptContext LispScriptContext(Dictionary<string, object> args = null)
        {
            var context = new ScriptContext {
                ScriptLanguages = {ScriptLisp.Language}
            }.Init();
            args?.Each((k,v) => context.Args[k] = v);
            return context;
        }

        [Test]
        public void Can_eval_lisp_in_ScriptPage()
        {
            var context = LispScriptContext();

            var script = @"
BEGIN LISP

```lisp
(defun fib (n)
  (if (< n 2)
      1
    (+ (fib (- n 1))
       (fib (- n 2)) )
  ))

(fib 15)
```

AFTER LISP
";

            var output = context.EvaluateScript(script);
            var expected = @"
BEGIN LISP

987

AFTER LISP".NormalizeNewLines();
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(expected));

            // Can run twice with identical results
            output = context.EvaluateScript(script);
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(expected));
        }

        [Test]
        public void Can_get_and_export_script_value()
        {
            var context = LispScriptContext(new ObjectDictionary { ["contextArg"] = 1 });
            
            var output = context.EvaluateScript(@"
{{ 2 | assignToGlobal => pageResultArg }} 
{{ 3 | to => scopeArg }}
```lisp
(+ contextArg pageResultArg scopeArg)
```");
            Assert.That(output.NormalizeNewLines(), Is.EqualTo("6"));
            
            output = context.EvaluateScript(@"
{{ 2 | assignToGlobal => pageResultArg }} 
{{ 3 | to => scopeArg }}
```lisp
(export retVal (+ contextArg pageResultArg scopeArg) 
        newVal 2)
```
Global: {{ retVal + newVal }}
");
            Assert.That(output.NormalizeNewLines(), Is.EqualTo("Global: 8"));
        }

        [Test]
        public void Does_support_silent_blocks()
        {
            var context = LispScriptContext(new ObjectDictionary { ["contextArg"] = 1 });
            
            var output = context.EvaluateScript(@"
{{ 2 | assignToGlobal => pageResultArg }} 
{{ 3 | to => scopeArg }}
```lisp
(setq retVal (+ contextArg pageResultArg scopeArg))
(setq newVal 2)
(export retVal retVal 
        newVal newVal)
```
Global: {{ retVal + newVal }}
");
            Assert.That(output.NormalizeNewLines(), Is.EqualTo("6\n2\nGlobal: 8"));
            
            output = context.EvaluateScript(@"
{{ 2 | assignToGlobal => pageResultArg }} 
{{ 3 | to => scopeArg }}
```lisp|quiet
(setq retVal (+ contextArg pageResultArg scopeArg))
(setq newVal 2)
(export retVal retVal 
        newVal newVal)
```
Global: {{ retVal + newVal }}
");
            Assert.That(output.NormalizeNewLines(), Is.EqualTo("Global: 8"));
        }

        [Test]
        public void Can_convert_IEnumerable_to_and_from_cons()
        {
            var context = LispScriptContext(new ObjectDictionary {
                ["numArray"] = new[] { 1, 2, 3 },
                ["numList"] = new[] { 1, 2, 3 }.ToList(),
                ["numSet"] = new[] { 1, 2, 3 }.ToHashSet(),
            });
            
            string render(string lisp) => context.EvaluateScript(lisp).NormalizeNewLines();
            
            Assert.That(render(@"
```lisp
(export array1 (map 1+ '(1 2 3)) )
```
SUM: {{ array1.sum() }}
"), Is.EqualTo("SUM: 9"));
            
            Assert.That(render(@"
```lisp
(export array1 (map 1+ '(1 2 3)) )
```
SUM: {{ array1.toList().sum() }}
"), Is.EqualTo("SUM: 9"));
            
            Assert.That(render(@"
```lisp
(export 
    sum (reduce + (mapcar 1+ (to-cons numArray))) 
)
```
SUM: {{ sum }}
"), Is.EqualTo("SUM: 9"));
            
        }
        
        private static ScriptContext LispNetContext(Dictionary<string, object> args = null)
        {
            var context = new ScriptContext {
                ScriptLanguages = {
                    ScriptLisp.Language
                },
                ScriptMethods = {
                    new ProtectedScripts(),
                },
                AllowScriptingOfAllTypes = true,
                ScriptNamespaces = {
                    "System",
                    typeof(StaticLog).Namespace,
                },
                ScriptTypes = {
                    typeof(DynamicInt),
                }
            };
            args?.Each((k,v) => context.Args[k] = v);
            return context.Init();
        }
        
        [Test]
        public void Can_call_delegates_LISP()
        {
            var context = LispNetContext(new ObjectDictionary {
                ["stringNums"] = "1 2 3",
                ["strings"] = new List<string> { " A ", " B ", " C " },
                ["argIncr"] = (Func<int,int>)(x => x + 1),
            });

            string render(string lisp) => context.RenderLisp(lisp).NormalizeNewLines();

            Assert.That(render(@"(/join (map 1+ '(1 2 3)))"), Is.EqualTo("2,3,4"));
            Assert.That(render(@"(/join (map 1+ '(1 2 3)) "", "")"), Is.EqualTo("2, 3, 4"));
            Assert.That(render(@"(String/Join "","" (to-array (map 1+ '(1 2 3))))"), Is.EqualTo("2,3,4"));
            Assert.That(render(@"((/F ""String.Join(string,object[])"") "","" (to-array (map 1+ '(1 2 3))))"), Is.EqualTo("2,3,4"));
            Assert.That(render(@"(F ""String.Join(string,object[])"" "","" (map 1+ '(1 2 3)))"), Is.EqualTo("2,3,4"));
            Assert.That(render(@"(F ""String.Join"" "","" (to-array (map 1+ '(1 2 3))))"), Is.EqualTo("2,3,4"));
            
            Assert.That(render(@"(/sum (map 1+ '(1 2 3)))"), Is.EqualTo("9"));
            Assert.That(render(@"(/sum (map argIncr '(1 2 3)))"), Is.EqualTo("9"));
            Assert.That(render(@"(/sum (map /incr '(1 2 3)))"), Is.EqualTo("9"));

            Assert.That(render(@"(/sum (map Math/Sqrt '(1 4 9)))"), Is.EqualTo("6"));
            Assert.That(render(@"(/sum (map (/F ""Math.Sqrt"") '(1 4 9)))"), Is.EqualTo("6"));

            Assert.That(render(@"(/concat (map .Trim strings))"), Is.EqualTo("ABC"));
            Assert.That(render(@"(/join (map .Trim strings))"), Is.EqualTo("A,B,C"));
            Assert.That(render(@"(.Replace stringNums "" "" "", "")"), Is.EqualTo("1, 2, 3"));
            Assert.That(render(@"(.Name (/typeof ""int""))"), Is.EqualTo("Int32"));
            
            Assert.That(context.RenderScript(@"
{{#function templateIncr(i) }}
    i | incr | return
{{/function}}

```lisp
(/sum (map templateIncr '(1 2 3)))
```
").NormalizeNewLines(), Is.EqualTo("9"));

            Assert.That(context.RenderScript(@"
```code
#function codeIncr(i)
    return (i.incr())
/function
```

```lisp
(/sum (map codeIncr '(1 2 3)))
```
").NormalizeNewLines(), Is.EqualTo("9"));

            Assert.That(context.RenderScript(@"
A

```code
#function codeIncr(i)
    return (i.incr())
/function
```

B

```lisp
(/sum (map codeIncr '(1 2 3)))
```

C
").NormalizeNewLines(), Is.EqualTo("A\n\n\nB\n\n9\n\nC"));
        }

        [Test]
        public void Can_create_Function_for_static_Methods_LISP()
        {
            var context = LispNetContext(new ObjectDictionary {
                ["msg"] = "msg string"
            });

            string eval(string lisp) => context.EvaluateLisp<string>(lisp);

            context.RenderLisp(@"(setq writeln (/F ""Console.WriteLine(string)"")) (writeln msg)");
            
            Assert.That(eval(@"(StaticLog/Clear) (StaticLog/Log msg) (return (StaticLog/AllLogs))"), 
                Is.EqualTo("msg string"));
            Assert.That(eval(@"(StaticLog/Clear) (StaticLog/Log<int> msg) (return (StaticLog/AllLogs))"), 
                Is.EqualTo("Int32 msg string"));
            Assert.That(eval(@"(StaticLog/Clear) ((/F ""StaticLog.Log"") msg) (return (StaticLog/AllLogs))"), 
                Is.EqualTo("msg string"));
        }

        [Test]
        public void Can_create_Function_for_generic_type_static_Methods_LISP()
        {
            var context = LispNetContext(new ObjectDictionary {
                ["msg"] = "msg string"
            });

            string eval(string lisp) => context.EvaluateLisp<string>(lisp);
            
            Assert.That(eval(@"(GenericStaticLog<string>/Clear) (setq log (/F ""GenericStaticLog<string>.Log(string)"")) (log msg) (return (GenericStaticLog<string>/AllLogs))"), 
                Is.EqualTo("String msg string"));
            Assert.That(eval(@"(GenericStaticLog<string>/Clear) (setq log (/F ""GenericStaticLog<string>.Log<int>(string)"")) (log msg) (return (GenericStaticLog<string>/AllLogs))"), 
                Is.EqualTo("String Int32 msg string"));
        }


        [Test]
        public void Can_create_Function_for_instance_methods_LISP()
        {
            var context = LispNetContext(new ObjectDictionary {
                ["msg"] = "msg string"
            });

            string eval(string lisp) => context.EvaluateLisp<string>(lisp);

            Assert.That(eval(@"(setq o (InstanceLog. ""instance"")) (.Log o msg) (return (.AllLogs o))"), 
                Is.EqualTo("instance msg string"));
        }
        
        [Test]
        public void Can_create_Type_from_registered_Script_Assembly_LISP()
        {
            var context = LispNetContext();

            string render(string lisp) => context.RenderLisp(lisp).NormalizeNewLines();
            string eval(string lisp) => context.EvaluateLisp<string>(lisp);

            Assert.That(render("(.add (DynamicInt.) 1 2)"), Is.EqualTo("3"));
            Assert.That(render(@"(.add (new ""DynamicInt"") 1 2)"), Is.EqualTo("3"));
            Assert.That(render(@"(.add (new (/typeof ""DynamicInt"")) 1 2)"), Is.EqualTo("3"));
            Assert.That(render(@"(.add (C ""DynamicInt()"") 1 2)"), Is.EqualTo("3"));
            Assert.That(render(@"(.add ((/C ""DynamicInt()"")) 1 2)"), Is.EqualTo("3"));
            
            Assert.That(render("(.GetTotal (Ints. 1 2))"), Is.EqualTo("3"));

            Assert.That(render(@"(.ToString (new ""Adder"" ""A""))"), Is.EqualTo($"string: A"));
            Assert.That(render(@"(.ToString (C ""Adder(string)"" ""A""))"), Is.EqualTo($"string: A"));
            Assert.That(render(@"(.ToString ((/C ""Adder(string)"") ""A""))"), Is.EqualTo($"string: A"));

            Assert.That(eval("(setq ints (Ints. 1 2)) (.AddA ints 3) (.AddA ints 4) (return (.GetTotal ints))"), Is.EqualTo("10"));

            //Assert.That(render("(.GetTotal (doto (Ints. 1 2) (.C 3) (.D 4)) )"), Is.EqualTo("10"));
        }

        [Test]
        public void Can_call_inner_class_properties_LISP()
        {
            var context = LispNetContext(new ObjectDictionary {
                ["o"] = new StaticLog(),
                ["o1"] = new StaticLog.Inner1(),
            });

            string result = null;

            Assert.That(context.EvaluateLisp<string>("(return (StaticLog/Prop))"), Is.EqualTo("StaticLog.Prop"));
            Assert.That(context.EvaluateLisp<string>("(return (StaticLog/Field))"), Is.EqualTo("StaticLog.Field"));
            Assert.That(context.EvaluateLisp<string>("(return (StaticLog/Const))"), Is.EqualTo("StaticLog.Const"));
            
            Assert.That(context.EvaluateLisp<string>("(return (StaticLog.Inner1/Prop1))"), Is.EqualTo("StaticLog.Inner1.Prop1"));
            Assert.That(context.EvaluateLisp<string>("(return (StaticLog.Inner1/Field1))"), Is.EqualTo("StaticLog.Inner1.Field1"));
            Assert.That(context.EvaluateLisp<string>("(return (StaticLog.Inner1/Const1))"), Is.EqualTo("StaticLog.Inner1.Const1"));

            Assert.That(context.EvaluateLisp<string>("(return (StaticLog.Inner1.Inner2/Prop2))"), Is.EqualTo("StaticLog.Inner1.Inner2.Prop2"));
            Assert.That(context.EvaluateLisp<string>("(return (StaticLog.Inner1.Inner2/Field2))"), Is.EqualTo("StaticLog.Inner1.Inner2.Field2"));
            Assert.That(context.EvaluateLisp<string>("(return (StaticLog.Inner1.Inner2/Const2))"), Is.EqualTo("StaticLog.Inner1.Inner2.Const2"));

            Assert.That(context.EvaluateLisp<string>("(return (.InstanceProp o))"), Is.EqualTo("StaticLog.InstanceProp"));
            Assert.That(context.EvaluateLisp<string>("(return (.InstanceField o))"), Is.EqualTo("StaticLog.InstanceField"));

            Assert.That(context.EvaluateLisp<string>("(return (.InstanceProp1 o1))"), Is.EqualTo("StaticLog.Inner1.InstanceProp1"));
            Assert.That(context.EvaluateLisp<string>("(return (.InstanceField1 o1))"), Is.EqualTo("StaticLog.Inner1.InstanceField1"));
        }

        [Test]
        public void Can_map_on_IEnumerables()
        {
            var context = LispScriptContext(new ObjectDictionary {
                ["numArray"] = new[] { 1, 2, 3 },
                ["numList"] = new[] { 1, 2, 3 }.ToList(),
                ["numSet"] = new[] { 1, 2, 3 }.ToHashSet(),
            });

            string render(string lisp) => context.RenderLisp(lisp).NormalizeNewLines();
           
            Assert.That(render(@"(reduce + (mapcar 1+ '(1 2 3)))"), Is.EqualTo("9"));

            // sum can do mapcar + all num's
            Assert.That(render(@"(sum (mapcar 1+ '(1 2 3)))"), Is.EqualTo("9"));
            Assert.That(render(@"(sum (mapcar 1+ '(1.1 2 3)))"), Is.EqualTo("9.1"));
            Assert.That(render(@"(sum (mapcar 1+ '(1 2 3.1)))"), Is.EqualTo("9.1"));
            
            Assert.That(render(@"(sum (map 1+ '(1 2 3)))"), Is.EqualTo("9"));
            Assert.That(render(@"(sum (map 1+ numArray))"), Is.EqualTo("9"));
            Assert.That(render(@"(sum (map 1+ numList))"), Is.EqualTo("9"));
            Assert.That(render(@"(sum (map 1+ numSet))"), Is.EqualTo("9"));

            Assert.That(render(@"(defmacro 2+ (n) `(+ ,n 2)) (sum (map 2+ '(1 2 3)))"), Is.EqualTo("12"));
        }
 
        [Test]
        public void Does_print_to_Output_Stream()
        {
            var context = LispScriptContext();
            
            Assert.That(context.RenderLisp(@"10 (let () (print ""A"")(princ '(1 2 3)) (terpri) nil) 20").NormalizeNewLines(), 
                Is.EqualTo("10\n\"A\"\n(1 2 3)\n20"));
            
            Assert.That(context.RenderLisp(@"10 (/write ""A"") 20").Replace("\r",""), 
                Is.EqualTo("10\nA20\n"));
        }

        [Test]
        public void Does_limit_max_iterations()
        {
            var context = LispScriptContext();

            // Context.MaxIterations = 1000000 but LISP Eval can be called 10x+ for evaluating 1 op
            context.RenderLisp(@"(dotimes (i 90000) (print i))");

            // Count resets per LISP Statement Block
            context.EvaluateScript(@"
```lisp
(dotimes (i 90000) (print i))
```

```lisp
(dotimes (i 90000) (print i))
```
");

            try
            {
                context.RenderLisp("(dotimes (i 100001) (print i))");
                Assert.Fail("Should Throw");
            }
            catch (ScriptException e)
            {
                Console.WriteLine(e);
                Assert.That(e.InnerException is NotSupportedException);
            }

            try
            {
                context.EvaluateScript(@"
                    ```lisp
                    (dotimes (i 100001) (print i))
                    ```
                    ");
                Assert.Fail("Should Throw");
            }
            catch (ScriptException e)
            {
                Console.WriteLine(e);
                Assert.That(e.InnerException is NotSupportedException);
            }

            try
            {
                context.RenderLisp(@"
                    (dotimes (i 10000) (print i))
                    (dotimes (i 10000) (print i))
                    (dotimes (i 10000) (print i))
                    (dotimes (i 10000) (print i))
                    (dotimes (i 10000) (print i))
                    (dotimes (i 10000) (print i))
                    (dotimes (i 10000) (print i))
                    (dotimes (i 10000) (print i))
                    (dotimes (i 10000) (print i))
                    (dotimes (i 10000) (print i))
                    (dotimes (i 10000) (print i))
                    (dotimes (i 1) (print i))
                ");
                Assert.Fail("Should Throw");
            }
            catch (ScriptException e)
            {
                Console.WriteLine(e);
                Assert.That(e.InnerException is NotSupportedException);
            }
        }
    }
    
}