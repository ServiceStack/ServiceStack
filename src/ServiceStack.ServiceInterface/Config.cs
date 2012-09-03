namespace ServiceStack.ServiceInterface
{
    public class Config
    {
        /// <summary>
        /// Would've preferred to use [assembly: ContractNamespace] attribute but it is not supported in Mono
        /// </summary>
        //public const string DefaultNamespace = "http://schemas.sericestack.net/examples/types";
        public const string DefaultNamespace = "http://schemas.servicestack.net/types";

        public const string ServiceStackBaseUri = "http://localhost:20000";
        public const string AbsoluteBaseUri = ServiceStackBaseUri + "/";
    }
}