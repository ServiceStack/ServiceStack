namespace ServiceStack;

/// <summary>
/// JS Functions available in UIs 
/// </summary>
public static class FormatMethods
{
    /// <summary>
    /// USD Currency
    /// </summary>
    public const string Currency = "currency";
    
    /// <summary>
    /// Human Readable File Size
    /// </summary>
    public const string Bytes = "bytes";
    
    /// <summary>
    /// Render Image URL as an img icon
    /// </summary>
    public const string Icon = "icon";
    
    /// <summary>
    /// Render Image URL as an img icon
    /// </summary>
    public const string IconRounded = "iconRounded";

    /// <summary>
    /// Register download link containing file name and file extension icon  
    /// </summary>
    public const string Attachment = "attachment";
    
    /// <summary>
    /// Linkify URLs
    /// options: added as attributes to &lt;a&gt; element, use 'cls' or 'className' for class attribute.
    /// </summary>
    public const string Link = "link";
    
    /// <summary>
    /// Linkify Emails with mailto:
    /// options: {subject:'Subject',body:'Email Body'} all other options added as HTML attributes
    /// </summary>
    public const string LinkEmail = "linkMailTo";
    
    /// <summary>
    /// Linkify Phone number with tel:
    /// options: added as attributes to &lt;a&gt; element, use 'cls' or 'className' for class attribute.
    /// </summary>
    public const string LinkPhone = "linkTel";
    
    /// <summary>
    /// Format Enum Flags into expanded enum strings 
    /// </summary>
    public const string EnumFlags = "enumFlags";
    
    /// <summary>
    /// Hides field from being displayed in search results
    /// </summary>
    public const string Hidden = "hidden";
}