namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class used to represent a DOM document. The methods of this class should only
    /// be called on the render process main thread thread.
    /// </summary>
    public sealed unsafe partial class CefDomDocument
    {
        /// <summary>
        /// Returns the document type.
        /// </summary>
        public CefDomDocumentType DocumentType
        {
            get { return cef_domdocument_t.get_type(_self); }
        }

        /// <summary>
        /// Returns the root document node.
        /// </summary>
        public CefDomNode Root
        {
            get
            {
                return CefDomNode.FromNative(
                    cef_domdocument_t.get_document(_self)
                    );
            }
        }

        /// <summary>
        /// Returns the BODY node of an HTML document.
        /// </summary>
        public CefDomNode Body
        {
            get
            {
                return CefDomNode.FromNative(
                    cef_domdocument_t.get_body(_self)
                    );
            }
        }

        /// <summary>
        /// Returns the HEAD node of an HTML document.
        /// </summary>
        public CefDomNode Head
        {
            get
            {
                return CefDomNode.FromNative(
                    cef_domdocument_t.get_head(_self)
                    );
            }
        }

        /// <summary>
        /// Returns the title of an HTML document.
        /// </summary>
        public string Title
        {
            get
            {
                var n_result = cef_domdocument_t.get_title(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the document element with the specified ID value.
        /// </summary>
        public CefDomNode GetElementById(string id)
        {
            fixed (char* id_str = id)
            {
                var n_id = new cef_string_t(id_str, id.Length);
                return CefDomNode.FromNativeOrNull(
                    cef_domdocument_t.get_element_by_id(_self, &n_id)
                    );
            }
        }

        /// <summary>
        /// Returns the node that currently has keyboard focus.
        /// </summary>
        public CefDomNode FocusedNode
        {
            get
            {
                return CefDomNode.FromNativeOrNull(
                    cef_domdocument_t.get_focused_node(_self)
                    );
            }
        }

        /// <summary>
        /// Returns true if a portion of the document is selected.
        /// </summary>
        public bool HasSelection
        {
            get { return cef_domdocument_t.has_selection(_self) != 0; }
        }

        /// <summary>
        /// Returns the selection offset within the start node.
        /// </summary>
        public int SelectionStartOffset
        {
            get { return cef_domdocument_t.get_selection_start_offset(_self); }
        }

        /// <summary>
        /// Returns the selection offset within the end node.
        /// </summary>
        public int GetSelectionEndOffset
        {
            get { return cef_domdocument_t.get_selection_end_offset(_self); }
        }

        /// <summary>
        /// Returns the contents of this selection as markup.
        /// </summary>
        public string SelectionAsMarkup
        {
            get
            {
                var n_result = cef_domdocument_t.get_selection_as_markup(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the contents of this selection as text.
        /// </summary>
        public string GetSelectionAsText
        {
            get
            {
                var n_result = cef_domdocument_t.get_selection_as_text(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the base URL for the document.
        /// </summary>
        public string BaseUrl
        {
            get
            {
                var n_result = cef_domdocument_t.get_base_url(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns a complete URL based on the document base URL and the specified
        /// partial URL.
        /// </summary>
        public string GetCompleteUrl(string partialUrl)
        {
            fixed (char* partialUrl_str = partialUrl)
            {
                var n_partialUrl = new cef_string_t(partialUrl_str, partialUrl.Length);
                var n_result = cef_domdocument_t.get_complete_url(_self, &n_partialUrl);
                return cef_string_userfree.ToString(n_result);
            }
        }
    }
}
