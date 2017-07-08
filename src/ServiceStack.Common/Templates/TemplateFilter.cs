namespace ServiceStack.Templates
{
    public class TemplateFilter
    {
        public ITemplatePages Pages { get; set; }

        public virtual TemplateFilter Init()
        {
            return this;
        }
    }
}