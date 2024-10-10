#if NETFRAMEWORK
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
#endif

namespace StackExchange.Profiling.Internal
{
    /// <summary>
    /// Internal MiniProfiler extensions, not meant for consumption.
    /// This can and probably will break without warning. Don't use the .Internal namespace directly.
    /// 
    /// Shim class covering for AsyncLocal{T} in pre-.NET 4.6 which didn't have it.
    /// </summary>
    /// <typeparam name="T">The type of data to store.</typeparam>
    public class FlowData<T>
    {
#if NETFRAMEWORK
        // Key specific to this type.
#pragma warning disable RCS1158 // Avoid static members in generic types.
        private static readonly string _key = typeof(FlowData<T>).FullName;
#pragma warning restore RCS1158  // Avoid static members in generic types.

        /// <summary>
        /// Gets or sets the value of the ambient data.
        /// </summary>
        public T Value
        {
            get
            {
                var handle = CallContext.LogicalGetData(_key) as ObjectHandle;
                return handle != null
                    ? (T)handle.Unwrap()
                    : default(T);
            }
            set { CallContext.LogicalSetData(_key, new ObjectHandle(value)); }
        }
#else
        private readonly AsyncLocal<T> _backing = new AsyncLocal<T>();

        /// <summary>
        /// Gets or sets the value of the ambient data.
        /// </summary>
        public T Value
        {
            get => _backing.Value;
            set => _backing.Value = value;
        }
#endif
    }
}