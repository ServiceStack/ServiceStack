using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Funq;
using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class Ints
    {
        public Ints(int a, int b)
        {
            A = a;
            B = b;
        }
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
        public int D { get; set; }
        public int GetTotal() => A + B + C + D;

        public int AddA(int a) => A += a; 

        public string GenericMethod<T>() => typeof(T).Name + " " + GetTotal();

        public string GenericMethod<T>(T value) => typeof(T).Name + $" {value} " + GetTotal();
    }

    public class Adder
    {
        public string String { get; set; }
        public double Double { get; set; }

        public Adder(string str) => String = str;
        public Adder(double num) => Double = num;

        public string Add(string str) => String += str;
        public double Add(double num) => Double += num;
        
        public override string ToString() => String != null ? $"string: {String}" : $"double: {Double}";
    }

    public class StaticLog
    {
        static StringBuilder sb = new StringBuilder();

        public static void Log(string message) => sb.Append(message);

        public static void Log<T>(string message) => sb.Append(typeof(T).Name + " " + message);

        public static string AllLogs() => sb.ToString();

        public static void Clear() => sb.Clear();
        
        public static string Prop { get; } = "StaticLog.Prop";
        public static string Field = "StaticLog.Field";
        public const string Const = "StaticLog.Const";

        public string InstanceProp { get; } = "StaticLog.InstanceProp";
        public string InstanceField = "StaticLog.InstanceField";

        public class Inner1
        {
            public static string Prop1 { get; } = "StaticLog.Inner1.Prop1";
            public static string Field1 = "StaticLog.Inner1.Field1";
            public const string Const1 = "StaticLog.Inner1.Const1";

            public string InstanceProp1 { get; } = "StaticLog.Inner1.InstanceProp1";
            public string InstanceField1 = "StaticLog.Inner1.InstanceField1";

            public static class Inner2
            {
                public static string Prop2 { get; } = "StaticLog.Inner1.Inner2.Prop2";
                public static string Field2 = "StaticLog.Inner1.Inner2.Field2";
                public const string Const2 = "StaticLog.Inner1.Inner2.Const2";
            }
        }
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

    internal class InternalType
    {
        public InternalType() { }
        public InternalType(int num) {}
    }

    public interface ICursor
    {
        string AProp { get; set; }
        string AMethod(int arg);
    }
    public class Cursor : ICursor 
    {
        public string AProp { get; set; }
        public string BProp { get; set; }
        public string AMethod(int arg) => $"AMethod: {arg}";
        public string BMethod(int arg) => $"BMethod: {arg}";
    }

    public class ContentResolver
    {
        public ICursor Query(
            Uri uri,
            string[] projection,
            string selection,
            string[] selectionArgs,
            string sortOrder)
        {
            return new Cursor();
        }
    }
    
    public class ScriptAssembliesTests
    {
        private static ScriptContext CreateContext() =>
            new ScriptContext {
                ScriptMethods = {
                    new ProtectedScripts()
                },
                ScriptTypes = {
                    typeof(DynamicInt),
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
            
            result = context.EvaluateScript("{{ 'ServiceStack.WebHost.Endpoints.Tests.ScriptTests.Ints'.typeof().Name }}");
            Assert.That(result, Is.Empty);
            
            context = CreateContext(c => c.AllowScriptingOfAllTypes = true).Init();
            result = context.EvaluateScript("{{ 'ServiceStack.WebHost.Endpoints.Tests.ScriptTests.Ints'.typeof().Name }}");
            Assert.That(result, Is.EqualTo(nameof(Ints)));
        }

        [Test]
        public void Does_not_allow_creating_instances_of_public_types()
        {
            var context = CreateContext(c => {
                c.ScriptTypes.Add(typeof(InternalType));
            }).Init();

            try
            {
                context.Evaluate("{{ 'InternalType'.new() |> return }}");
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.That(e.InnerException.GetType(), Is.EqualTo(typeof(NotSupportedException)));
            }

            try
            {
                context.Evaluate("{{ 'InternalType'.new(1) |> return }}");
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.That(e.InnerException.GetType(), Is.EqualTo(typeof(NotSupportedException)));
            }
            
            context = CreateContext(c => {
                c.ScriptTypes.Add(typeof(InternalType));
                c.AllowScriptingOfAllTypes = true; 
            }).Init();
            
            var result = context.Evaluate("{{ 'InternalType'.new() |> return }}");
            Assert.That(result.GetType(), Is.EqualTo(typeof(InternalType)));

            result = context.Evaluate("{{ 'InternalType'.new(1) |> return }}");
            Assert.That(result.GetType(), Is.EqualTo(typeof(InternalType)));
        }

        [Test]
        public void typeof_returns_correct_types()
        {
            var context = CreateContext(c => {
                c.AllowScriptingOfAllTypes = true;
                c.ScriptNamespaces.Add("System");
                c.ScriptNamespaces.Add("System.Collections.Generic");
                c.ScriptNamespaces.Add(typeof(StaticLog).Namespace);
            }).Init();

            Assert.That(context.Evaluate<Type>("{{ typeof('int') |> return}}"), Is.EqualTo(typeof(int)));
            Assert.That(context.Evaluate<Type>("{{ typeof('Int32') |> return}}"), Is.EqualTo(typeof(Int32)));
            Assert.That(context.Evaluate<Type>("{{ typeof('List<>') |> return}}"), Is.EqualTo(typeof(List<>)));
            Assert.That(context.Evaluate<Type>("{{ typeof('Dictionary<,>') |> return}}"), Is.EqualTo(typeof(Dictionary<,>)));
            Assert.That(context.Evaluate<Type>("{{ typeof('List<string>') |> return}}"), Is.EqualTo(typeof(List<string>)));
            Assert.That(context.Evaluate<Type>("{{ typeof('Dictionary<string,int>') |> return}}"), Is.EqualTo(typeof(Dictionary<string,int>)));
            Assert.That(context.Evaluate<Type>("{{ typeof('Dictionary<String,Int32>') |> return}}"), Is.EqualTo(typeof(Dictionary<string,int>)));
            Assert.That(context.Evaluate<Type>("{{ typeof('Dictionary<String,ServiceStack.WebHost.Endpoints.Tests.ScriptTests.Ints>') |> return}}"), Is.EqualTo(typeof(Dictionary<string,Ints>)));
            
            Assert.That(context.Evaluate<Type>("{{ typeof('StaticLog+Inner1') |> return}}"), Is.EqualTo(typeof(StaticLog.Inner1)));
            Assert.That(context.Evaluate<Type>("{{ typeof('StaticLog+Inner1+Inner2') |> return}}"), Is.EqualTo(typeof(StaticLog.Inner1.Inner2)));
            Assert.That(context.Evaluate<Type>("{{ typeof('StaticLog.Inner1') |> return}}"), Is.EqualTo(typeof(StaticLog.Inner1)));
            Assert.That(context.Evaluate<Type>("{{ typeof('StaticLog.Inner1.Inner2') |> return}}"), Is.EqualTo(typeof(StaticLog.Inner1.Inner2)));

            Assert.That(context.Evaluate<Type>("{{ typeof('string[]') |> return}}"), Is.EqualTo(typeof(string[])));
            Assert.That(context.Evaluate<Type>("{{ typeof('List<string>[]') |> return}}"), Is.EqualTo(typeof(List<string>[])));
            Assert.That(context.Evaluate<Type>("{{ typeof('Dictionary<String, System.Int32>[]') |> return}}"), Is.EqualTo(typeof(Dictionary<String, System.Int32>[])));
        }

        [Test]
        public void Can_create_Type_from_registered_Script_Assembly()
        {
            var context = CreateContext().Init();

            var result = context.EvaluateScript(
                @"{{ 'DynamicInt'.new() |> to => d }}{{ d.call('add', [1, 2]) }}");
            Assert.That(result, Is.EqualTo("3"));

            result = context.EvaluateScript(
                @"{{ 'DynamicInt'.new() |> to => d }}{{ d.call('add', [3, 4]) }}");
            Assert.That(result, Is.EqualTo("7"));

            result = context.EvaluateScript(
                @"{{ 'ServiceStack.DynamicInt'.new() |> to => d }}{{ d.call('add', [5, 6]) }}");
            Assert.That(result, Is.EqualTo("11"));
            
            result = context.EvaluateScript(
                @"{{ typeof('DynamicInt').createInstance() |> to => d }}{{ d.call('add', [3, 4]) }}");
            Assert.That(result, Is.EqualTo("7"));
        }

        [Test]
        public void Cant_create_instances_from_ambiguous_constructors()
        {
            var context = CreateContext(c => c.ScriptTypes.Add(typeof(Adder))).Init();

            object result = null;

            try 
            { 
                result = context.Evaluate("{{ 'Adder'.new([1]) }}");
                Assert.Fail("Should throw");
            }
            catch (ScriptException e)
            {
                Assert.That(e.InnerException.GetType(), Is.EqualTo(typeof(NotSupportedException)));
            }
            
            result = context.Evaluate("{{ 'Adder'.new([1.toString()]) |> to => o }}{{ o.String |> return }}");
            Assert.That(result, Is.EqualTo("1"));
            
            result = context.Evaluate("{{ 'Adder'.new([1.toDouble()]) |> to => o }}{{ o.Double |> return }}");
            Assert.That(result, Is.EqualTo(1.0d));
            
            result = context.Evaluate("{{ Constructor('Adder(string)')(1) |> to => o }}{{ o.String |> return }}");
            Assert.That(result, Is.EqualTo("1"));
            
            result = context.Evaluate("{{ Constructor('Adder(double)')(1) |> to => o }}{{ o.Double |> return }}");
            Assert.That(result, Is.EqualTo(1.0d));
        }

        [Test]
        public void Can_call_ambiguous_methods()
        {
            var context = CreateContext(c => c.ScriptTypes.Add(typeof(Adder))).Init();

            object result = null;

            try 
            { 
                result = context.Evaluate("{{ 'Adder'.new(['1']) |> to => o }}{{ o.call('Add',[1]) }}{{ o.String |> return }}");
                Assert.Fail("Should throw");
            }
            catch (ScriptException e)
            {
                Assert.That(e.InnerException.GetType(), Is.EqualTo(typeof(NotSupportedException)));
            }
            
            result = context.Evaluate("{{ 'Adder'.new(['1']) |> to => o }}{{ o.call('Add',['1']) }}{{ o.String |> return }}");
            Assert.That(result, Is.EqualTo("11"));
            
            result = context.Evaluate("{{ 'Adder'.new([1.0]) |> to => o }}{{ o.call('Add',[1.0]) }}{{ o.Double |> return }}");
            Assert.That(result, Is.EqualTo(2.0d));
            
            result = context.Evaluate("{{ Constructor('Adder(string)')(1) |> to => o }}{{ Function('Adder.Add(string)') |> to => adder }}{{ o.adder(1) |> return }}");
            Assert.That(result, Is.EqualTo("11"));
            
            result = context.Evaluate("{{ Constructor('Adder(double)')(1) |> to => o }}{{ Function('Adder.Add(double)') |> to => adder }}{{ o.adder(1) |> return }}");
            Assert.That(result, Is.EqualTo(2.0d));
        }

        [Test]
        public void Can_create_Type_from_registered_Script_Assembly_from_Constructor_ObjectActivator()
        {
            var context = CreateContext(c => {
                c.AllowScriptingOfAllTypes = true;
                c.ScriptNamespaces.Add("System");
            }).Init();

            var result = context.Evaluate<object>(
                @"{{ Constructor('DynamicInt()') |> to => ctor }}{{ ctor() |> return }}");
            Assert.That(result.GetType(), Is.EqualTo(typeof(DynamicInt)));

            result = context.Evaluate<object>(
                @"{{ Constructor('DynamicInt()')() |> return }}");
            Assert.That(result.GetType(), Is.EqualTo(typeof(DynamicInt)));
            
            result = context.Evaluate<object>(
                @"{{ Constructor('DateTime(int,int,int)')(2001,1,1) |> return }}");
            Assert.That(result, Is.EqualTo(new DateTime(2001,1,1)));
            
            result = context.Evaluate<object>(
                @"{{ Constructor('DateTime(int,int,int)') |> to => newDate }}{{ newDate(2001,1,1) |> return }}");
            Assert.That(result, Is.EqualTo(new DateTime(2001,1,1)));
            
            result = context.Evaluate<object>(
                @"{{ Constructor('DateTime(int,int,int)') |> to => newDate }}{{ 2001.newDate(1,1) |> return }}");
            Assert.That(result, Is.EqualTo(new DateTime(2001,1,1)));
         
            result = context.Evaluate<object>(
                @"{{ Constructor('Tuple<string,int>(System.String,System.Int32)') |> to => tuple }}{{ tuple('A',1) |> to => pair }}{{ `${pair.Item1}=${pair.Item2}` |> return }}");
            Assert.That(result, Is.EqualTo("A=1"));
         
            result = context.Evaluate<object>(
                @"{{ Constructor('System.Collections.Generic.KeyValuePair<string,int>(System.String,System.Int32)')('A',1) |> to => kvp }}{{ `${kvp.Key}=${kvp.Value}` |> return }}");
            Assert.That(result, Is.EqualTo("A=1"));
            
            result = context.Evaluate<object>(
                @"{{ Constructor('System.Collections.Generic.KeyValuePair<char,double>(char,double)')('A',1) |> to => kvp }}{{ `${kvp.Key}=${kvp.Value}` |> return }}");
            Assert.That(result, Is.EqualTo("A=1"));
        }

        [Test]
        public void Can_call_generic_methods()
        {
            var context = CreateContext(c => c.ScriptAssemblies.Add(typeof(Ints).Assembly)).Init();

            var result = context.Evaluate<string>(
                "{{ 'Ints'.new([1,2]).call('GenericMethod<string>') |> return }}");
            Assert.That(result, Is.EqualTo("String 3"));

            result = context.Evaluate<string>(
                "{{ 'Ints'.new([1,2]).call('GenericMethod<string>',['arg']) |> return }}");
            Assert.That(result, Is.EqualTo("String arg 3"));

            result = context.Evaluate<string>(
                "{{ typeof('Ints').createInstance([1,2]).call('GenericMethod<string>',['arg']) |> return }}");
            Assert.That(result, Is.EqualTo("String arg 3"));
        }

        [Test]
        public void Can_create_type_with_constructor_arguments()
        {
            var context = CreateContext(c => c.ScriptAssemblies.Add(typeof(Ints).Assembly)).Init();
            
            var result = context.Evaluate<int>(
                "{{ 'Ints'.new([1,2]).call('GetTotal') |> return }}");
            Assert.That(result, Is.EqualTo(3));
            
            result = context.Evaluate<int>(
                "{{ typeof('Ints').createInstance([1,2]).call('GetTotal') |> return }}");
            Assert.That(result, Is.EqualTo(3));
            
            result = context.Evaluate<int>(
                "{{ 'Ints'.new([1,2]).set({ C:3, D:4.0 }).call('GetTotal') |> return }}");
            Assert.That(result, Is.EqualTo(10));
            
            result = context.Evaluate<int>(
                "{{ 'Ints'.new([1,2]).set({ C:3 }).set({ D:4.0 }).call('GetTotal') |> return }}");
            Assert.That(result, Is.EqualTo(10));
            
            result = context.Evaluate<int>(
                "{{ Constructor('Ints(int,int)')(1,2).set({ C:3, D:4 }).call('GetTotal') |> return }}");
            Assert.That(result, Is.EqualTo(10));
            
            result = context.Evaluate<int>(
                "{{ Function('Ints.GetTotal') |> to => total}}" +
                "{{ Constructor('Ints(int,int)') |> to => tuple}}" +
                "{{ tuple(1,2).set({ C:3, D:4 }).total() |> return }}");
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
                "{{ 'KeyValuePair<string,int>'.new(['A',1]) |> to => kvp }}{{ kvp.Key }}={{ kvp.Value }}");
            Assert.That(result, Is.EqualTo("A=1"));

            result = context.EvaluateScript(
                "{{ typeof('KeyValuePair<string,int>').createInstance(['A',1]) |> to => kvp }}{{ kvp.Key }}={{ kvp.Value }}");
            Assert.That(result, Is.EqualTo("A=1"));
        }

        [Test]
        public void Can_create_Type_from_Loaded_Assembly()
        {
            var context = CreateContext(c => c.AllowScriptingOfAllTypes = true).Init();

            var result = context.Evaluate(
                "{{ 'ServiceStack.WebHost.Endpoints.Tests.ScriptTests.Ints'.new() |> return}}");
            Assert.That(result as Ints, Is.Not.Null);
        }

        [Test]
        public void Script_Types_Use_Case()
        {
            var context = CreateContext(c => {
                c.AllowScriptingOfAllTypes = true;
                c.ScriptNamespaces.Add(typeof(DateTime).Namespace);
                c.ScriptNamespaces.Add(typeof(Adder).Namespace);
            }).Init();
            //var url = new Uri();

            object result = null;
            result = context.Evaluate("{{ 'Uri'.new(['http://host.org']) |> return }}");
            Assert.That(result, Is.EqualTo(new Uri("http://host.org")));
            result = context.Evaluate("{{ Constructor('Uri(string)')('http://host.org') |> return }}");
            Assert.That(result, Is.EqualTo(new Uri("http://host.org")));
            result = context.Evaluate("{{ Constructor('Uri(string)') |> to => url }}{{ url('http://host.org') |> return }}");
            Assert.That(result, Is.EqualTo(new Uri("http://host.org")));
            result = context.Evaluate("{{ Constructor('Uri(string)') |> to => url }}{{ 'http://host.org'.url() |> return }}");
            Assert.That(result, Is.EqualTo(new Uri("http://host.org")));
            result = context.Evaluate("{{ Constructor('Uri(string)') |> to => url }}{{ 'http://host.org' |> url |> return }}");
            Assert.That(result, Is.EqualTo(new Uri("http://host.org")));

            result = context.Evaluate("{{ 'DateTime'.new() |> return }}");
            Assert.That(result.GetType(), Is.EqualTo(typeof(DateTime)));

            result = context.Evaluate("{{ Constructor('Adder(double)') |> to => doubleAdder }}{{ doubleAdder(1) |> return }}");
            Assert.That(((Adder) result).Double, Is.EqualTo(1.0d));
            result = context.Evaluate("{{ Constructor('Adder(double)') |> to => doubleAdder }}{{ 1.doubleAdder() |> return }}");
            Assert.That(((Adder) result).Double, Is.EqualTo(1.0d));
            result = context.Evaluate("{{ Constructor('Adder(double)') |> to => doubleAdder }}{{ 1 |> doubleAdder |> return }}");
            Assert.That(((Adder) result).Double, Is.EqualTo(1.0d));
            
            result = context.Evaluate("{{ 'Ints'.new([1,2]) |> to => o }}{{ o.call('GenericMethod<int>') |> return }}");
            Assert.That((string) result, Is.EqualTo("Int32 3"));
            result = context.Evaluate("{{ 'Ints'.new([1,2]) |> to => o }}{{ o.call('GenericMethod<int>',[1]) |> return }}");
            Assert.That((string) result, Is.EqualTo("Int32 1 3"));
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
            
            result = context.EvaluateScript(@"{{ Function('Console.WriteLine(string)') |> to => writeln }}
                {{ writeln('static method') }}
                {{ 'ext method'.writeln() }}");

            result = context.Evaluate<string>(@"{{ Function('StaticLog.Clear')() }}
                {{ Function('StaticLog.Log') |> to => log }}
                {{ log('arg.') }}
                {{ 'ext.'.log() }}
                {{ Function('StaticLog.AllLogs') |> to => allLogs }}{{ allLogs() |> return }}");
            Assert.That(result, Is.EqualTo("arg.ext."));
            
            result = context.Evaluate<string>(@"{{ Function('StaticLog.Clear')() }}
                {{ Function('StaticLog.Log<int>') |> to => log }}
                {{ log('arg.') }}
                {{ 'ext.'.log() }}
                {{ Function('StaticLog.AllLogs') |> to => allLogs }}{{ allLogs() |> return }}");
            Assert.That(result, Is.EqualTo("Int32 arg.Int32 ext."));

            result = context.Evaluate<string>(@"{{ Function('StaticLog.Clear')() }}
                {{ Function('StaticLog.Log')('iife') }}
                {{ Function('StaticLog.AllLogs')() |> return }}");
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
            
            result = context.Evaluate<string>(@"{{ Function('GenericStaticLog<string>.Clear()')() }}
                {{ Function('GenericStaticLog<string>.Log(string)') |> to => log }}
                {{ log('arg.') }}
                {{ 'ext.'.log() }}
                {{ Function('GenericStaticLog<string>.AllLogs') |> to => allLogs }}{{ allLogs() |> return }}");
            Assert.That(result, Is.EqualTo("String arg.String ext."));

            result = context.Evaluate<string>(@"{{ Function('GenericStaticLog<string>.Clear()')() }}
                {{ Function('GenericStaticLog<string>.Log(string)')('iife') }}
                {{ Function('GenericStaticLog<string>.AllLogs')() |> return }}");
            Assert.That(result, Is.EqualTo("String iife"));
            
            result = context.Evaluate<string>(@"{{ Function('GenericStaticLog<string>.Clear()')() }}
                {{ Function('GenericStaticLog<string>.Log<int>') |> to => log }}
                {{ Function('GenericStaticLog<string>.Log<int>(string)') |> to => log }}
                {{ log('arg.') }}
                {{ 'ext.'.log() }}
                {{ Function('GenericStaticLog<string>.AllLogs') |> to => allLogs }}{{ allLogs() |> return }}");
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

            result = context.Evaluate<string>(@"{{ 'InstanceLog'.new(['instance']) |> to => o }}
                {{ Function('InstanceLog.Log') |> to => log }}                
                {{ o.log('arg.') }}
                {{ log(o,'param.') }}
                {{ Function('InstanceLog.AllLogs') |> to => allLogs }}{{ o.allLogs() |> return }}");
            Assert.That(result, Is.EqualTo("instance arg.instance param."));

            result = context.Evaluate<string>(@"{{ Function('InstanceLog.Log<int>') |> to => log }}
                {{ 'InstanceLog'.new(['instance']) |> to => o }}
                {{ o.log('arg.') }}
                {{ log(o,'param.') }}
                {{ Function('InstanceLog.AllLogs') |> to => allLogs }}{{ o.allLogs() |> return }}");
            Assert.That(result, Is.EqualTo("instance Int32 arg.instance Int32 param."));
        }

        [Test]
        public void Can_get_inner_class_properties()
        {
            var context = CreateContext(c => {
                c.AllowScriptingOfAllTypes = true;
                c.ScriptNamespaces.Add(typeof(StaticLog).Namespace);
            }).Init();
            
            string result = null;

            result = context.Evaluate<string>(@"{{ Function('StaticLog.Prop')() |> return }}");
            Assert.That(result, Is.EqualTo("StaticLog.Prop"));
            result = context.Evaluate<string>(@"{{ Function('StaticLog.Field')() |> return }}");
            Assert.That(result, Is.EqualTo("StaticLog.Field"));
            result = context.Evaluate<string>(@"{{ Function('StaticLog.Const')() |> return }}");
            Assert.That(result, Is.EqualTo("StaticLog.Const"));
            
            result = context.Evaluate<string>(@"{{ Function('StaticLog.Inner1.Prop1')() |> return }}");
            Assert.That(result, Is.EqualTo("StaticLog.Inner1.Prop1"));
            result = context.Evaluate<string>(@"{{ Function('StaticLog.Inner1.Field1')() |> return }}");
            Assert.That(result, Is.EqualTo("StaticLog.Inner1.Field1"));
            result = context.Evaluate<string>(@"{{ Function('StaticLog.Inner1.Const1')() |> return }}");
            Assert.That(result, Is.EqualTo("StaticLog.Inner1.Const1"));
            
            result = context.Evaluate<string>(@"{{ Function('StaticLog.Inner1.Inner2.Prop2')() |> return }}");
            Assert.That(result, Is.EqualTo("StaticLog.Inner1.Inner2.Prop2"));
            result = context.Evaluate<string>(@"{{ Function('StaticLog.Inner1.Inner2.Field2')() |> return }}");
            Assert.That(result, Is.EqualTo("StaticLog.Inner1.Inner2.Field2"));
            result = context.Evaluate<string>(@"{{ Function('StaticLog.Inner1.Inner2.Const2')() |> return }}");
            Assert.That(result, Is.EqualTo("StaticLog.Inner1.Inner2.Const2"));
            
            context.Args["o"] = new StaticLog();
            context.Args["o1"] = new StaticLog.Inner1();
            result = context.Evaluate<string>(@"{{ Function('StaticLog.InstanceProp')(o) |> return }}");
            Assert.That(result, Is.EqualTo("StaticLog.InstanceProp"));
            result = context.Evaluate<string>(@"{{ Function('StaticLog.InstanceField')(o) |> return }}");
            Assert.That(result, Is.EqualTo("StaticLog.InstanceField"));
            
            result = context.Evaluate<string>(@"{{ Function('StaticLog.Inner1.InstanceProp1')(o1) |> return }}");
            Assert.That(result, Is.EqualTo("StaticLog.Inner1.InstanceProp1"));
            result = context.Evaluate<string>(@"{{ Function('StaticLog.Inner1.InstanceField1')(o1) |> return }}");
            Assert.That(result, Is.EqualTo("StaticLog.Inner1.InstanceField1"));
        }

        [Test]
        public void Can_call_ContentResolver_Query_on_instance()
        {
            var context = CreateContext(c => {
                c.AllowScriptingOfAllTypes = true;
                c.ScriptNamespaces.Add("System");
                c.ScriptNamespaces.Add(typeof(ContentResolver).Namespace);
            }).Init();

            var result = context.Evaluate(@"{{ F('ContentResolver.Query(Uri,string[],string,string[],string)') |> to => Query }} 
                    {{ MainActivity.Query('http://host.org',['A'],'B',['C'],'D') |> return }}", 
                new Dictionary<string, object> {
                    ["MainActivity"] = new ContentResolver()
                });
            
            Assert.That(result is ICursor);
        }

        [Test]
        public void Can_Call_registered_IOC_Dependency()
        {
            var context = CreateContext(c => {
                c.ScriptTypes.Add(typeof(InstanceLog));
            }).Init();
            context.Container.AddTransient(() => new InstanceLog("ioc"));
            
            string result = null;

            result = context.Evaluate<string>(@"{{ resolve('InstanceLog') |> to => o }}
                {{ Function('InstanceLog.Log') |> to => log }}                
                {{ o.log('arg') }}
                {{ Function('InstanceLog.AllLogs') |> to => allLogs }}{{ o.allLogs() |> return }}".NormalizeNewLines());
            
            Assert.That(result, Is.EqualTo("ioc arg"));
        }

        [Test]
        public void Can_call_resolve_interface_from_registered_Dependency()
        {
            var context = CreateContext(c => {
                c.ScriptNamespaces.Add(typeof(ICursor).Namespace);
                c.AllowScriptingOfAllTypes = true;
            });
            context.Container.AddTransient<ICursor>(() => new Cursor());
            context.Init();

            var output = context.EvaluateCode("'ICursor'.typeof().Name |> return");
            Assert.That(output, Is.EqualTo(nameof(ICursor)));

            output = context.EvaluateCode<List<string>>("resolve('ICursor').methods() |> return");
            Assert.That(output, Is.EqualTo(new[]{ "AMethod", "BMethod"}));

            output = context.EvaluateCode<string>("F('ICursor.AMethod')(resolve('ICursor'), 1) |> return");
            Assert.That(output, Is.EqualTo("AMethod: 1"));

            output = context.EvaluateCode<string>("F('Cursor.BMethod')(resolve('ICursor'), 2) |> return");
            Assert.That(output, Is.EqualTo("BMethod: 2"));
        }

    }
}