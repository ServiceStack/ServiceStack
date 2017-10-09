// -----------------------------------------------------------------------
//   <copyright file="ByteArrayKeyComparer.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace Wire.Internal
{
    /// <summary>
    ///     By default ByteArrayKey overrides "public bool Equals(object obj)" to do comparisons.
    ///     But this causes boxing/allocations, so by having a custom comparer we can prevent that.
    /// </summary>
    internal class ByteArrayKeyComparer : IEqualityComparer<ByteArrayKey>
    {
        public static readonly ByteArrayKeyComparer Instance = new ByteArrayKeyComparer();

        public bool Equals(ByteArrayKey x, ByteArrayKey y)
        {
            return ByteArrayKey.Compare(x.Bytes, y.Bytes);
        }

        public int GetHashCode(ByteArrayKey obj)
        {
            return obj.GetHashCode();
        }
    }
}