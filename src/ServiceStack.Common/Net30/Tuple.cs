#region (c)2009 Lokad - New BSD license

// Copyright (c) Lokad 2009 
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence

#endregion

#if !NET_4_0 && !SILVERLIGHT && !MONOTOUCH && !XBOX

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ServiceStack.Net30
{
    public static class SystemUtil
    {
        internal static int GetHashCode(params object[] args)
        {
            unchecked
            {
                int result = 0;
                foreach (var o in args)
                {
                    result = (result * 397) ^ (o != null ? o.GetHashCode() : 0);
                }
                return result;
            }
        }		
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class ImmutableAttribute : Attribute
    {
    }

    /// <summary>
    /// Helper extensions for tuples
    /// </summary>
    public static class ExtendTuple
    {
        public static Triple<T1, T2, T3> Append<T1, T2, T3>(this Tuple<T1, T2> tuple, T3 item)
        {
            return Tuple.From(tuple.Item1, tuple.Item2, item);
        }

        public static Quad<T1, T2, T3, T4> Append<T1, T2, T3, T4>(this Tuple<T1, T2, T3> tuple, T4 item)
        {
            return Tuple.From(tuple.Item1, tuple.Item2, tuple.Item3, item);
        }

        public static void AddTuple<T1, T2>(this ICollection<Tuple<T1, T2>> collection, T1 first, T2 second)
        {
            collection.Add(Tuple.From(first, second));
        }

        public static void AddTuple<T1, T2>(this ICollection<Pair<T1, T2>> collection, T1 first, T2 second)
        {
            collection.Add(Tuple.From(first, second));
        }

        public static void AddTuple<T1, T2, T3>(this ICollection<Tuple<T1, T2, T3>> collection, T1 first, T2 second, T3 third)
        {
            collection.Add(Tuple.From(first, second, third));
        }

        public static void AddTuple<T1, T2, T3, T4>(this ICollection<Tuple<T1, T2, T3, T4>> collection, T1 first, T2 second,
                                                    T3 third, T4 fourth)
        {
            collection.Add(Tuple.From(first, second, third, fourth));
        }

    }
    
    [Serializable]
    [Immutable]
    public sealed class Pair<TKey, TValue> : Tuple<TKey, TValue>
    {
        public Pair(TKey first, TValue second) : base(first, second) {}

        public TKey Key
        {
            get { return Item1; }
        }

        public TValue Value
        {
            get { return Item2; }
        }
    }

    [Serializable]
    [Immutable]
    public sealed class Quad<T1, T2, T3, T4> : Tuple<T1, T2, T3, T4>
    {
        public Quad(T1 first, T2 second, T3 third, T4 fourth) : base(first, second, third, fourth)
        {
        }
    }

    [Serializable]
    [Immutable]
    public sealed class Triple<T1, T2, T3> : Tuple<T1, T2, T3>
    {
        public Triple(T1 first, T2 second, T3 third)
            : base(first, second, third)
        {
        }
    }

    [Serializable]
    [Immutable]
    [DebuggerDisplay("({Item1},{Item2})")]
    public class Tuple<T1, T2> : IEquatable<Tuple<T1, T2>>
    {
        readonly T1 _item1;

        public T1 Item1
        {
            get { return _item1; }
        }

        readonly T2 _item2;

        public T2 Item2
        {
            get { return _item2; }
        }

        public Tuple(T1 first, T2 second)
        {
            _item1 = first;
            _item2 = second;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                throw new NullReferenceException("obj is null");
            if (ReferenceEquals(this, obj)) return true;
            if (!(obj is Tuple<T1, T2>)) return false;
            return Equals((Tuple<T1, T2>)obj);
        }

        public override string ToString()
        {
            return string.Format("({0},{1})", Item1, Item2);
        }

        public bool Equals(Tuple<T1, T2> obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj.Item1, Item1) && Equals(obj.Item2, Item2);
        }

        public override int GetHashCode()
        {
            return SystemUtil.GetHashCode(Item1, Item2);
        }

        public static bool operator ==(Tuple<T1, T2> left, Tuple<T1, T2> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Tuple<T1, T2> left, Tuple<T1, T2> right)
        {
            return !Equals(left, right);
        }
    }

    [Serializable]
    [DebuggerDisplay("({Item1},{Item2},{Item3})")]
    public class Tuple<T1, T2, T3> : IEquatable<Tuple<T1, T2, T3>>
    {
        readonly T1 _item1;

        public T1 Item1
        {
            get { return _item1; }
        }

        readonly T2 _item2;

        public T2 Item2
        {
            get { return _item2; }
        }

        readonly T3 _item3;

        public T3 Item3
        {
            get { return _item3; }
        }

        public Tuple(T1 first, T2 second, T3 third)
        {
            _item1 = first;
            _item2 = second;
            _item3 = third;
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", Item1, Item2, Item3);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                throw new NullReferenceException("obj is null");
            if (ReferenceEquals(this, obj)) return true;
            if (!(obj is Tuple<T1, T2, T3>)) return false;
            return Equals((Tuple<T1, T2, T3>)obj);
        }

        public bool Equals(Tuple<T1, T2, T3> obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj.Item1, Item1) && Equals(obj.Item2, Item2) && Equals(obj.Item3, Item3);
        }

        public override int GetHashCode()
        {
            return SystemUtil.GetHashCode(Item1, Item2, Item3);
        }

        public static bool operator ==(Tuple<T1, T2, T3> left, Tuple<T1, T2, T3> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Tuple<T1, T2, T3> left, Tuple<T1, T2, T3> right)
        {
            return !Equals(left, right);
        }
    }

    [Serializable]
    [DebuggerDisplay("({Item1},{Item2},{Item3},{Item4})")]
    [Immutable]
    public class Tuple<T1, T2, T3, T4> : IEquatable<Tuple<T1, T2, T3, T4>>
    {
        readonly T1 _item1;

        public T1 Item1
        {
            get { return _item1; }
        }

        readonly T2 _item2;

        public T2 Item2
        {
            get { return _item2; }
        }

        readonly T3 _item3;

        public T3 Item3
        {
            get { return _item3; }
        }

        readonly T4 _item4;

        public T4 Item4
        {
            get { return _item4; }
        }

        public Tuple(T1 first, T2 second, T3 third, T4 fourth)
        {
            _item1 = first;
            _item2 = second;
            _item3 = third;
            _item4 = fourth;
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2},{3})", Item1, Item2, Item3, Item4);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                throw new NullReferenceException("obj is null");
            if (ReferenceEquals(this, obj)) return true;
            if (!(obj is Tuple<T1, T2, T3, T4>)) return false;
            return Equals((Tuple<T1, T2, T3, T4>)obj);
        }

        public bool Equals(Tuple<T1, T2, T3, T4> obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj.Item1, Item1)
                && Equals(obj.Item2, Item2)
                    && Equals(obj.Item3, Item3)
                        && Equals(obj.Item4, Item4);
        }

        public override int GetHashCode()
        {
            return SystemUtil.GetHashCode(Item1, Item2, Item3, Item4);
        }

        public static bool operator ==(Tuple<T1, T2, T3, T4> left, Tuple<T1, T2, T3, T4> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Tuple<T1, T2, T3, T4> left, Tuple<T1, T2, T3, T4> right)
        {
            return !Equals(left, right);
        }
    }

    public static class Tuple
    {
        public static Pair<T1, T2> From<T1, T2>(T1 first, T2 second)
        {
            return new Pair<T1, T2>(first, second);
        }

        public static Tuple<T1, T2> Create<T1, T2>(T1 first, T2 second)
        {
            return new Pair<T1, T2>(first, second);
        }

        public static Triple<T1, T2, T3> From<T1, T2, T3>(T1 first, T2 second, T3 third)
        {
            return new Triple<T1, T2, T3>(first, second, third);
        }

        public static Tuple<T1, T2, T3> Create<T1, T2, T3>(T1 first, T2 second, T3 third)
        {
            return new Triple<T1, T2, T3>(first, second, third);
        }

        public static Quad<T1, T2, T3, T4> From<T1, T2, T3, T4>(T1 first, T2 second, T3 third, T4 fourth)
        {
            return new Quad<T1, T2, T3, T4>(first, second, third, fourth);
        }

        public static Tuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 first, T2 second, T3 third, T4 fourth)
        {
            return new Quad<T1, T2, T3, T4>(first, second, third, fourth);
        }
    }

}


#endif

