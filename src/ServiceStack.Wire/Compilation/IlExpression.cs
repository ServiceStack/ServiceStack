// -----------------------------------------------------------------------
//   <copyright file="IlExpression.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

#if NET45
using System;
using System.Reflection;
using System.Reflection.Emit;
using Wire.Extensions;

namespace Wire.Compilation
{

    public abstract class IlExpression
    {
        public abstract void Emit(IlCompilerContext ctx);
        public abstract Type Type();
    }

    public class IlBool : IlExpression
    {
        private readonly bool _value;

        public IlBool(bool value)
        {
            _value = value;
        }

        public override void Emit(IlCompilerContext ctx)
        {
            ctx.Il.Emit(_value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            ctx.StackDepth++;
        }

        public override Type Type() => typeof(bool);
    }

    public class IlRuntimeConstant : IlExpression
    {
        private readonly object _object;

        public IlRuntimeConstant(object value, int index)
        {
            _object = value;
            Index = index;
        }

        public int Index { get; }

        public override void Emit(IlCompilerContext ctx)
        {
            var field = ctx.SelfType.GetFields(BindingFlagsEx.All)[Index];
            ctx.Il.Emit(OpCodes.Ldarg_0);
            ctx.Il.Emit(OpCodes.Ldfld, field);
            ctx.StackDepth++;
        }

        public override Type Type() => _object.GetType();
    }

    public class IlReadField : IlExpression
    {
        private readonly FieldInfo _field;
        private readonly IlExpression _target;

        public IlReadField(FieldInfo field, IlExpression target)
        {
            _field = field;
            _target = target;
        }

        public override void Emit(IlCompilerContext ctx)
        {
            _target.Emit(ctx);
            ctx.Il.Emit(OpCodes.Ldfld, _field);
            //we are still at the same stack size as we consumed the target
        }

        public override Type Type() => _field.FieldType;
    }

    public class IlWriteVariable : IlExpression
    {
        private readonly IlExpression _value;
        private readonly IlVariable _variable;

        public IlWriteVariable(IlVariable variable, IlExpression value)
        {
            _variable = variable;
            _value = value;
        }

        public override void Emit(IlCompilerContext ctx)
        {
            _value.Emit(ctx);
            ctx.Il.Emit(OpCodes.Stloc, _variable.VariableIndex);
            ctx.StackDepth--;
        }

        public override Type Type()
        {
            throw new NotImplementedException();
        }
    }

    public class IlWriteField : IlExpression
    {
        private readonly FieldInfo _field;
        private readonly IlExpression _target;
        private readonly IlExpression _value;

        public IlWriteField(FieldInfo field, IlExpression target, IlExpression value)
        {
            _field = field;
            _target = target;
            _value = value;
        }

        public override void Emit(IlCompilerContext ctx)
        {
            _target.Emit(ctx);
            _value.Emit(ctx);
            ctx.Il.Emit(OpCodes.Stfld, _field);
            ctx.StackDepth -= 2;
        }

        public override Type Type()
        {
            throw new NotImplementedException();
        }
    }

    public class IlNew : IlExpression
    {
        private readonly Type _type;

        public IlNew(Type type)
        {
            _type = type;
        }

        public override void Emit(IlCompilerContext ctx)
        {
            var ctor = _type.GetConstructor(new Type[] {});
            // ReSharper disable once AssignNullToNotNullAttribute
            ctx.Il.Emit(OpCodes.Newobj, ctor);
            ctx.StackDepth++;
        }

        public override Type Type() => _type;
    }

    public class IlParameter : IlExpression
    {
        private readonly Type _type;

        public IlParameter(int parameterIndex, Type type, string name)
        {
            Name = name;
            ParameterIndex = parameterIndex;
            _type = type;
        }

        public string Name { get; }
        public int ParameterIndex { get; }

        public override void Emit(IlCompilerContext ctx)
        {
            ctx.Il.Emit(OpCodes.Ldarg, ParameterIndex);
            ctx.StackDepth++;
        }

        public override Type Type() => _type;
    }

    public class IlVariable : IlExpression
    {
        public IlVariable(int variableIndex, Type type, string name)
        {
            VariableIndex = variableIndex;
            Name = name;
            VarType = type;
        }

        public int VariableIndex { get; }
        public string Name { get; }
        public Type VarType { get; }

        public override void Emit(IlCompilerContext ctx)
        {
            ctx.Il.Emit(OpCodes.Ldloc, VariableIndex);
            ctx.StackDepth++;
        }

        public override Type Type() => VarType;
    }

    public class IlCastClass : IlExpression
    {
        private readonly IlExpression _expression;
        private readonly Type _type;

        public IlCastClass(Type type, IlExpression expression)
        {
            _type = type;
            _expression = expression;
        }

        public override void Emit(IlCompilerContext ctx)
        {
            _expression.Emit(ctx);
            ctx.StackDepth--;
            ctx.Il.Emit(OpCodes.Castclass, _type);
            ctx.StackDepth++;
        }

        public override Type Type() => _type;
    }

    public class IlBox : IlExpression
    {
        private readonly IlExpression _expression;
        private readonly Type _type;

        public IlBox(Type type, IlExpression expression)
        {
            _type = type;
            _expression = expression;
        }

        public override void Emit(IlCompilerContext ctx)
        {
            _expression.Emit(ctx);
            ctx.Il.Emit(OpCodes.Box, _type);
        }

        public override Type Type() => typeof(object);
    }

    public class IlUnbox : IlExpression
    {
        private readonly IlExpression _expression;
        private readonly Type _type;

        public IlUnbox(Type type, IlExpression expression)
        {
            _type = type;
            _expression = expression;
        }

        public override void Emit(IlCompilerContext ctx)
        {
            _expression.Emit(ctx);
            ctx.Il.Emit(OpCodes.Unbox_Any, _type);
        }

        public override Type Type() => _type;
    }

    public class IlCall : IlExpression
    {
        private readonly IlExpression[] _args;
        private readonly MethodInfo _method;
        private readonly IlExpression _target;

        public IlCall(IlExpression target, MethodInfo method, params IlExpression[] args)
        {
            if (args.Length != method.GetParameters().Length)
            {
                throw new ArgumentException("Parameter count mismatch", nameof(args));
            }

            _target = target;
            _method = method;
            _args = args;
        }

        public override void Emit(IlCompilerContext ctx)
        {
            _target.Emit(ctx);
            ctx.StackDepth--;
            foreach (var arg in _args)
            {
                arg.Emit(ctx);
                ctx.StackDepth--;
            }
            if (_method.IsVirtual)
            {
                ctx.Il.EmitCall(OpCodes.Callvirt, _method, null);
            }
            else
            {
                ctx.Il.EmitCall(OpCodes.Call, _method, null);
            }
            if (_method.ReturnType != typeof(void))
            {
                ctx.StackDepth++;
            }
        }

        public override Type Type() => _method.ReturnType;
    }

    public class IlCallStatic : IlExpression
    {
        private readonly IlExpression[] _args;
        private readonly MethodInfo _method;

        public IlCallStatic(MethodInfo method, params IlExpression[] args)
        {
            if (args.Length != method.GetParameters().Length)
            {
                throw new ArgumentException("Parameter count mismatch", nameof(args));
            }

            _method = method;
            _args = args;
        }

        public override void Emit(IlCompilerContext ctx)
        {
            foreach (var arg in _args)
            {
                arg.Emit(ctx);
                ctx.StackDepth--;
            }
            ctx.Il.EmitCall(OpCodes.Call, _method, null);
            if (_method.ReturnType != typeof(void))
            {
                ctx.StackDepth++;
            }
        }

        public override Type Type() => _method.ReturnType;
    }

}
#endif