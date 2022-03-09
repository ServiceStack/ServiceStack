using System;

namespace ServiceStack;

/** A simple solution to handle FormData Content Type that would otherwise require a ModelBinder
 * https://docs.microsoft.com/en-us/aspnet/core/mvc/advanced/custom-model-binding?view=aspnetcore-6.0
 * That uses MultipartReader to parse the stream reuqest body
 * https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-6.0#upload-large-files-with-streaming
 */

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class MultiPartFieldAttribute : AttributeBase
{
    public string ContentType { get; set; }
    public MultiPartFieldAttribute(string contentType) => ContentType = contentType;

    public Type StringSerializer { get; set; }
    public MultiPartFieldAttribute(Type stringSerializer) => StringSerializer = stringSerializer;
}