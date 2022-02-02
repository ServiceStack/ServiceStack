using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
using ServiceStack.Common.Tests;
//using ServiceStack.OrmLite;
using ServiceStack.Reflection;

namespace ServiceStack.Text.TestsConsole
{
    class Program
    {
        public static void Main(string[] args)
        {
            // PrintDumpColumnSchema();
            
            //var da = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("dyn"),  AssemblyBuilderAccess.Save);

            //var dm = da.DefineDynamicModule("dyn_mod", "dyn.dll");
            //var dt = dm.DefineType("dyn_type");

            //var type = typeof(KeyValuePair<string,string>);
            //var pi = type.GetProperty("Key");

            //var lambdaValueType = PropertyInvoker.GetExpressionLambda<KeyValuePair<string,string>>(pi);
            //lambdaValueType.CompileToMethod(dt.DefineMethod("KVP", MethodAttributes.Public | MethodAttributes.Static));

            //var lambdaRefType = PropertyInvoker.GetExpressionLambda<TRef>(typeof(TRef).GetProperty("PropRef"));
            //lambdaRefType.CompileToMethod(dt.DefineMethod("TRef_PropRef", MethodAttributes.Public | MethodAttributes.Static));

            //var lambdaRefType2 = PropertyInvoker.GetExpressionLambda<IncludeExclude>(typeof(IncludeExclude).GetProperty("Id"));
            //lambdaRefType2.CompileToMethod(dt.DefineMethod("IncludeExclude_Id", MethodAttributes.Public | MethodAttributes.Static));

            //dt.CreateType();
            //da.Save("dyn.dll");

            // new StringConcatPerfTests {
            //     MultipleIterations = new[] { 1000, 10000, 100000, 1000000, 10000000 }
            // }.Compare_interpolation_vs_string_Concat();

            Console.ReadLine();
        }

        class StringConcatPerfTests : PerfTestBase
        {
            public void Compare_interpolation_vs_string_Concat()
            {
                CompareMultipleRuns(
                    "Interpolation",
                    () => SimpleInterpolation("foo"),
                    "string.Concat",
                    () => SimpleConcat("foo"));
            }
        }


        public static object SimpleInterpolation(string text) => $"Hi {text}";

        public static object SimpleFormat(string text) => string.Format("Hi {0}", text);

        public static object SimpleConcat(string text) => "Hi " + text;

        // public static void PrintDumpColumnSchema()
        // {
        //     var dbFactory = new OrmLiteConnectionFactory(":memory:",
        //         SqliteDialect.Provider);
        //
        //     using var db = dbFactory.Open();
        //     db.CreateTableIfNotExists<Person>();
        //
        //     ColumnSchema[] columnSchemas = db.GetTableColumns<Person>();
        //         
        //     columnSchemas.Each(x => x.ToString().Print());
        //     columnSchemas.Each(x => x.PrintDump());
        // }

        public class Person
        {
            public static Person[] Rockstars = {
                new(1, "Jimi", "Hendrix", 27),
                new(2, "Janis", "Joplin", 27),
                new(3, "Jim", "Morrisson", 27),
                new(4, "Kurt", "Cobain", 27),
                new(5, "Elvis", "Presley", 42),
                new(6, "Michael", "Jackson", 50),
            };

            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public int Age { get; set; }

            public Person() { }
            public Person(int id, string firstName, string lastName, int age)
            {
                Id = id;
                FirstName = firstName;
                LastName = lastName;
                Age = age;
            }

            protected bool Equals(Person other)
            {
                return Id == other.Id &&
                       string.Equals(FirstName, other.FirstName) &&
                       string.Equals(LastName, other.LastName) &&
                       Age == other.Age;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Person)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Id;
                    hashCode = (hashCode * 397) ^ (FirstName != null ? FirstName.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (LastName != null ? LastName.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ Age;
                    return hashCode;
                }
            }
        }
    }
}
