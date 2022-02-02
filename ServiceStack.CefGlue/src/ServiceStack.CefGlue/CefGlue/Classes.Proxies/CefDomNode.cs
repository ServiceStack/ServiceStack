namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class used to represent a DOM node. The methods of this class should only be
    /// called on the render process main thread.
    /// </summary>
    public sealed unsafe partial class CefDomNode
    {
        /// <summary>
        /// Returns the type for this node.
        /// </summary>
        public CefDomNodeType NodeType
        {
            get { return cef_domnode_t.get_type(_self); }
        }

        /// <summary>
        /// Returns true if this is a text node.
        /// </summary>
        public bool IsText
        {
            get { return cef_domnode_t.is_text(_self) != 0; }
        }

        /// <summary>
        /// Returns true if this is an element node.
        /// </summary>
        public bool IsElement
        {
            get { return cef_domnode_t.is_element(_self) != 0; }
        }

        /// <summary>
        /// Returns true if this is an editable node.
        /// </summary>
        public bool IsEditable
        {
            get { return cef_domnode_t.is_editable(_self) != 0; }
        }

        /// <summary>
        /// Returns true if this is a form control element node.
        /// </summary>
        public bool IsFormControlElement
        {
            get { return cef_domnode_t.is_form_control_element(_self) != 0; }
        }

        /// <summary>
        /// Returns the type of this form control element node.
        /// </summary>
        public string FormControlElementType
        {
            get
            {
                var n_result = cef_domnode_t.get_form_control_element_type(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns true if this object is pointing to the same handle as |that|
        /// object.
        /// </summary>
        public bool IsSame(CefDomNode that)
        {
            return cef_domnode_t.is_same(_self, that.ToNative()) != 0;
        }

        /// <summary>
        /// Returns the name of this node.
        /// </summary>
        public string Name
        {
            get
            {
                var n_result = cef_domnode_t.get_name(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the value of this node.
        /// </summary>
        public string Value
        {
            get
            {
                var n_result = cef_domnode_t.get_value(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Set the value of this node. Returns true on success.
        /// </summary>
        public bool SetValue(string value)
        {
            fixed (char* value_str = value)
            {
                var n_value = new cef_string_t(value_str, value != null ? value.Length : 0);

                return cef_domnode_t.set_value(_self, &n_value) != 0;
            }
        }

        /// <summary>
        /// Returns the contents of this node as markup.
        /// </summary>
        public string GetAsMarkup()
        {
            var n_result = cef_domnode_t.get_as_markup(_self);
            return cef_string_userfree.ToString(n_result);
        }

        /// <summary>
        /// Returns the document associated with this node.
        /// </summary>
        public CefDomDocument Document
        {
            get
            {
                return CefDomDocument.FromNative(
                    cef_domnode_t.get_document(_self)
                    );
            }
        }

        /// <summary>
        /// Returns the parent node.
        /// </summary>
        public CefDomNode Parent
        {
            get
            {
                return CefDomNode.FromNativeOrNull(
                    cef_domnode_t.get_parent(_self)
                    );
            }
        }

        /// <summary>
        /// Returns the previous sibling node.
        /// </summary>
        public CefDomNode PreviousSibling
        {
            get
            {
                return CefDomNode.FromNativeOrNull(
                    cef_domnode_t.get_previous_sibling(_self)
                    );
            }
        }

        /// <summary>
        /// Returns the next sibling node.
        /// </summary>
        public CefDomNode NextSibling
        {
            get
            {
                return CefDomNode.FromNativeOrNull(
                    cef_domnode_t.get_next_sibling(_self)
                    );
            }
        }

        /// <summary>
        /// Returns true if this node has child nodes.
        /// </summary>
        public bool HasChildren
        {
            get { return cef_domnode_t.has_children(_self) != 0; }
        }

        /// <summary>
        /// Return the first child node.
        /// </summary>
        public CefDomNode FirstChild
        {
            get
            {
                return CefDomNode.FromNativeOrNull(
                    cef_domnode_t.get_first_child(_self)
                    );
            }
        }

        /// <summary>
        /// Returns the last child node.
        /// </summary>
        public CefDomNode LastChild
        {
            get
            {
                return CefDomNode.FromNativeOrNull(
                    cef_domnode_t.get_last_child(_self)
                    );
            }
        }

        /// <summary>
        /// The following methods are valid only for element nodes.
        /// Returns the tag name of this element.
        /// </summary>
        public string ElementTagName
        {
            get
            {
                var n_result = cef_domnode_t.get_element_tag_name(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns true if this element has attributes.
        /// </summary>
        public bool HasAttributes
        {
            get { return cef_domnode_t.has_element_attributes(_self) != 0; }
        }

        /// <summary>
        /// Returns true if this element has an attribute named |attrName|.
        /// </summary>
        public bool HasAttribute(string attrName)
        {
            fixed (char* attrName_str = attrName)
            {
                var n_attrName = new cef_string_t(attrName_str, attrName.Length);
                return cef_domnode_t.has_element_attribute(_self, &n_attrName) != 0;
            }
        }

        /// <summary>
        /// Returns the element attribute named |attrName|.
        /// </summary>
        public string GetAttribute(string attrName)
        {
            fixed (char* attrName_str = attrName)
            {
                var n_attrName = new cef_string_t(attrName_str, attrName.Length);
                var n_result = cef_domnode_t.get_element_attribute(_self, &n_attrName);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns a map of all element attributes.
        /// </summary>
        public IDictionary<string, string> GetAttributes()
        {
            var attrMap = libcef.string_map_alloc();
            cef_domnode_t.get_element_attributes(_self, attrMap);
            var result = cef_string_map.ToDictionary(attrMap);
            libcef.string_map_free(attrMap);
            return result;
        }

        /// <summary>
        /// Set the value for the element attribute named |attrName|. Returns true on
        /// success.
        /// </summary>
        public bool SetAttribute(string attrName, string value)
        {
            fixed (char* attrName_str = attrName)
            fixed (char* value_str = value)
            {
                var n_attrName = new cef_string_t(attrName_str, attrName.Length);
                var n_value = new cef_string_t(value_str, value != null ? value.Length : 0);
                return cef_domnode_t.set_element_attribute(_self, &n_attrName, &n_value) != 0;
            }
        }

        /// <summary>
        /// Returns the inner text of the element.
        /// </summary>
        public string InnerText
        {
            get
            {
                var n_result = cef_domnode_t.get_element_inner_text(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the bounds of the element.
        /// </summary>
        public CefRectangle GetElementBounds()
        {
            var n_result = cef_domnode_t.get_element_bounds(_self);
            return new CefRectangle(
                n_result.x,
                n_result.y,
                n_result.width,
                n_result.height
                );
        }
    }
}
