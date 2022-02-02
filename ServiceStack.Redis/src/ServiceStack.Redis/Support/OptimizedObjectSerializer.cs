using System;
using System.IO;
using System.Text;
using ServiceStack.Text;
using ServiceStack;

namespace ServiceStack.Redis.Support
{
    /// <summary>
    /// Optimized  <see cref="ISerializer"/> implementation. Primitive types are manually serialized, the rest are serialized using binary serializer />.
    /// </summary>
    public class OptimizedObjectSerializer : ObjectSerializer
    {
        internal const ushort RawDataFlag = 0xfa52;
        internal static readonly byte[] EmptyArray = new byte[0];

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override byte[] Serialize(object value)
        {
            var temp = SerializeToWrapper(value);
            return base.Serialize(temp);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="someBytes"></param>
        /// <returns></returns>
        public override object Deserialize(byte[] someBytes)
        {
            var temp = (SerializedObjectWrapper)base.Deserialize(someBytes);
            return Unwrap(temp);
        }

        /// <summary>
        /// serialize value and wrap with <see cref="SerializedObjectWrapper"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        SerializedObjectWrapper SerializeToWrapper(object value)
        {
            // raw data is a special case when some1 passes in a buffer (byte[] or ArraySegment<byte>)
            if (value is ArraySegment<byte>)
            {
                // ArraySegment<byte> is only passed in when a part of buffer is being 
                // serialized, usually from a MemoryStream (To avoid duplicating arrays 
                // the byte[] returned by MemoryStream.GetBuffer is placed into an ArraySegment.)
                // 
                return new SerializedObjectWrapper(RawDataFlag, (ArraySegment<byte>)value);
            }

            byte[] tmpByteArray = value as byte[];

            // - or we just received a byte[]. No further processing is needed.
            if (tmpByteArray != null)
            {
                return new SerializedObjectWrapper(RawDataFlag, new ArraySegment<byte>(tmpByteArray));
            }

            TypeCode code = value == null ? TypeCode.DBNull : value.GetType().GetTypeCode();

            byte[] data;
            int length = -1;

            switch (code)
            {
                case TypeCode.DBNull:
                    data = EmptyArray;
                    length = 0;
                    break;

                case TypeCode.String:
                    data = Encoding.UTF8.GetBytes((string)value);
                    break;

                case TypeCode.Boolean:
                    data = BitConverter.GetBytes((bool)value);
                    break;

                case TypeCode.Int16:
                    data = BitConverter.GetBytes((short)value);
                    break;

                case TypeCode.Int32:
                    data = BitConverter.GetBytes((int)value);
                    break;

                case TypeCode.Int64:
                    data = BitConverter.GetBytes((long)value);
                    break;

                case TypeCode.UInt16:
                    data = BitConverter.GetBytes((ushort)value);
                    break;

                case TypeCode.UInt32:
                    data = BitConverter.GetBytes((uint)value);
                    break;

                case TypeCode.UInt64:
                    data = BitConverter.GetBytes((ulong)value);
                    break;

                case TypeCode.Char:
                    data = BitConverter.GetBytes((char)value);
                    break;

                case TypeCode.DateTime:
                    data = BitConverter.GetBytes(((DateTime)value).ToBinary());
                    break;

                case TypeCode.Double:
                    data = BitConverter.GetBytes((double)value);
                    break;

                case TypeCode.Single:
                    data = BitConverter.GetBytes((float)value);
                    break;

                default:
#if NETCORE
        		    data = new byte[0];
                    length = 0;
#else
                    using (var ms = new MemoryStream())
                    {
                        bf.Serialize(ms, value);
                        code = TypeCode.Object;
                        data = ms.GetBuffer();
                        length = (int)ms.Length;
                    }
#endif
                    break;
            }

            if (length < 0)
                length = data.Length;

            return new SerializedObjectWrapper((ushort)((ushort)code | 0x0100), new ArraySegment<byte>(data, 0, length));
        }

        /// <summary>
        /// Unwrap object wrapped in <see cref="SerializedObjectWrapper"/>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
		object Unwrap(SerializedObjectWrapper item)
        {
            if (item.Data.Array == null)
                return null;

            if (item.Flags == RawDataFlag)
            {
                ArraySegment<byte> tmp = item.Data;

                if (tmp.Count == tmp.Array.Length)
                    return tmp.Array;

                // we should never arrive here, but it's better to be safe than sorry
                var retval = new byte[tmp.Count];

                Array.Copy(tmp.Array, tmp.Offset, retval, 0, tmp.Count);

                return retval;
            }

            var code = (TypeCode)(item.Flags & 0x00ff);

            byte[] data = item.Data.Array;
            int offset = item.Data.Offset;
            int count = item.Data.Count;

            switch (code)
            {
                // incrementing a non-existing key then getting it
                // returns as a string, but the flag will be 0
                // so treat all 0 flagged items as string
                // this may help inter-client data management as well
                //
                // however we store 'null' as Empty + an empty array, 
                // so this must special-cased for compatibilty with 
                // earlier versions. we introduced DBNull as null marker in emc2.6
                case TypeCode.Empty:
                    return (data == null || count == 0)
                            ? null
                            : Encoding.UTF8.GetString(data, offset, count);

                case TypeCode.DBNull:
                    return null;

                case TypeCode.String:
                    return Encoding.UTF8.GetString(data, offset, count);

                case TypeCode.Boolean:
                    return BitConverter.ToBoolean(data, offset);

                case TypeCode.Int16:
                    return BitConverter.ToInt16(data, offset);

                case TypeCode.Int32:
                    return BitConverter.ToInt32(data, offset);

                case TypeCode.Int64:
                    return BitConverter.ToInt64(data, offset);

                case TypeCode.UInt16:
                    return BitConverter.ToUInt16(data, offset);

                case TypeCode.UInt32:
                    return BitConverter.ToUInt32(data, offset);

                case TypeCode.UInt64:
                    return BitConverter.ToUInt64(data, offset);

                case TypeCode.Char:
                    return BitConverter.ToChar(data, offset);

                case TypeCode.DateTime:
                    return DateTime.FromBinary(BitConverter.ToInt64(data, offset));

                case TypeCode.Double:
                    return BitConverter.ToDouble(data, offset);

                case TypeCode.Single:
                    return BitConverter.ToSingle(data, offset);

                case TypeCode.Object:
                    using (var ms = new MemoryStream(data, offset, count))
                    {
#if NETCORE
	            		return null;
#else
                        return bf.Deserialize(ms);
#endif
                    }

                default: throw new InvalidOperationException("Unknown TypeCode was returned: " + code);
            }
        }
    }
}
