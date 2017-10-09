// -----------------------------------------------------------------------
//   <copyright file="ExceptionSerializerFactory.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using Wire.Extensions;
using Wire.ValueSerializers;

namespace Wire.SerializerFactories
{
    public class ExceptionSerializerFactory : ValueSerializerFactory
    {
        private static readonly TypeInfo ExceptionTypeInfo = typeof(Exception).GetTypeInfo();
        private readonly FieldInfo _className;
        private readonly FieldInfo _innerException;
        private readonly FieldInfo _message;
        private readonly FieldInfo _remoteStackTraceString;
        private readonly FieldInfo _stackTraceString;

        public ExceptionSerializerFactory()
        {
            _className = ExceptionTypeInfo.GetField("_className", BindingFlagsEx.All);
            _innerException = ExceptionTypeInfo.GetField("_innerException", BindingFlagsEx.All);
            _message = ExceptionTypeInfo.GetField("_message", BindingFlagsEx.All);
            _remoteStackTraceString = ExceptionTypeInfo.GetField("_remoteStackTraceString", BindingFlagsEx.All);
            _stackTraceString = ExceptionTypeInfo.GetField("_stackTraceString", BindingFlagsEx.All);
        }

        public override bool CanSerialize(Serializer serializer, Type type)
            => ExceptionTypeInfo.IsAssignableFrom(type.GetTypeInfo());

        public override bool CanDeserialize(Serializer serializer, Type type) => CanSerialize(serializer, type);

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            ConcurrentDictionary<Type, ValueSerializer> typeMapping)
        {
            var exceptionSerializer = new ObjectSerializer(type);

            object Reader(Stream stream, DeserializerSession session)
            {
                var exception = Activator.CreateInstance(type);
                var className = stream.ReadString(session);
                var message = stream.ReadString(session);
                var remoteStackTraceString = stream.ReadString(session);
                var stackTraceString = stream.ReadString(session);
                var innerException = stream.ReadObject(session);

                _className.SetValue(exception, className);
                _message.SetValue(exception, message);
                _remoteStackTraceString.SetValue(exception, remoteStackTraceString);
                _stackTraceString.SetValue(exception, stackTraceString);
                _innerException.SetValue(exception, innerException);
                return exception;
            }

            void Writer(Stream stream, object exception, SerializerSession session)
            {
                var className = (string) _className.GetValue(exception);
                var message = (string) _message.GetValue(exception);
                var remoteStackTraceString = (string) _remoteStackTraceString.GetValue(exception);
                var stackTraceString = (string) _stackTraceString.GetValue(exception);
                var innerException = _innerException.GetValue(exception);
                StringSerializer.WriteValueImpl(stream, className, session);
                StringSerializer.WriteValueImpl(stream, message, session);
                StringSerializer.WriteValueImpl(stream, remoteStackTraceString, session);
                StringSerializer.WriteValueImpl(stream, stackTraceString, session);
                stream.WriteObjectWithManifest(innerException, session);
            }

            exceptionSerializer.Initialize(Reader, Writer);
            typeMapping.TryAdd(type, exceptionSerializer);
            return exceptionSerializer;
        }
    }
}