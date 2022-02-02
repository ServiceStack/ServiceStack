namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class that supports the reading of XML data via the libxml streaming API.
    /// The methods of this class should only be called on the thread that creates
    /// the object.
    /// </summary>
    public sealed unsafe partial class CefXmlReader
    {
        /// <summary>
        /// Create a new CefXmlReader object. The returned object's methods can only
        /// be called from the thread that created the object.
        /// </summary>
        public static CefXmlReader Create(CefStreamReader stream, CefXmlEncoding encodingType, string uri)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            fixed (char* uri_str = uri)
            {
                var n_uri = new cef_string_t(uri_str, uri != null ? uri.Length : 0);
                return CefXmlReader.FromNative(
                    cef_xml_reader_t.create(stream.ToNative(), encodingType, &n_uri)
                    );
            }
        }

        /// <summary>
        /// Moves the cursor to the next node in the document. This method must be
        /// called at least once to set the current cursor position. Returns true if
        /// the cursor position was set successfully.
        /// </summary>
        public bool MoveToNextNode()
        {
            return cef_xml_reader_t.move_to_next_node(_self) != 0;
        }

        /// <summary>
        /// Close the document. This should be called directly to ensure that cleanup
        /// occurs on the correct thread.
        /// </summary>
        public bool Close()
        {
            return cef_xml_reader_t.close(_self) != 0;
        }

        /// <summary>
        /// Returns true if an error has been reported by the XML parser.
        /// </summary>
        public bool HasError
        {
            get { return cef_xml_reader_t.has_error(_self) != 0; }
        }

        /// <summary>
        /// Returns the error string.
        /// </summary>
        public string Error
        {
            get
            {
                var n_result = cef_xml_reader_t.get_error(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// The below methods retrieve data for the node at the current cursor
        /// position.
        /// Returns the node type.
        /// </summary>
        public CefXmlNodeType NodeType
        {
            get { return cef_xml_reader_t.get_type(_self); }
        }

        /// <summary>
        /// Returns the node depth. Depth starts at 0 for the root node.
        /// </summary>
        public int Depth
        {
            get { return cef_xml_reader_t.get_depth(_self); }
        }

        /// <summary>
        /// Returns the local name. See
        /// http://www.w3.org/TR/REC-xml-names/#NT-LocalPart for additional details.
        /// </summary>
        public string LocalName
        {
            get
            {
                var n_result = cef_xml_reader_t.get_local_name(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the namespace prefix. See http://www.w3.org/TR/REC-xml-names/ for
        /// additional details.
        /// </summary>
        public string Prefix
        {
            get
            {
                var n_result = cef_xml_reader_t.get_prefix(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the qualified name, equal to (Prefix:)LocalName. See
        /// http://www.w3.org/TR/REC-xml-names/#ns-qualnames for additional details.
        /// </summary>
        public string QualifiedName
        {
            get
            {
                var n_result = cef_xml_reader_t.get_qualified_name(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the URI defining the namespace associated with the node. See
        /// http://www.w3.org/TR/REC-xml-names/ for additional details.
        /// </summary>
        public string NamespaceUri
        {
            get
            {
                var n_result = cef_xml_reader_t.get_namespace_uri(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the base URI of the node. See http://www.w3.org/TR/xmlbase/ for
        /// additional details.
        /// </summary>
        public string BaseUri
        {
            get
            {
                var n_result = cef_xml_reader_t.get_base_uri(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the xml:lang scope within which the node resides. See
        /// http://www.w3.org/TR/REC-xml/#sec-lang-tag for additional details.
        /// </summary>
        public string XmlLang
        {
            get
            {
                var n_result = cef_xml_reader_t.get_xml_lang(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns true if the node represents an empty element. <a/> is considered
        /// empty but <a></a> is not.
        /// </summary>
        public bool IsEmptyElement
        {
            get { return cef_xml_reader_t.is_empty_element(_self) != 0; }
        }

        /// <summary>
        /// Returns true if the node has a text value.
        /// </summary>
        public bool HasValue
        {
            get { return cef_xml_reader_t.has_value(_self) != 0; }
        }

        /// <summary>
        /// Returns the text value.
        /// </summary>
        public string Value
        {
            get
            {
                var n_result = cef_xml_reader_t.get_value(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns true if the node has attributes.
        /// </summary>
        public bool HasAttributes
        {
            get { return cef_xml_reader_t.has_attributes(_self) != 0; }
        }

        /// <summary>
        /// Returns the number of attributes.
        /// </summary>
        public int AttributeCount
        {
            get { return (int)cef_xml_reader_t.get_attribute_count(_self); }
        }

        /// <summary>
        /// Returns the value of the attribute at the specified 0-based index.
        /// </summary>
        public string GetAttribute(int index)
        {
            var n_result = cef_xml_reader_t.get_attribute_byindex(_self, index);
            return cef_string_userfree.ToString(n_result);
        }

        /// <summary>
        /// Returns the value of the attribute with the specified qualified name.
        /// </summary>
        public string GetAttribute(string qualifiedName)
        {
            fixed (char* qualifiedName_str = qualifiedName)
            {
                var n_qualifiedName = new cef_string_t(qualifiedName_str, qualifiedName != null ? qualifiedName.Length : 0);
                var n_result = cef_xml_reader_t.get_attribute_byqname(_self, &n_qualifiedName);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the value of the attribute with the specified local name and
        /// namespace URI.
        /// </summary>
        public string GetAttribute(string localName, string namespaceUri)
        {
            fixed (char* localName_str = localName)
            fixed (char* namespaceUri_str = namespaceUri)
            {
                var n_localName = new cef_string_t(localName_str, localName != null ? localName.Length : 0);
                var n_namespaceUri = new cef_string_t(namespaceUri_str, namespaceUri != null ? namespaceUri.Length : 0);

                var n_result = cef_xml_reader_t.get_attribute_bylname(_self, &n_localName, &n_namespaceUri);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns an XML representation of the current node's children.
        /// </summary>
        public string GetInnerXml()
        {
            var n_result = cef_xml_reader_t.get_inner_xml(_self);
            return cef_string_userfree.ToString(n_result);
        }

        /// <summary>
        /// Returns an XML representation of the current node including its children.
        /// </summary>
        public string GetOuterXml()
        {
            var n_result = cef_xml_reader_t.get_outer_xml(_self);
            return cef_string_userfree.ToString(n_result);
        }

        /// <summary>
        /// Returns the line number for the current node.
        /// </summary>
        public int LineNumber
        {
            get { return cef_xml_reader_t.get_line_number(_self); }
        }

        /// <summary>
        /// Attribute nodes are not traversed by default. The below methods can be
        /// used to move the cursor to an attribute node. MoveToCarryingElement() can
        /// be called afterwards to return the cursor to the carrying element. The
        /// depth of an attribute node will be 1 + the depth of the carrying element.
        /// Moves the cursor to the attribute at the specified 0-based index. Returns
        /// true if the cursor position was set successfully.
        /// </summary>
        public bool MoveToAttribute(int index)
        {
            return cef_xml_reader_t.move_to_attribute_byindex(_self, index) != 0;
        }

        /// <summary>
        /// Moves the cursor to the attribute with the specified qualified name.
        /// Returns true if the cursor position was set successfully.
        /// </summary>
        public bool MoveToAttribute(string qualifiedName)
        {
            fixed (char* qualifiedName_str = qualifiedName)
            {
                var n_qualifiedName = new cef_string_t(qualifiedName_str, qualifiedName != null ? qualifiedName.Length : 0);

                return cef_xml_reader_t.move_to_attribute_byqname(_self, &n_qualifiedName) != 0;
            }
        }

        /// <summary>
        /// Moves the cursor to the attribute with the specified local name and
        /// namespace URI. Returns true if the cursor position was set successfully.
        /// </summary>
        public bool MoveToAttribute(string localName, string namespaceUri)
        {
            fixed (char* localName_str = localName)
            fixed (char* namespaceUri_str = namespaceUri)
            {
                var n_localName = new cef_string_t(localName_str, localName != null ? localName.Length : 0);
                var n_namespaceUri = new cef_string_t(namespaceUri_str, namespaceUri != null ? namespaceUri.Length : 0);

                return cef_xml_reader_t.move_to_attribute_bylname(_self, &n_localName, &n_namespaceUri) != 0;
            }
        }

        /// <summary>
        /// Moves the cursor to the first attribute in the current element. Returns
        /// true if the cursor position was set successfully.
        /// </summary>
        public bool MoveToFirstAttribute()
        {
            return cef_xml_reader_t.move_to_first_attribute(_self) != 0;
        }

        /// <summary>
        /// Moves the cursor to the next attribute in the current element. Returns
        /// true if the cursor position was set successfully.
        /// </summary>
        public bool MoveToNextAttribute()
        {
            return cef_xml_reader_t.move_to_next_attribute(_self) != 0;
        }

        /// <summary>
        /// Moves the cursor back to the carrying element. Returns true if the cursor
        /// position was set successfully.
        /// </summary>
        public bool MoveToCarryingElement()
        {
            return cef_xml_reader_t.move_to_carrying_element(_self) != 0;
        }
    }
}
