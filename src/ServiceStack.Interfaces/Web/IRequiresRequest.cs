namespace ServiceStack.Web
{
    /// <summary>
    /// Implement on services that need access to the RequestContext
    /// </summary>
    public interface IRequiresRequest
    {
        IRequest Request { get; set; }
    }
}