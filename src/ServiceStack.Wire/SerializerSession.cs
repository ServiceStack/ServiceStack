// -----------------------------------------------------------------------
//   <copyright file="SerializerSession.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Wire
{
    public class SerializerSession
    {
        public const int MinBufferSize = 9;
        private readonly ushort _nextTypeId;
        private readonly Dictionary<object, int> _objects;
        public readonly Serializer Serializer;
        private byte[] _buffer = new byte[MinBufferSize];

        private int _nextObjectId;
        private LinkedList<Type> _trackedTypes;

        public SerializerSession(Serializer serializer)
        {
            Serializer = serializer;
            if (serializer.Options.PreserveObjectReferences)
            {
                _objects = new Dictionary<object, int>();
            }
            _nextTypeId = (ushort) serializer.Options.KnownTypes.Length;
        }

        public void TrackSerializedObject(object obj)
        {
            try
            {
                _objects.Add(obj, _nextObjectId++);
            }
            catch (Exception x)
            {
                throw new Exception("Error tracking object ", x);
            }
        }

        public bool TryGetObjectId(object obj, out int objectId)
        {
            return _objects.TryGetValue(obj, out objectId);
        }

        public bool ShouldWriteTypeManifest(Type type, out ushort index)
        {
            return !TryGetValue(type, out index);
        }

        public byte[] GetBuffer(int length)
        {
            if (length <= _buffer.Length)
            {
                return _buffer;
            }

            length = Math.Max(length, _buffer.Length*2);

            _buffer = new byte[length];

            return _buffer;
        }

        public bool TryGetValue(Type key, out ushort value)
        {
            if (_trackedTypes == null || _trackedTypes.Count == 0)
            {
                value = 0;
                return false;
            }

            var j = _nextTypeId;
            foreach (var t in _trackedTypes)
            {
                if (key == t)
                {
                    value = j;
                    return true;
                }
                j++;
            }

            value = 0;
            return false;
        }

        public void TrackSerializedType(Type type)
        {
            if (_trackedTypes == null)
            {
                _trackedTypes = new LinkedList<Type>();
            }
            _trackedTypes.AddLast(type);
        }
    }
}