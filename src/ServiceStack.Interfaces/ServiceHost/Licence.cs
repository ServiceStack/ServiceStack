namespace ServiceStack.ServiceHost
{
    /// <summary>
    /// Simple model to hold licence information
    /// </summary>
    public class Licence
    {
        public string Product { get; set; }
        public string Key { get; set; }

        public Licence() {}

        public Licence(string product, string key)
        {
            Product = product;
            Key = key;
        }
    }
}