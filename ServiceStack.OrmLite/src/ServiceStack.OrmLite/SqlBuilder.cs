using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Threading;
using ServiceStack.Text;
using PropertyAttributes = System.Reflection.PropertyAttributes;

namespace ServiceStack.OrmLite
{
#if !NO_EXPRESSIONS
    /// <summary>
    /// Nice SqlBuilder class by @samsaffron from Dapper.Contrib:
    /// http://samsaffron.com/archive/2011/09/05/Digging+ourselves+out+of+the+mess+Linq-2-SQL+created
    /// Modified to work in .NET 3.5
    /// </summary>
    public class SqlBuilder
    {
        readonly Dictionary<string, Clauses> data = new Dictionary<string, Clauses>();
        int seq;

        class Clause
        {
            public string Sql { get; set; }
            public object Parameters { get; set; }
        }

        class DynamicParameters
        {
            class Property
            {
                public Property(string name, Type type, object value)
                {
                    Name = name;
                    Type = type;
                    Value = value;
                }

                public readonly string Name;
                public readonly Type Type;
                public readonly object Value;
            }

            private readonly List<Property> properties = new List<Property>();

            public DynamicParameters(object initParams)
            {
                AddDynamicParams(initParams);
            }

            public void AddDynamicParams(object cmdParams)
            {
                if (cmdParams == null) return;
                foreach (var pi in cmdParams.GetType().GetPublicProperties())
                {
                    var getterFn = pi.CreateGetter();
                    if (getterFn == null) continue;
                    var value = getterFn(cmdParams);
                    properties.Add(new Property(pi.Name, pi.PropertyType, value));
                }
            }

            // The property set and get methods require a special attrs:
            private const MethodAttributes GetSetAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            public object CreateDynamicType()
            {
                var assemblyName = new AssemblyName { Name = "tmpAssembly" };
#if NETCORE
                var typeBuilder =
                    AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
                    .DefineDynamicModule("tmpModule")
                    .DefineType("SqlBuilderDynamicParameters", TypeAttributes.Public | TypeAttributes.Class);
#else
                var typeBuilder =
                    Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
                    .DefineDynamicModule("tmpModule")
                    .DefineType("SqlBuilderDynamicParameters", TypeAttributes.Public | TypeAttributes.Class);
#endif
                var emptyCtor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                var ctorIL = emptyCtor.GetILGenerator();

                var unsetValues = new List<Property>();

                // Loop over the attributes that will be used as the properties names in out new type
                foreach (var p in properties)
                {
                    // Generate a private field
                    var field = typeBuilder.DefineField("_" + p.Name, p.Type, FieldAttributes.Private);

                    //set default values with Emit for popular types
                    if (p.Type == typeof(int))
                    {
                        ctorIL.Emit(OpCodes.Ldarg_0);
                        ctorIL.Emit(OpCodes.Ldc_I4, (int)p.Value);
                        ctorIL.Emit(OpCodes.Stfld, field);
                    }
                    else if (p.Type == typeof(long))
                    {
                        ctorIL.Emit(OpCodes.Ldarg_0);
                        ctorIL.Emit(OpCodes.Ldc_I8, (long)p.Value);
                        ctorIL.Emit(OpCodes.Stfld, field);
                    }
                    else if (p.Type == typeof(string))
                    {
                        ctorIL.Emit(OpCodes.Ldarg_0);
                        ctorIL.Emit(OpCodes.Ldstr, (string)p.Value);
                        ctorIL.Emit(OpCodes.Stfld, field);
                    }
                    else
                    {
                        unsetValues.Add(p); //otherwise use reflection
                    }

                    // Generate a public property
                    var property = typeBuilder.DefineProperty(p.Name, PropertyAttributes.None, p.Type, new[] { p.Type });

                    // Define the "get" accessor method for current private field.
                    var currGetPropMthdBldr = typeBuilder.DefineMethod("get_" + p.Name, GetSetAttr, p.Type, Type.EmptyTypes);

                    // Get Property impl
                    var currGetIL = currGetPropMthdBldr.GetILGenerator();
                    currGetIL.Emit(OpCodes.Ldarg_0);
                    currGetIL.Emit(OpCodes.Ldfld, field);
                    currGetIL.Emit(OpCodes.Ret);

                    // Define the "set" accessor method for current private field.
                    var currSetPropMthdBldr = typeBuilder.DefineMethod("set_" + p.Name, GetSetAttr, null, new[] { p.Type });

                    // Set Property impl
                    var currSetIL = currSetPropMthdBldr.GetILGenerator();
                    currSetIL.Emit(OpCodes.Ldarg_0);
                    currSetIL.Emit(OpCodes.Ldarg_1);
                    currSetIL.Emit(OpCodes.Stfld, field);
                    currSetIL.Emit(OpCodes.Ret);

                    // Hook up, getters and setters.
                    property.SetGetMethod(currGetPropMthdBldr);
                    property.SetSetMethod(currSetPropMthdBldr);
                }

                ctorIL.Emit(OpCodes.Ret);

#if NETCORE
                var generetedType = typeBuilder.CreateTypeInfo().AsType();
#else
                var generetedType = typeBuilder.CreateType();
#endif
                var instance = Activator.CreateInstance(generetedType);

                //Using reflection for less property types. Not caching since it's a generated type.
                foreach (var p in unsetValues)
                {
                    generetedType.GetProperty(p.Name).GetSetMethod().Invoke(instance, new[] { p.Value });
                }

                return instance;
            }
        }

        class Clauses : List<Clause>
        {
            readonly string joiner;
            readonly string prefix;
            readonly string postfix;

            public Clauses(string joiner, string prefix = "", string postfix = "")
            {
                this.joiner = joiner;
                this.prefix = prefix;
                this.postfix = postfix;
            }

            public string ResolveClauses(DynamicParameters p)
            {
                foreach (var item in this)
                {
                    p.AddDynamicParams(item.Parameters);
                }
                return prefix + string.Join(joiner, this.Select(c => c.Sql).ToArray()) + postfix;
            }
        }

        public class Template : ISqlExpression
        {
            readonly string sql;
            readonly SqlBuilder builder;
            readonly object initParams;
            int dataSeq = -1; // Unresolved

            public Template(SqlBuilder builder, string sql, object parameters)
            {
                this.initParams = parameters;
                this.sql = sql;
                this.builder = builder;
            }

            static readonly Regex regex = new Regex(@"\/\*\*.+\*\*\/", RegexOptions.Compiled | RegexOptions.Multiline);

            void ResolveSql()
            {
                if (dataSeq != builder.seq)
                {
                    var p = new DynamicParameters(initParams);

                    rawSql = sql;

                    foreach (var pair in builder.data)
                    {
                        rawSql = rawSql.Replace("/**" + pair.Key + "**/", pair.Value.ResolveClauses(p));
                    }
                    parameters = p.CreateDynamicType();

                    // replace all that is left with empty
                    rawSql = regex.Replace(rawSql, "");

                    dataSeq = builder.seq;
                }
            }

            string rawSql;
            object parameters;

            public string RawSql { get { ResolveSql(); return rawSql; } }
            public object Parameters { get { ResolveSql(); return parameters; } }

            public List<IDbDataParameter> Params { get; private set; }

            public string ToSelectStatement() => ToSelectStatement(QueryType.Select);
            public string ToSelectStatement(QueryType forType)
            {
                return RawSql;
            }

            public string SelectInto<T>() => RawSql;
            public string SelectInto<T>(QueryType queryType) => RawSql;
        }

        public Template AddTemplate(string sql, object parameters = null)
        {
            return new Template(this, sql, parameters);
        }

        void AddClause(string name, string sql, object parameters, string joiner, string prefix = "", string postfix = "")
        {
            Clauses clauses;
            if (!data.TryGetValue(name, out clauses))
            {
                clauses = new Clauses(joiner, prefix, postfix);
                data[name] = clauses;
            }
            clauses.Add(new Clause { Sql = sql, Parameters = parameters });
            seq++;
        }


        public SqlBuilder LeftJoin(string sql, object parameters = null)
        {
            AddClause("leftjoin", sql, parameters, joiner: "\nLEFT JOIN ", prefix: "\nLEFT JOIN ", postfix: "\n");
            return this;
        }

        public SqlBuilder Where(string sql, object parameters = null)
        {
            AddClause("where", sql, parameters, " AND ", prefix: "WHERE ", postfix: "\n");
            return this;
        }

        public SqlBuilder OrderBy(string sql, object parameters = null)
        {
            AddClause("orderby", sql, parameters, " , ", prefix: "ORDER BY ", postfix: "\n");
            return this;
        }

        public SqlBuilder Select(string sql, object parameters = null)
        {
            AddClause("select", sql, parameters, " , ", prefix: "", postfix: "\n");
            return this;
        }

        public SqlBuilder AddParameters(object parameters)
        {
            AddClause("--parameters", "", parameters, "");
            return this;
        }

        public SqlBuilder Join(string sql, object parameters = null)
        {
            AddClause("join", sql, parameters, joiner: "\nJOIN ", prefix: "\nJOIN", postfix: "\n");
            return this;
        }
    }
#endif

}