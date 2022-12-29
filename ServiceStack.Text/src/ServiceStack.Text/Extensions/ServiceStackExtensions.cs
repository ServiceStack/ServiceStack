using System;

namespace ServiceStack.Extensions
{
    /// <summary>
    /// Move conflicting extension methods into 'ServiceStack.Extensions' namespace
    /// </summary>
    public static class ServiceStackExtensions
    {
        //Ambiguous definitions in .NET Core 3.0 System MemoryExtensions.cs 
        public static ReadOnlyMemory<char> Trim(this ReadOnlyMemory<char> span)
        {
            return span.TrimStart().TrimEnd();
        }

        public static ReadOnlyMemory<char> TrimStart(this ReadOnlyMemory<char> value)
        {
            if (value.IsEmpty) return TypeConstants.NullStringMemory;
            var span = value.Span;
            int start = 0;
            for (; start < span.Length; start++)
            {
                if (!char.IsWhiteSpace(span[start]))
                    break;
            }
            return value.Slice(start);
        }

        public static ReadOnlyMemory<char> TrimEnd(this ReadOnlyMemory<char> value)
        {
            if (value.IsEmpty) return TypeConstants.NullStringMemory;
            var span = value.Span;
            int end = span.Length - 1;
            for (; end >= 0; end--)
            {
                if (!char.IsWhiteSpace(span[end]))
                    break;
            }
            return value.Slice(0, end + 1);
        }
    }
}