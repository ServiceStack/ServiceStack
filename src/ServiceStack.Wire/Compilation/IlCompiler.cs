// -----------------------------------------------------------------------
//   <copyright file="IlCompiler.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//       Copyright (C) 2016-2016 Akka.NET Team <https://github.com/akkadotnet>
//   </copyright>
// -----------------------------------------------------------------------
#if NET45
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Wire.Compilation
{

    public class IlCompiler<TDel> : IlBuilder, ICompiler<TDel>
    {
        public TDel Compile()
        {
            var delegateType = typeof(TDel);
            var invoke = delegateType.GetMethod("Invoke");

            var self = BuildSelf();
            var selfType = self?.GetType() ?? typeof(object);
            var parametersWithSelf = GetParameterTypesWithSelf(invoke, selfType);
            var returnType = invoke.ReturnType;
            var method = new DynamicMethod("foo", MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard, returnType, parametersWithSelf, typeof(string).Module, true);

            var il = method.GetILGenerator();
            var context = new IlCompilerContext(il, selfType);

            //declare local variables
            foreach (var variable in Variables)
            {
                il.DeclareLocal(variable.Type());
            }

            //declare "this"
            method.DefineParameter(0, ParameterAttributes.None, "this");

            //declare custom parameters
            foreach (var parameter in Parameters)
            {
                method.DefineParameter(parameter.ParameterIndex, ParameterAttributes.None, parameter.Name);
            }

            //emit il code
            LazyEmits.ForEach(e => e(context));

            //we need to return
            context.Il.Emit(OpCodes.Ret);

            //   Console.WriteLine(context.Il.ToString());

            //if we have a return type, it's OK that there is one item on the stack
            if (returnType != typeof(void))
            {
                context.StackDepth--;
            }

            //if the stack is not aligned, there is some error
            if (context.StackDepth != 0)
            {
                throw new NotSupportedException("Stack error");
            }

            var del = (TDel) (object) method.CreateDelegate(typeof(TDel), self);

            return del;
        }

        private static Type[] GetParameterTypesWithSelf(MethodInfo invoke, Type selfType)
        {
            var parameterTypes = invoke.GetParameters().Select(a => a.ParameterType).ToArray();
            var parametersWithSelf = new[] {selfType}.Concat(parameterTypes).ToArray();
            return parametersWithSelf;
        }

        private object BuildSelf()
        {
            if (!Constants.Any())
            {
                return null;
            }

            //TODO: a tuple will not be enough, we need arbitrary many constants.
            //just emit the state object instead.
            var tupleTypes = Constants.Select(c => c.GetType()).ToArray();
            var genericTupleFactory =
                typeof(Tuple)
                    .GetMethods()
                    .First(m => m.Name == "Create" && m.GetParameters().Length == tupleTypes.Length);
            var tupleFactory = genericTupleFactory.MakeGenericMethod(tupleTypes);

            var self = tupleFactory.Invoke(null, Constants.ToArray());
            return self;
        }
    }

}
#endif