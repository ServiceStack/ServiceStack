using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

public class OllamaProvider(ILogger log, IHttpClientFactory factory) : OpenAiProviderBase(log, factory)
{
    public bool AllModels { get; set; }
    
    public override async Task LoadAsync(CancellationToken token=default)
    {
        if (AllModels)
        {
            var models = await GetModelsAsync(token).ConfigAwait();
            Models ??= new();
            foreach (var model in models)
            {
                Models[model.Key] = model.Value;
            }
        }
    }

    public async Task<Dictionary<string, string>> GetModelsAsync(CancellationToken token=default)
    {
        var ret = new Dictionary<string, string>();
        
        var obj = await GetJsonObjectAsync(BaseUrl + "/api/tags", token).ConfigAwait();
        if (obj.TryGetValue("models", out var oModels)
            && oModels is List<object> models)
        {
            foreach (var model in models)
            {
                if (model is Dictionary<string, object> modelObj
                    && modelObj.TryGetValue("name", out var oName)
                    && oName is string name)
                {
                    if (name.EndsWith(":latest"))
                        name = name[..^7];
                    ret[name] = name;
                }
            }
        }
        return ret;
    }

    public static OpenAiProviderBase? Create(ILogger log, IHttpClientFactory factory, Dictionary<string, object?> definition)
    {
        var to = new OllamaProvider(log, factory);
        to.Populate(definition);
        
        to.AllModels = (bool) definition.GetValueOrDefault("all_models", false)!;
        
        if (to.Models.Count == 0 && !to.AllModels)
            return null;

        return to;
    }
}