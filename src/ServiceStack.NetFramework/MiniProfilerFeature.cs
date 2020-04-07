using ServiceStack.MiniProfiler;

namespace ServiceStack
{
    public class MiniProfilerFeature : IPlugin, Model.IHasStringId
    {
        public string Id { get; set; } = Plugins.MiniProfiler;
        public MiniProfilerFeature()
        {
            Profiler.Current = new MiniProfilerAdapter();
        }

        public void Register(IAppHost appHost)
        {
            appHost.RawHttpHandlers.Add(MiniProfiler.UI.MiniProfilerHandler.MatchesRequest);
        }
    }
}