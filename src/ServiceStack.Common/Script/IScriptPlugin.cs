namespace ServiceStack.Script
{
    public interface IScriptPlugin
    {
        void Register(ScriptContext context);
    }

    public interface IScriptPluginBefore
    {
        void BeforePluginsLoaded(ScriptContext context);
    }

    public interface IScriptPluginAfter
    {
        void AfterPluginsLoaded(ScriptContext context);
    }
}