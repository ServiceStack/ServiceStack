// -----------------------------------------------------------------------
//   <copyright file="UnsupportedTypeSerializer.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;
using Wire.Compilation;
using Wire.Internal;

namespace Wire.ValueSerializers
{
    //https://github.com/AsynkronIT/Wire/issues/115

    public class UnsupportedTypeException : Exception
    {
        public Type Type;

        public UnsupportedTypeException(Type t, string msg) : base(msg)
        {
        }
    }

    public class UnsupportedTypeSerializer : ValueSerializer
    {
        private readonly string _errorMessage;
        private readonly Type _invalidType;

        public UnsupportedTypeSerializer(Type t, string msg)
        {
            _errorMessage = msg;
            _invalidType = t;
        }

        public override int EmitReadValue([NotNull] ICompiler<ObjectReader> c, int stream, int session,
            [NotNull] FieldInfo field)
        {
            throw new UnsupportedTypeException(_invalidType, _errorMessage);
        }

        public override void EmitWriteValue(ICompiler<ObjectWriter> c, int stream, int fieldValue, int session)
        {
            throw new UnsupportedTypeException(_invalidType, _errorMessage);
        }

        public override object ReadValue([NotNull] Stream stream, [NotNull] DeserializerSession session)
        {
            throw new UnsupportedTypeException(_invalidType, _errorMessage);
        }

        public override void WriteManifest([NotNull] Stream stream, [NotNull] SerializerSession session)
        {
            throw new UnsupportedTypeException(_invalidType, _errorMessage);
        }

        public override void WriteValue([NotNull] Stream stream, object value, [NotNull] SerializerSession session)
        {
            throw new UnsupportedTypeException(_invalidType, _errorMessage);
        }

        public override Type GetElementType()
        {
            throw new UnsupportedTypeException(_invalidType, _errorMessage);
        }
    }
}