// -----------------------------------------------------------------------
//   <copyright file="Delegates.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System.IO;

namespace Wire
{
    //Reads an entire object from a stream, including manifests
    public delegate object ObjectReader(Stream stream, DeserializerSession session);

    //Reads a specific fieldinfo from a stream and sets it on the `obj`
    public delegate void FieldReader(Stream stream, object obj, DeserializerSession session);

    //Writes an entire object to a stream, including manifests
    public delegate void ObjectWriter(Stream stream, object obj, SerializerSession session);

    //Writes the content of an object to a stream
    public delegate void FieldsWriter(Stream stream, object obj, SerializerSession session);

    //Reads a Reflection.FieldInfo from the given object. the fieldinfo itself, is compiled into the delegate body
    public delegate object FieldInfoReader(object obj);

    //Writes a value to a Reflection.FieldInfo
    public delegate void FieldInfoWriter(object obj, object value);

    public delegate object TypedArray(object obj);
}