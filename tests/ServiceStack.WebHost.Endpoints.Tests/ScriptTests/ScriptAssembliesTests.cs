using System;
using System.Collections.Generic;
using System.Text;
using Funq;
using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class IntTuple
    {
        public IntTuple(int a, int b)
        {
            A = a;
            B = b;
        }
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
        public int D { get; set; }
        public int GetTotal() => A + B + C + D;

        public string GenericMethod<T>() => typeof(T).Name + " " + GetTotal();

        public string GenericMethod<T>(T value) => typeof(T).Name + $" {value} " + GetTotal();
    }

    public class StaticLog
    {
        static StringBuilder sb = new StringBuilder();

        public static void Log(string message) => sb.Append(message);

        public static string AllLogs() => sb.ToString();

        public static void Clear() => sb.Clear();
    }

    public class GenericStaticLog<T>
    {
        static StringBuilder sb = new StringBuilder();

        public static void Log(string message) => sb.Append(typeof(T).Name + " " + message);

        public static void Log<T2>(string message) => sb.Append(typeof(T).Name + " " + typeof(T2).Name + " " + message);

        public static string AllLogs() => sb.ToString();

        public static void Clear() => sb.Clear();
    }

    public class InstanceLog
    {
        private readonly string prefix;
        public InstanceLog(string prefix) => this.prefix = prefix;

        StringBuilder sb = new StringBuilder();

        public void Log(string message) => sb.Append(prefix + " " + message);

        public void Log<T2>(string message) => sb.Append(prefix + " " + typeof(T2).Name + " " + message);

        public string AllLogs() => sb.ToString();

        public void Clear() => sb.Clear();
    }
    
    public class ScriptAssembliesTests
    {
        private static ScriptContext CreateContext() =>
            new ScriptContext {
                ScriptMethods = {
                    new ProtectedScripts()
                },
                ScriptAssemblies = {
                    typeof(DynamicInt).Assembly,
                }
            };

        private static ScriptContext CreateContext(Action<ScriptContext> fn)
        {
            var context = CreateContext();
            fn(context);
            return context;
        }

        [Test]
        public void Does_not_allow_Types_by_default()
        {
            Assert.Throws<ScriptException>(() =>
                new ScriptContext().Init().EvaluateScript("{{ 'int'.typeof().Name }}"));

            var context = CreateContext().Init();
            var result = context.EvaluateScript("{{ 'DynamicInt'.typeof().Name }}");
            
            Assert.That(result, Is.EqualTo(nameof(DynamicInt)));
            
            result = context.EvaluateScript("{{ 'ServiceStack.WebHost.Endpoints.Tests.ScriptTests.IntTuple'.typeof().Name }}");
            Assert.That(result, Is.Empty);
            
            context = CreateContext(c => c.AllowScriptingOfAllTypes = true).Init();
            result = context.EvaluateScript("{{ 'ServiceStack.WebHost.Endpoints.Tests.ScriptTests.IntTuple'.typeof().Name }}");
            Assert.That(result, Is.EqualTo(nameof(IntTuple)));
        }

        [Test]
        public void Can_create_Type_from_registered_Script_Assembly()
        {
            var context = CreateContext().Init();

            var result = context.EvaluateScript(
                @"{{ 'DynamicInt'.new() | to => d }}{{ d.call('add', [1, 2]) }}");
            
            Assert.That(result, Is.EqualTo("3"));

            result = context.EvaluateScript(
                @"{{ 'DynamicInt'.new() | to => d }}{{ d.call('add', [3, 4]) }}");
            
            Assert.That(result, Is.EqualTo("7"));

            result = context.EvaluateScript(
                @"{{ 'ServiceStack.DynamicInt'.new() | to => d }}{{ d.call('add', [5, 6]) }}");
            
            Assert.That(result, Is.EqualTo("11"));
        }

        [Test]
        public void Can_call_generic_methods()
        {
            var context = CreateContext(c => c.ScriptAssemblies.Add(typeof(IntTuple).Assembly)).Init();

            var result = context.Evaluate<string>(
                "{{ 'IntTuple'.new([1,2]).call('GenericMethod<string>') | return }}");
            
            Assert.That(result, Is.EqualTo("String 3"));

            result = context.Evaluate<string>(
                "{{ 'IntTuple'.new([1,2]).call('GenericMethod<string>',['arg']) | return }}");
            
            Assert.That(result, Is.EqualTo("String arg 3"));
        }

        [Test]
        public void Can_create_type_with_constructor_arguments()
        {
            var context = CreateContext(c => c.ScriptAssemblies.Add(typeof(IntTuple).Assembly)).Init();
            
            var result = context.Evaluate<int>(
                "{{ 'IntTuple'.new([1,2]).call('GetTotal') | return }}");
            
            Assert.That(result, Is.EqualTo(3));
            
            result = context.Evaluate<int>(
                "{{ 'IntTuple'.new([1,2]).set({ C:3, D:4 }).call('GetTotal') | return }}");
            
            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public void Can_create_generic_type_with_constructor_arguments()
        {
            var context = CreateContext(c => {
                c.AllowScriptingOfAllTypes = true;
                c.ScriptNamespaces.Add(typeof(KeyValuePair<,>).Namespace);
            }).Init();

            var result = context.EvaluateScript(
                "{{ 'KeyValuePair<string,int>'.new(['A',1]) | to => kvp }}{{ kvp.Key }}={{ kvp.Value }}");
            
            Assert.That(result, Is.EqualTo("A=1"));
        }

        [Test]
        public void Can_create_Type_from_Loaded_Assembly()
        {
            var context = CreateContext(c => c.AllowScriptingOfAllTypes = true).Init();

            var result = context.Evaluate(
                "{{ 'ServiceStack.WebHost.Endpoints.Tests.ScriptTests.IntTuple'.new() | return}}");
            Assert.That(result as IntTuple, Is.Not.Null);
        }

        [Test]
        public void Can_create_Function_for_static_Methods()
        {
            var context = CreateContext(c => {
                c.AllowScriptingOfAllTypes = true;
                c.ScriptNamespaces.Add(typeof(Console).Namespace);
                c.ScriptNamespaces.Add(typeof(StaticLog).Namespace);
            }).Init();

            string result = null;
            
            result = context.EvaluateScript(@"{{ Function('Console.WriteLine(string)') | to => writeln }}
                {{ writeln('static method') }}
                {{ 'ext method'.writeln() }}");

            StaticLog.Clear();
            
            result = context.Evaluate<string>(@"{{ Function('StaticLog.Log') | to => log }}
                {{ log('arg.') }}
                {{ 'ext.'.log() }}
                {{ Function('StaticLog.AllLogs') | to => allLogs }}{{ allLogs() | return }}");
            Assert.That(result, Is.EqualTo("arg.ext."));

            StaticLog.Clear();
            
            result = context.Evaluate<string>(@"{{ Function('StaticLog.Log')('iife') }}
                {{ Function('StaticLog.AllLogs')() | return }}");
            Assert.That(result, Is.EqualTo("iife"));
        }

        [Test]
        public void Can_create_Function_for_generic_type_static_Methods()
        {
            var context = CreateContext(c => {
                c.AllowScriptingOfAllTypes = true;
                c.ScriptNamespaces.Add(typeof(GenericStaticLog<>).Namespace);
            }).Init();

            string result = null;
            
            GenericStaticLog<string>.Clear();
            
            result = context.Evaluate<string>(@"{{ Function('GenericStaticLog<string>.Log(string)') | to => log }}
                {{ log('arg.') }}
                {{ 'ext.'.log() }}
                {{ Function('GenericStaticLog<string>.AllLogs') | to => allLogs }}{{ allLogs() | return }}");
            Assert.That(result, Is.EqualTo("String arg.String ext."));

            GenericStaticLog<string>.Clear();
            
            result = context.Evaluate<string>(@"{{ Function('GenericStaticLog<string>.Log(string)')('iife') }}
                {{ Function('GenericStaticLog<string>.AllLogs')() | return }}");
            Assert.That(result, Is.EqualTo("String iife"));
            
            GenericStaticLog<string>.Clear();
            
            result = context.Evaluate<string>(@"{{ Function('GenericStaticLog<string>.Log<int>(string)') | to => log }}
                {{ log('arg.') }}
                {{ 'ext.'.log() }}
                {{ Function('GenericStaticLog<string>.AllLogs') | to => allLogs }}{{ allLogs() | return }}");
            Assert.That(result, Is.EqualTo("String Int32 arg.String Int32 ext."));
        }

        [Test]
        public void Can_create_Function_for_instance_methods()
        {
            var context = CreateContext(c => {
                c.AllowScriptingOfAllTypes = true;
                c.ScriptNamespaces.Add(typeof(InstanceLog).Namespace);
            }).Init();

            string result = null;

            result = context.Evaluate<string>(@"{{ 'InstanceLog'.new(['instance']) | to => o }}
                {{ Function('InstanceLog.Log') | to => log }}                
                {{ o.log('arg.') }}
                {{ log(o,'param.') }}
                {{ Function('InstanceLog.AllLogs') | to => allLogs }}{{ o.allLogs() | return }}");
            Assert.That(result, Is.EqualTo("instance arg.instance param."));

            result = context.Evaluate<string>(@"{{ Function('InstanceLog.Log<int>') | to => log }}
                {{ 'InstanceLog'.new(['instance']) | to => o }}
                {{ o.log('arg.') }}
                {{ log(o,'param.') }}
                {{ Function('InstanceLog.AllLogs') | to => allLogs }}{{ o.allLogs() | return }}");
            Assert.That(result, Is.EqualTo("instance Int32 arg.instance Int32 param."));
          
        }
    }
}