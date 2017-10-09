// -----------------------------------------------------------------------
//   <copyright file="DefaultCodeGenerator.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Wire.Extensions;
using Wire.Internal;
using Wire.ValueSerializers;

namespace Wire.Compilation
{
    public class DefaultCodeGenerator : ICodeGenerator
    {
        public const string PreallocatedByteBuffer = nameof(PreallocatedByteBuffer);

        public void BuildSerializer([NotNull] Serializer serializer, [NotNull] ObjectSerializer objectSerializer)
        {
            var type = objectSerializer.Type;
            var fields = type.GetFieldInfosForType();
            int preallocatedBufferSize;
            var writer = GetFieldsWriter(serializer, fields, out preallocatedBufferSize);
            var reader = GetFieldsReader(serializer, fields, type);

            objectSerializer.Initialize(reader, writer, preallocatedBufferSize);
        }

        private ObjectReader GetFieldsReader([NotNull] Serializer serializer, [NotNull] FieldInfo[] fields,
                                             [NotNull] Type type)
        {
            var c = new Compiler<ObjectReader>();
            var stream = c.Parameter<Stream>("stream");
            var session = c.Parameter<DeserializerSession>("session");
            var newExpression = c.NewObject(type);
            var target = c.Variable<object>("target");
            var assignNewObjectToTarget = c.WriteVar(target, newExpression);

            c.Emit(assignNewObjectToTarget);

            if (serializer.Options.PreserveObjectReferences)
            {
                var trackDeserializedObjectMethod =
                    typeof(DeserializerSession).GetTypeInfo().GetMethod(nameof(DeserializerSession.TrackDeserializedObject));

                c.EmitCall(trackDeserializedObjectMethod, session, target);
            }

            //for (var i = 0; i < storedFieldCount; i++)
            //{
            //    var fieldName = stream.ReadLengthEncodedByteArray(session);
            //    if (!Utils.Compare(fieldName, fieldNames[i]))
            //    {
            //        //TODO: field name mismatch
            //        //this should really be a compare less equal or greater
            //        //to know if the field is added or removed

            //        //1) if names are equal, read the value and assign the field

            //        //2) if the field is less than the expected field, then this field is an unknown new field
            //        //we need to read this object and just ignore its content.

            //        //3) if the field is greater than the expected, we need to check the next expected until
            //        //the current is less or equal, then goto 1)
            //    }
            //}

            var typedTarget = c.CastOrUnbox(target, type);
            var serializers = fields.Select(field => serializer.GetSerializerByType(field.FieldType)).ToArray();

            var preallocatedBufferSize = serializers.Length != 0 ? serializers.Max(s => s.PreallocatedByteBufferSize) : 0;
            if (preallocatedBufferSize > 0)
            {
                EmitPreallocatedBuffer(c, preallocatedBufferSize, session,
                                       typeof(DeserializerSession).GetTypeInfo().GetMethod(nameof(DeserializerSession.GetBuffer)));
            }

            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var s = serializers[i];

                int read;
                if (!serializer.Options.VersionTolerance && field.FieldType.IsWirePrimitive())
                {
                    //Only optimize if property names are not included.
                    //if they are included, we need to be able to skip past unknown property data
                    //e.g. if sender have added a new property that the receiveing end does not yet know about
                    //which we cannot do w/o a manifest
                    read = s.EmitReadValue(c, stream, session, field);
                }
                else
                {
                    var method = typeof(StreamEx).GetTypeInfo().GetMethod(nameof(StreamEx.ReadObject));
                    read = c.StaticCall(method, stream, session);
                    read = c.Convert(read, field.FieldType);
                }

                if (field.IsInitOnly)
                {
                    var assignReadToField = c.WriteReadonlyField(field, target, read);
                    c.Emit(assignReadToField);
                }
                else
                {
                    var assignReadToField = c.WriteField(field, typedTarget, read);
                    c.Emit(assignReadToField);
                }

            }
            c.Emit(target);

            var readAllFields = c.Compile();
            return readAllFields;
        }

        private static void EmitPreallocatedBuffer<T>(ICompiler<T> c, int preallocatedBufferSize, int session,
                                                      MethodInfo getBuffer)
        {
            var size = c.Constant(preallocatedBufferSize);
            var buffer = c.Variable<byte[]>(PreallocatedByteBuffer);
            var bufferValue = c.Call(getBuffer, session, size);
            var assignBuffer = c.WriteVar(buffer, bufferValue);
            c.Emit(assignBuffer);
        }

        //this generates a FieldWriter that writes all fields by unrolling all fields and calling them individually
        //no loops involved
        private ObjectWriter GetFieldsWriter([NotNull] Serializer serializer, [NotNull] IEnumerable<FieldInfo> fields,
                                             out int preallocatedBufferSize)
        {
            var c = new Compiler<ObjectWriter>();

            var stream = c.Parameter<Stream>("stream");
            var target = c.Parameter<object>("target");
            var session = c.Parameter<SerializerSession>("session");
            var preserveReferences = c.Constant(serializer.Options.PreserveObjectReferences);

            if (serializer.Options.PreserveObjectReferences)
            {
                var method =
                    typeof(SerializerSession).GetTypeInfo().GetMethod(nameof(SerializerSession.TrackSerializedObject));

                c.EmitCall(method, session, target);
            }

            var fieldsArray = fields.ToArray();
            var serializers = fieldsArray.Select(field => serializer.GetSerializerByType(field.FieldType)).ToArray();

            preallocatedBufferSize = serializers.Length != 0 ? serializers.Max(s => s.PreallocatedByteBufferSize) : 0;

            if (preallocatedBufferSize > 0)
            {
                EmitPreallocatedBuffer(c, preallocatedBufferSize, session,
                                       typeof(SerializerSession).GetTypeInfo().GetMethod("GetBuffer"));
            }

            for (var i = 0; i < fieldsArray.Length; i++)
            {
                var field = fieldsArray[i];
                //get the serializer for the type of the field
                var valueSerializer = serializers[i];
                //runtime Get a delegate that reads the content of the given field

                var cast = c.CastOrUnbox(target, field.DeclaringType);
                var readField = c.ReadField(field, cast);

                //if the type is one of our special primitives, ignore manifest as the content will always only be of this type
                if (!serializer.Options.VersionTolerance && field.FieldType.IsWirePrimitive())
                {
                    //primitive types does not need to write any manifest, if the field type is known
                    valueSerializer.EmitWriteValue(c, stream, readField, session);
                }
                else
                {
                    var converted = c.Convert<object>(readField);
                    var valueType = field.FieldType;
                    if (field.FieldType.IsNullable())
                    {
                        var nullableType = field.FieldType.GetNullableElement();
                        valueSerializer = serializer.GetSerializerByType(nullableType);
                        valueType = nullableType;
                    }

                    var vs = c.Constant(valueSerializer);
                    var vt = c.Constant(valueType);

                    var method = typeof(StreamEx).GetTypeInfo().GetMethod(nameof(StreamEx.WriteObject));

                    c.EmitStaticCall(method, stream, converted, vt, vs, preserveReferences, session);
                }
            }

            return c.Compile();
        }
    }
}