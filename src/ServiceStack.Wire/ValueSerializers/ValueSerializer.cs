// -----------------------------------------------------------------------
//   <copyright file="ValueSerializer.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using Wire.Compilation;
using Wire.Internal;

namespace Wire.ValueSerializers
{
    public abstract class ValueSerializer
    {
        /// <summary>
        ///     Marks a given <see cref="ValueSerializer" /> as one requiring a preallocated byte buffer to perform its operations.
        ///     The byte[] value will be accessible in <see cref="ValueSerializer.EmitWriteValue" /> and
        ///     <see cref="ValueSerializer.EmitReadValue" /> in the <see cref="ICompiler{TDel}" /> with
        ///     <see cref="ICompiler{TDel}.GetVariable{T}" /> under following name
        ///     <see cref="DefaultCodeGenerator.PreallocatedByteBuffer" />.
        /// </summary>
        public virtual int PreallocatedByteBufferSize => 0;

        public abstract void WriteManifest([NotNull] Stream stream, [NotNull] SerializerSession session);
        public abstract void WriteValue([NotNull] Stream stream, object value, [NotNull] SerializerSession session);
        public abstract object ReadValue([NotNull] Stream stream, [NotNull] DeserializerSession session);
        public abstract Type GetElementType();

        public virtual void EmitWriteValue(ICompiler<ObjectWriter> c, int stream, int fieldValue, int session)
        {
            var converted = c.Convert<object>(fieldValue);
            var method = typeof(ValueSerializer).GetTypeInfo().GetMethod(nameof(WriteValue));

            //write it to the value serializer
            var vs = c.Constant(this);
            c.EmitCall(method, vs, stream, converted, session);
        }

        public virtual int EmitReadValue([NotNull] ICompiler<ObjectReader> c, int stream, int session,
            [NotNull] FieldInfo field)
        {
            var method = typeof(ValueSerializer).GetTypeInfo().GetMethod(nameof(ReadValue));
            var ss = c.Constant(this);
            var read = c.Call(method, ss, stream, session);
            read = c.Convert(read, field.FieldType);
            return read;
        }

        protected static MethodInfo GetStatic([NotNull] LambdaExpression expression, [NotNull] Type expectedReturnType)
        {
            var unaryExpression = (UnaryExpression) expression.Body;
            var methodCallExpression = (MethodCallExpression) unaryExpression.Operand;
            var methodCallObject = (ConstantExpression) methodCallExpression.Object;
            var method = (MethodInfo) methodCallObject.Value;

            if (method.IsStatic == false)
            {
                throw new ArgumentException($"Method {method.Name} should be static.");
            }

            if (method.ReturnType != expectedReturnType)
            {
                throw new ArgumentException($"Method {method.Name} should return {expectedReturnType.Name}.");
            }

            return method;
        }
    }
}