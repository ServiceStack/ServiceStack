namespace ServiceStack.Redis
{
    public class SortOptions
    {
        public string SortPattern { get; set; }
        public int? Skip { get; set; }
        public int? Take { get; set; }
        public string GetPattern { get; set; }
        public bool SortAlpha { get; set; }
        public bool SortDesc { get; set; }
        public string StoreAtKey { get; set; }
    }
}