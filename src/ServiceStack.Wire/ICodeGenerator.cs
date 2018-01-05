// -----------------------------------------------------------------------
//   <copyright file="ICodeGenerator.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using Wire.ValueSerializers;

namespace Wire
{
    public interface ICodeGenerator
    {
        void BuildSerializer(Serializer serializer, ObjectSerializer objectSerializer);
    }
}