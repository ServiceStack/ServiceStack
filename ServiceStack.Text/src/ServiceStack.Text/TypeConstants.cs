using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace ServiceStack
{
    public static class TypeConstants
    {
        static TypeConstants()
        {
            ZeroTask = InTask(0);
            TrueTask = InTask(true);
            FalseTask = InTask(false);
            EmptyTask = InTask((object)null);
        }

        private static Task<T> InTask<T>(this T result)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(result);
            return tcs.Task;
        }

        public static readonly Task<int> ZeroTask;
        public static readonly Task<bool> TrueTask;
        public static readonly Task<bool> FalseTask;
        public static readonly Task<object> EmptyTask;

        public static readonly object EmptyObject = new object();
        
        public const char NonWidthWhiteSpace = (char)0x200B; //Use zero-width space marker to capture empty string
        public static char[] NonWidthWhiteSpaceChars = { (char)0x200B };
        
        public static ReadOnlySpan<char> NullStringSpan => default;
        public static ReadOnlySpan<char> EmptyStringSpan => new(NonWidthWhiteSpaceChars);

        public static ReadOnlyMemory<char> NullStringMemory => default;
        public static ReadOnlyMemory<char> EmptyStringMemory => "".AsMemory();

        public static readonly string[] EmptyStringArray = Array.Empty<string>();
        public static readonly long[] EmptyLongArray = Array.Empty<long>();
        public static readonly int[] EmptyIntArray = Array.Empty<int>();
        public static readonly char[] EmptyCharArray = Array.Empty<char>();
        public static readonly bool[] EmptyBoolArray = Array.Empty<bool>();
        public static readonly byte[] EmptyByteArray = Array.Empty<byte>();
        public static readonly object[] EmptyObjectArray = Array.Empty<object>();
        public static readonly Type[] EmptyTypeArray = Type.EmptyTypes;
        public static readonly FieldInfo[] EmptyFieldInfoArray = Array.Empty<FieldInfo>();
        public static readonly PropertyInfo[] EmptyPropertyInfoArray = Array.Empty<PropertyInfo>();

        public static readonly byte[][] EmptyByteArrayArray = Array.Empty<byte[]>();

        public static readonly Dictionary<string, string> EmptyStringDictionary = new(0);
        public static readonly Dictionary<string, object> EmptyObjectDictionary = new();
        public static readonly List<Dictionary<string, object>> EmptyObjectDictionaryList = new();

        public static readonly List<string> EmptyStringList = new(0);
        public static readonly List<long> EmptyLongList = new(0);
        public static readonly List<int> EmptyIntList = new(0);
        public static readonly List<char> EmptyCharList = new(0);
        public static readonly List<bool> EmptyBoolList = new(0);
        public static readonly List<byte> EmptyByteList = new(0);
        public static readonly List<object> EmptyObjectList = new(0);
        public static readonly List<Type> EmptyTypeList = new(0);
        public static readonly List<FieldInfo> EmptyFieldInfoList = new(0);
        public static readonly List<PropertyInfo> EmptyPropertyInfoList = new(0);
    }

    public static class TypeConstants<T>
    {
        public static readonly T[] EmptyArray = Array.Empty<T>();
        public static readonly List<T> EmptyList = new(0);
        public static readonly HashSet<T> EmptyHashSet = new();
    }
}