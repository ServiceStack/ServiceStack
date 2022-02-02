//
// This file manually written from cef/include/internal/cef_string_multimap.h
//
namespace Xilium.CefGlue.Interop
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;

    internal static unsafe partial class libcef
    {
        // Allocate a new string multimap.
        [DllImport(DllName, EntryPoint = "cef_string_multimap_alloc", CallingConvention = CEF_CALL)]
        public static extern cef_string_multimap* string_multimap_alloc();

        // Return the number of elements in the string multimap.
        [DllImport(DllName, EntryPoint = "cef_string_multimap_size", CallingConvention = CEF_CALL)]
        private static extern UIntPtr string_multimap_size_core(cef_string_multimap* map);

        public static int string_multimap_size(cef_string_multimap* map)
        {
            return checked((int)string_multimap_size_core(map));
        }

        // Return the number of values with the specified key.
        [DllImport(DllName, EntryPoint = "cef_string_multimap_find_count", CallingConvention = CEF_CALL)]
        private static extern UIntPtr string_multimap_find_count_core(cef_string_multimap* map, cef_string_t* key);

        public static int string_multimap_find_count(cef_string_multimap* map, cef_string_t* key)
        {
            return checked((int)string_multimap_find_count_core(map, key));
        }

        // Return the value_index-th value with the specified key.
        [DllImport(DllName, EntryPoint = "cef_string_multimap_enumerate", CallingConvention = CEF_CALL)]
        private static extern int string_multimap_enumerate_core(cef_string_multimap* map, cef_string_t* key, UIntPtr value_index, cef_string_t* value);

        public static int string_multimap_enumerate(cef_string_multimap* map, cef_string_t* key, int value_index, cef_string_t* value)
        {
            return string_multimap_enumerate_core(map, key, checked((UIntPtr)value_index), value);
        }

        // Return the key at the specified zero-based string multimap index.
        [DllImport(DllName, EntryPoint = "cef_string_multimap_key", CallingConvention = CEF_CALL)]
        private static extern int string_multimap_key_core(cef_string_multimap* map, UIntPtr index, cef_string_t* key);

        public static int string_multimap_key(cef_string_multimap* map, int index, cef_string_t* key)
        {
            return string_multimap_key_core(map, checked((UIntPtr)index), key);
        }

        // Return the value at the specified zero-based string multimap index.
        [DllImport(DllName, EntryPoint = "cef_string_multimap_value", CallingConvention = CEF_CALL)]
        private static extern int string_multimap_value_core(cef_string_multimap* map, UIntPtr index, cef_string_t* value);

        public static int string_multimap_value(cef_string_multimap* map, int index, cef_string_t* value)
        {
            return string_multimap_value_core(map, checked((UIntPtr)index), value);
        }

        // Append a new key/value pair at the end of the string multimap.
        [DllImport(DllName, EntryPoint = "cef_string_multimap_append", CallingConvention = CEF_CALL)]
        public static extern int string_multimap_append(cef_string_multimap* map, cef_string_t* key, cef_string_t* value);

        // Clear the string multimap.
        [DllImport(DllName, EntryPoint = "cef_string_multimap_clear", CallingConvention = CEF_CALL)]
        public static extern void string_multimap_clear(cef_string_multimap* map);

        // Free the string multimap.
        [DllImport(DllName, EntryPoint = "cef_string_multimap_free", CallingConvention = CEF_CALL)]
        public static extern void string_multimap_free(cef_string_multimap* map);
    }
}
