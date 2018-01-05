// -----------------------------------------------------------------------
//   <copyright file="IlBuilder.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//       Copyright (C) 2016-2016 Akka.NET Team <https://github.com/akkadotnet>
//   </copyright>
// -----------------------------------------------------------------------

#if NET45
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Wire.Extensions;

namespace Wire.Compilation
{

    public class IlBuilder
    {
        private readonly List<IlExpression> _expressions = new List<IlExpression>();
        protected List<IlParameter> Parameters { get; } = new List<IlParameter>();
        protected List<IlVariable> Variables { get; } = new List<IlVariable>();
        protected List<object> Constants { get; } = new List<object>();
        protected List<Action<IlCompilerContext>> LazyEmits { get; } = new List<Action<IlCompilerContext>>();

        public int NewObject(System.Type type)
        {
            var ctor = type.GetConstructor(new Type[] {});
            // ReSharper disable once PossibleNullReferenceException
            if (ctor != null && ctor.GetMethodBody().GetILAsByteArray().Length <= 8)
            {
                var @new = new IlNew(type);
                _expressions.Add(@new);
            }
            else
            {
                var typeExp = Constant(type);
                var t = _expressions[typeExp]; //we need the type as a constant so we can load it in IlNew
                var method = typeof(TypeEx).GetMethod(nameof(TypeEx.GetEmptyObject));
                var call = new IlCallStatic(method, t);
                _expressions.Add(call);
            }

            return _expressions.Count - 1;
        }

        public int Parameter<T>(string name)
        {
            var exp = new IlParameter(Parameters.Count + 1, typeof(T), name);
            _expressions.Add(exp);
            Parameters.Add(exp);

            return _expressions.Count - 1;
        }

        public int Variable<T>(string name)
        {
            var exp = new IlVariable(Variables.Count, typeof(T), name);
            _expressions.Add(exp);
            Variables.Add(exp);

            return _expressions.Count - 1;
        }

        public int Variable(string name,Type type)
        {
            var exp = new IlVariable(Variables.Count, type, name);
            _expressions.Add(exp);
            Variables.Add(exp);

            return _expressions.Count - 1;
        }

        public int GetVariable<T>(string name)
        {
            var existing =
                _expressions.OfType<IlVariable>().FirstOrDefault(v => v.Name == name && v.VarType == typeof(T));
            if (existing == null)
            {
                throw new Exception("Variable not found");
            }

            return _expressions.IndexOf(existing);
        }

        public int Constant(object value)
        {
            if (value is bool)
            {
                //doing this is faster than storing this as state
                _expressions.Add(new IlBool((bool) value));
                return _expressions.Count - 1;
            }

            _expressions.Add(new IlRuntimeConstant(value, Constants.Count));
            Constants.Add(value);
            return _expressions.Count - 1;
        }

        public int CastOrUnbox(int value, Type type)
        {
            var valueExp = _expressions[value];
            if (type.IsValueType)
                _expressions.Add(new IlUnbox(type, valueExp));
            else
                _expressions.Add(new IlCastClass(type, valueExp));
            return _expressions.Count - 1;
        }

        public void EmitCall(MethodInfo method, int target, params int[] arguments)
        {
            var call = Call(method, target, arguments);
            Emit(call);
        }

        public void EmitStaticCall(MethodInfo method, params int[] arguments)
        {
            var call = StaticCall(method, arguments);
            Emit(call);
        }

        public int Call(MethodInfo method, int target, params int[] arguments)
        {
            var call = new IlCall(_expressions[target], method, arguments.Select(a => _expressions[a]).ToArray());
            _expressions.Add(call);
            return _expressions.Count - 1;
        }

        public int StaticCall(MethodInfo method, params int[] arguments)
        {
            var call = new IlCallStatic(method, arguments.Select(a => _expressions[a]).ToArray());
            _expressions.Add(call);
            return _expressions.Count - 1;
        }

        public int ReadField(FieldInfo field, int target)
        {
            var exp = new IlReadField(field, _expressions[target]);
            _expressions.Add(exp);
            return _expressions.Count - 1;
        }

        public int WriteField(FieldInfo field, int typedTarget, int value)
        {
            var exp = new IlWriteField(field, _expressions[typedTarget], _expressions[value]);
            _expressions.Add(exp);
            return _expressions.Count - 1;
        }

        public int WriteReadonlyField(FieldInfo field, int target, int value)
        {
            var exp = new IlWriteField(field, _expressions[target], _expressions[value]);
            _expressions.Add(exp);
            return _expressions.Count - 1;
        }

        public int WriteVar(int variable, int value)
        {
            var variableExp = _expressions[variable] as IlVariable;
            var valueExp = _expressions[value];
            var exp = new IlWriteVariable(variableExp, valueExp);
            _expressions.Add(exp);
            return _expressions.Count - 1;
        }

        public void Emit(int value)
        {
            LazyEmits.Add(ctx =>
            {
                var exp = _expressions[value];
                exp.Emit(ctx);
            });
        }

        public int Convert<T>(int value)
        {
            return Convert(value, typeof(T));
        }

        public int Convert(int value, Type type)
        {
            var valueExp = _expressions[value];
            if (valueExp.Type().IsValueType)
                _expressions.Add(new IlBox(valueExp.Type(), valueExp));
            else
                _expressions.Add(new IlCastClass(type, valueExp));
            return _expressions.Count - 1;
        }
    }

}
#endif