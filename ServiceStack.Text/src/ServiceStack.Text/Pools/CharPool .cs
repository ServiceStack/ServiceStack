using System;

namespace ServiceStack.Text.Pools
{
    public sealed class CharPool
    {
        public static void Flush()
        {
            lock (Pool)
            {
                for (var i = 0; i < Pool.Length; i++)
                    Pool[i] = null;
            }
        }

        private CharPool() { }
        private const int POOL_SIZE = 20;
        public const int BUFFER_LENGTH = 1450; //<= MTU - DJB
        private static readonly CachedBuffer[] Pool = new CachedBuffer[POOL_SIZE];

        public static char[] GetBuffer()
        {
            return GetBuffer(BUFFER_LENGTH);
        }

        public static char[] GetBuffer(int minSize)
        {
            char[] cachedBuff = GetCachedBuffer(minSize);
            return cachedBuff ?? new char[minSize];
        }

        public static char[] GetCachedBuffer(int minSize)
        {
            lock (Pool)
            {
                var bestIndex = -1;
                char[] bestMatch = null;
                for (var i = 0; i < Pool.Length; i++)
                {
                    var buffer = Pool[i];
                    if (buffer == null || buffer.Size < minSize)
                    {
                        continue;
                    }
                    if (bestMatch != null && bestMatch.Length < buffer.Size)
                    {
                        continue;
                    }

                    var tmp = buffer.Buffer;
                    if (tmp == null)
                    {
                        Pool[i] = null;
                    }
                    else
                    {
                        bestMatch = tmp;
                        bestIndex = i;
                    }
                }

                if (bestIndex >= 0)
                {
                    Pool[bestIndex] = null;
                }

                return bestMatch;
            }
        }

        /// <remarks>
        /// https://docs.microsoft.com/en-us/dotnet/framework/configure-apps/file-schema/runtime/gcallowverylargeobjects-element
        /// </remarks>
        private const int MaxcharArraySize = int.MaxValue - 56;

        public static void ResizeAndFlushLeft(ref char[] buffer, int toFitAtLeastchars, int copyFromIndex, int copychars)
        {
            Helpers.DebugAssert(buffer != null);
            Helpers.DebugAssert(toFitAtLeastchars > buffer.Length);
            Helpers.DebugAssert(copyFromIndex >= 0);
            Helpers.DebugAssert(copychars >= 0);

            int newLength = buffer.Length * 2;
            if (newLength < 0)
            {
                newLength = MaxcharArraySize;
            }

            if (newLength < toFitAtLeastchars) newLength = toFitAtLeastchars;

            if (copychars == 0)
            {
                ReleaseBufferToPool(ref buffer);
            }

            var newBuffer = GetCachedBuffer(toFitAtLeastchars) ?? new char[newLength];

            if (copychars > 0)
            {
                Buffer.BlockCopy(buffer, copyFromIndex, newBuffer, 0, copychars);
                ReleaseBufferToPool(ref buffer);
            }

            buffer = newBuffer;
        }

        public static void ReleaseBufferToPool(ref char[] buffer)
        {
            if (buffer == null) return;

            lock (Pool)
            {
                var minIndex = 0;
                var minSize = int.MaxValue;
                for (var i = 0; i < Pool.Length; i++)
                {
                    var tmp = Pool[i];
                    if (tmp == null || !tmp.IsAlive)
                    {
                        minIndex = 0;
                        break;
                    }
                    if (tmp.Size < minSize)
                    {
                        minIndex = i;
                        minSize = tmp.Size;
                    }
                }

                Pool[minIndex] = new CachedBuffer(buffer);
            }

            buffer = null;
        }

        private class CachedBuffer
        {
            private readonly WeakReference _reference;

            public int Size { get; }

            public bool IsAlive => _reference.IsAlive;
            public char[] Buffer => (char[])_reference.Target;

            public CachedBuffer(char[] buffer)
            {
                Size = buffer.Length;
                _reference = new WeakReference(buffer);
            }
        }
    }
}