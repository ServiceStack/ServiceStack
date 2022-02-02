namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class used to represent post data for a web request. The methods of this
    /// class may be called on any thread.
    /// </summary>
    public sealed unsafe partial class CefPostData
    {
        /// <summary>
        /// Create a new CefPostData object.
        /// </summary>
        public static CefPostData Create()
        {
            return CefPostData.FromNative(
                cef_post_data_t.create()
                );
        }

        /// <summary>
        /// Returns true if this object is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return cef_post_data_t.is_read_only(_self) != 0; }
        }

        /// <summary>
        /// Returns true if the underlying POST data includes elements that are not
        /// represented by this CefPostData object (for example, multi-part file upload
        /// data). Modifying CefPostData objects with excluded elements may result in
        /// the request failing.
        /// </summary>
        public bool HasExcludedElements
        {
            get { return cef_post_data_t.has_excluded_elements(_self) != 0; }
        }

        /// <summary>
        /// Returns the number of existing post data elements.
        /// </summary>
        public int Count
        {
            get { return (int)cef_post_data_t.get_element_count(_self); }
        }

        /// <summary>
        /// Retrieve the post data elements.
        /// </summary>
        public CefPostDataElement[] GetElements()
        {
            // FIXME: CefPostDataElement.GetElements(): check CEF C API impl
            var count = Count;
            if (count == 0) return new CefPostDataElement[0];

            UIntPtr n_elementsCount = (UIntPtr)count;
            var n_elements = new cef_post_data_element_t*[count];
            fixed (cef_post_data_element_t** n_elements_ptr = n_elements)
            {
                cef_post_data_t.get_elements(_self, &n_elementsCount, n_elements_ptr);
                if ((int)n_elementsCount > count) throw new InvalidOperationException();
            }

            count = (int)n_elementsCount;
            var elements = new CefPostDataElement[count];
            for (var i = 0; i < count; i++)
            {
                elements[i] = CefPostDataElement.FromNative(n_elements[i]);
            }

            return elements;
        }

        /// <summary>
        /// Remove the specified post data element.  Returns true if the removal
        /// succeeds.
        /// </summary>
        public bool Remove(CefPostDataElement element)
        {
            return cef_post_data_t.remove_element(_self, element.ToNative()) != 0;
        }

        /// <summary>
        /// Add the specified post data element.  Returns true if the add succeeds.
        /// </summary>
        public bool Add(CefPostDataElement element)
        {
            return cef_post_data_t.add_element(_self, element.ToNative()) != 0;
        }

        /// <summary>
        /// Remove all existing post data elements.
        /// </summary>
        public void RemoveAll()
        {
            cef_post_data_t.remove_elements(_self);
        }
    }
}
