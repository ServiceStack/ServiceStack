namespace ServiceStack.Templates
{
    public interface ITemplatePlugin
    {
        void Register(TemplateContext context);
    }

    public interface ITemplatePluginBefore
    {
        void BeforePluginsLoaded(TemplateContext context);
    }

    public interface ITemplatePluginAfter
    {
        void AfterPluginsLoaded(TemplateContext context);
    }
}