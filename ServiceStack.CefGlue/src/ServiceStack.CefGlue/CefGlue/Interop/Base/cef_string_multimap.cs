namespace Xilium.CefGlue.Interop
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;

    [StructLayout(LayoutKind.Sequential, Pack = libcef.ALIGN)]
    internal unsafe struct cef_string_multimap
    {
        public static NameValueCollection ToNameValueCollection(cef_string_multimap* multimap)
        {
            var result = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
            if (multimap == null) return result;

            var size = libcef.string_multimap_size(multimap);

            var n_key = new cef_string_t();
            var n_value = new cef_string_t();
            for (var i = 0; i < size; i++)
            {
                libcef.string_multimap_key(multimap, i, &n_key);
                libcef.string_multimap_value(multimap, i, &n_value);

                result.Add(cef_string_t.ToString(&n_key), cef_string_t.ToString(&n_value));
            }

            libcef.string_clear(&n_key);
            libcef.string_clear(&n_value);

            return result;
        }

        public static cef_string_multimap* From(NameValueCollection collection)
        {
            var result = libcef.string_multimap_alloc();

            foreach (string key in collection)
            {
                fixed (char* key_ptr = key)
                {
                    var n_key = new cef_string_t(key_ptr, key.Length);

                    foreach (var value in collection.GetValues(key))
                    {
                        fixed (char* value_ptr = value)
                        {
                            var n_value = new cef_string_t(value_ptr, value.Length);

                            libcef.string_multimap_append(result, &n_key, &n_value);
                        }
                    }
                }
            }

            return result;
        }
    }
}
