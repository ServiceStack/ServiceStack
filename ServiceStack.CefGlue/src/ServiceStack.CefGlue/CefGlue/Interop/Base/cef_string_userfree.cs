namespace Xilium.CefGlue.Interop
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// It is sometimes necessary for the system to allocate string structures with
    /// the expectation that the user will free them. The userfree types act as a
    /// hint that the user is responsible for freeing the structure.
    /// </summary>
    /// <remarks>
    /// <c>cef_string_userfree*</c> === <c>cef_string_userfree_t</c>.
    /// </remarks>
    internal unsafe struct cef_string_userfree
    {
        public static string ToString(cef_string_userfree* str)
        {
            if (str != null)
            {
                var result = cef_string_t.ToString((cef_string_t*)str);
                libcef.string_userfree_free(str);
                return result;
            }

            return null;
        }

        /*
        public static int GetLength(cef_string_userfree* str)
        {
            return ((cef_string_t*)str)->length;
        }

        internal static char GetFirstCharOrDefault(cef_string_userfree* str)
        {
            var str_t = ((cef_string_t*)str);
            if (str_t->length > 0)
            {
                return *(str_t->str);
            }
            return '\x0';
        }

        public static void Free(cef_string_userfree* str)
        {
            if (str != null)
            {
                NativeMethods.cef_string_userfree_free(str);
            }
        }

        public static string GetString(cef_string_userfree* str)
        {
            if (str != null)
            {
                return cef_string_t.ToString((cef_string_t*)str);
            }
            else
            {
                return null;
            }
        }

        public static string GetStringAndFree(cef_string_userfree* str)
        {
            if (str != null)
            {
                var result = cef_string_t.ToString((cef_string_t*)str);
                NativeMethods.cef_string_userfree_free(str);
                return result;
            }
            else
            {
                return null;
            }
        }
        */
    }
}
