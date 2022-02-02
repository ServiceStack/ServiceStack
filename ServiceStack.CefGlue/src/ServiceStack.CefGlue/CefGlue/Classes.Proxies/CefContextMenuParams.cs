namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Provides information about the context menu state. The ethods of this class
    /// can only be accessed on browser process the UI thread.
    /// </summary>
    public sealed unsafe partial class CefContextMenuParams
    {
        /// <summary>
        /// Returns the X coordinate of the mouse where the context menu was invoked.
        /// Coords are relative to the associated RenderView's origin.
        /// </summary>
        public int X
        {
            get { return cef_context_menu_params_t.get_xcoord(_self); }
        }

        /// <summary>
        /// Returns the Y coordinate of the mouse where the context menu was invoked.
        /// Coords are relative to the associated RenderView's origin.
        /// </summary>
        public int Y
        {
            get { return cef_context_menu_params_t.get_ycoord(_self); }
        }

        /// <summary>
        /// Returns flags representing the type of node that the context menu was
        /// invoked on.
        /// </summary>
        public CefContextMenuTypeFlags ContextMenuType
        {
            get { return cef_context_menu_params_t.get_type_flags(_self); }
        }

        /// <summary>
        /// Returns the URL of the link, if any, that encloses the node that the
        /// context menu was invoked on.
        /// </summary>
        public string LinkUrl
        {
            get
            {
                var n_result = cef_context_menu_params_t.get_link_url(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the link URL, if any, to be used ONLY for "copy link address". We
        /// don't validate this field in the frontend process.
        /// </summary>
        public string UnfilteredLinkUrl
        {
            get
            {
                var n_result = cef_context_menu_params_t.get_unfiltered_link_url(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the source URL, if any, for the element that the context menu was
        /// invoked on. Example of elements with source URLs are img, audio, and video.
        /// </summary>
        public string SourceUrl
        {
            get
            {
                var n_result = cef_context_menu_params_t.get_source_url(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns true if the context menu was invoked on an image which has
        /// non-empty contents.
        /// </summary>
        public bool HasImageContents
        {
            get { return cef_context_menu_params_t.has_image_contents(_self) != 0; }
        }

        /// <summary>
        /// Returns the title text or the alt text if the context menu was invoked on
        /// an image.
        /// </summary>
        public string TitleText
        {
            get
            {
                var n_result = cef_context_menu_params_t.get_title_text(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the URL of the top level page that the context menu was invoked on.
        /// </summary>
        public string PageUrl
        {
            get
            {
                var n_result = cef_context_menu_params_t.get_page_url(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the URL of the subframe that the context menu was invoked on.
        /// </summary>
        public string FrameUrl
        {
            get
            {
                var n_result = cef_context_menu_params_t.get_frame_url(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the character encoding of the subframe that the context menu was
        /// invoked on.
        /// </summary>
        public string FrameCharset
        {
            get
            {
                var n_result = cef_context_menu_params_t.get_frame_charset(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the type of context node that the context menu was invoked on.
        /// </summary>
        public CefContextMenuMediaType MediaType
        {
            get { return cef_context_menu_params_t.get_media_type(_self); }
        }

        /// <summary>
        /// Returns flags representing the actions supported by the media element, if
        /// any, that the context menu was invoked on.
        /// </summary>
        public CefContextMenuMediaStateFlags MediaState
        {
            get { return cef_context_menu_params_t.get_media_state_flags(_self); }
        }

        /// <summary>
        /// Returns the text of the selection, if any, that the context menu was
        /// invoked on.
        /// </summary>
        public string SelectionText
        {
            get
            {
                var n_result = cef_context_menu_params_t.get_selection_text(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the text of the misspelled word, if any, that the context menu was
        /// invoked on.
        /// </summary>
        public string GetMisspelledWord()
        {
            var n_result = cef_context_menu_params_t.get_misspelled_word(_self);
            return cef_string_userfree.ToString(n_result);
        }

        /// <summary>
        /// Returns true if suggestions exist, false otherwise. Fills in |suggestions|
        /// from the spell check service for the misspelled word if there is one.
        /// </summary>
        public string[] GetDictionarySuggestions()
        {
            var n_suggestions = libcef.string_list_alloc();
            cef_context_menu_params_t.get_dictionary_suggestions(_self, n_suggestions);
            var suggestions = cef_string_list.ToArray(n_suggestions);
            libcef.string_list_free(n_suggestions);
            return suggestions;
        }

        /// <summary>
        /// Returns true if the context menu was invoked on an editable node.
        /// </summary>
        public bool IsEditable
        {
            get { return cef_context_menu_params_t.is_editable(_self) != 0; }
        }

        /// <summary>
        /// Returns true if the context menu was invoked on an editable node where
        /// spell-check is enabled.
        /// </summary>
        public bool IsSpellCheckEnabled
        {
            get { return cef_context_menu_params_t.is_spell_check_enabled(_self) != 0; }
        }

        /// <summary>
        /// Returns flags representing the actions supported by the editable node, if
        /// any, that the context menu was invoked on.
        /// </summary>
        public CefContextMenuEditStateFlags EditState
        {
            get { return cef_context_menu_params_t.get_edit_state_flags(_self); }
        }

        /// <summary>
        /// Returns true if the context menu contains items specified by the renderer
        /// process (for example, plugin placeholder or pepper plugin menu items).
        /// </summary>
        public bool IsCustomMenu
        {
            get { return cef_context_menu_params_t.is_custom_menu(_self) != 0; }
        }

        /// <summary>
        /// Returns true if the context menu was invoked from a pepper plugin.
        /// </summary>
        public bool IsPepperMenu
        {
            get { return cef_context_menu_params_t.is_pepper_menu(_self) != 0; }
        }
    }
}
