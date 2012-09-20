namespace ServiceStack.ServiceInterface.ServiceModel
{
    /// <summary>
    /// Generic ResponseStatus for when Response Type can't be inferred
    /// </summary>
    public class ErrorResponse
    {
        public ResponseStatus ResponseStatus { get; set; } 
    }
}