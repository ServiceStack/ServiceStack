namespace ServiceStack.NativeTypes.Swift;

/// <summary>
/// Decorate generation of type
/// </summary>
public class SwiftTypeConverter
{
    public SwiftTypeConverter()
    {
        DecodeMethod = "convertIfPresent";
        EncodeMethod = "encode";
    }

    /// <summary>
    /// Attribute (e.g. @propertyWrapper) to decorate the property with
    /// </summary>
    public string Attribute { get; set; }
        
    /// <summary>
    /// Name of KeyedDecodingContainer extension method to call to decode the property value, e.g. convertIfPresent
    /// </summary>
    public string DecodeMethod { get; set;  }
        
    /// <summary>
    /// Name of KeyedDecodingContainer extension method to call to decode the property value, e.g. encode
    /// </summary>
    public string EncodeMethod { get; set;  }
}