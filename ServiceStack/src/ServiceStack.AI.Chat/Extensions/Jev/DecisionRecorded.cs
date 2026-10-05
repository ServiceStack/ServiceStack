using System.Globalization;
using System.Text.Json.Nodes;
using static ServiceStack.AI.DecisionRecipeValidator;

namespace ServiceStack.AI;

public static class DecisionRecorded
{
    public static JsonObject Normalize(JsonNode? response,JsonObject questions)
    {
        Require(response is JsonObject&&response["answers"] is JsonObject answers&&answers.Count==questions.Count&&answers.All(p=>questions.ContainsKey(p.Key)),"response.answers","The provider did not return every requested answer.");
        var source=(JsonObject)response!["answers"]!;var result=new JsonObject();
        foreach(var (name,q) in questions) {
            var question=(JsonObject)q!;var kind=String(question["type"]);var path="response.answers."+name;
            Require(source[name] is JsonObject a&&String(a["type"])==kind,path,"Unexpected answer type.");var answer=(JsonObject)source[name]!;var normalized=new JsonObject {["type"]=kind};
            if(kind=="noul"){Require(Number(answer["noul"],out var probability)&&probability is >=0 and <=1,path,"Invalid probability of yes.");normalized["noul"]=answer["noul"]?.DeepClone();}
            else {
                var options=kind=="choice"?((JsonObject)question["criteria"]!).Select(p=>p.Key).ToArray():Enumerable.Range(0,((JsonArray)question["criteria"]!).Count).Select(i=>i.ToString(CultureInfo.InvariantCulture)).ToArray();
                Require(answer["probabilities"] is JsonObject p&&p.Count==options.Length&&p.All(x=>options.Contains(x.Key)),path,"Invalid probability options.");var probabilities=(JsonObject)answer["probabilities"]!;
                Require(probabilities.All(p=>Number(p.Value,out var n)&&n is >=0 and <=1),path,"Invalid probabilities.");
                Require(Math.Abs(probabilities.Sum(p=>{Number(p.Value,out var n);return n;})-1)<=0.025,path,"Probabilities do not sum to one.");
                Require(answer["confidence"]==null||Number(answer["confidence"],out var confidence)&&confidence is >=0 and <=1,path,"Invalid confidence.");
                normalized["probabilities"]=new JsonObject(options.Select(o=>new KeyValuePair<string,JsonNode?>(o,probabilities[o]?.DeepClone())));normalized["confidence"]=answer["confidence"]?.DeepClone();
                if(kind=="choice"){Require(options.Contains(String(answer["choice"])),path,"The selected option is not in the question.");normalized["choice"]=answer["choice"]?.DeepClone();}
                else {
                    Require(Number(answer["score"],out var score)&&score>=0&&score<=options.Length-1,path,"Invalid score.");var legend=answer["legend"];
                    Require(legend==null||legend is JsonObject l&&l.Count==options.Length&&l.All(p=>options.Contains(p.Key)),path,"Invalid scale legend.");
                    var expected=new JsonObject(options.Select((o,i)=>new KeyValuePair<string,JsonNode?>(o,question["criteria"]![i]?.DeepClone())));
                    if(legend!=null)Require(JsonNode.DeepEquals(legend,expected),path,"Provider scale differs from the submitted rubric.");normalized["score"]=answer["score"]?.DeepClone();normalized["legend"]=expected;
                }
            }result[name]=normalized;
        }return result;
    }
    public static void Bounded(JsonNode? value,string path="publication",int depth=0)
    {
        Require(depth<=32,path,"Use at most 32 JSON nesting levels.");if(value is JsonObject obj)foreach(var item in obj)Bounded(item.Value,path,depth+1);if(value is JsonArray array)foreach(var item in array)Bounded(item,path,depth+1);
    }
    public static JsonObject Validate(JsonObject document,JsonNode? value)
    {
        Bounded(value,"execution");Require(value is JsonObject,"execution","A successful recorded execution is required.");var execution=(JsonObject)value!;
        Require(execution.All(p=>new[]{"status","input","prompt","answers","model","completedAt","durationMs"}.Contains(p.Key)),"execution","Remove private or unsupported execution fields.");
        Require(String(execution["status"])=="succeeded","execution.status","Run this recipe successfully first.");
        var request=Compile(document,execution["input"]);Require(JevJson.Hash(execution["prompt"])==JevJson.Hash(request["state"]),"execution.prompt","Prompt must match the recorded input.");
        var normalized=Normalize(new JsonObject {["answers"]=execution["answers"]?.DeepClone()},(JsonObject)document["questions"]!);
        Require(JsonNode.DeepEquals(normalized,execution["answers"]),"execution.answers","Use complete normalized results without private fields.");DecisionRecipeValidator.Text(execution["model"],"execution.model",200);
        var timestamp=String(execution["completedAt"]);Require(timestamp!=null&&System.Text.RegularExpressions.Regex.IsMatch(timestamp,@"(?:Z|[+]00:00)$")&&DateTimeOffset.TryParse(timestamp,CultureInfo.InvariantCulture,DateTimeStyles.None,out var completed)&&completed.Offset==TimeSpan.Zero,"execution.completedAt","Use a UTC completion time.");
        if(execution.ContainsKey("durationMs"))Require(Number(execution["durationMs"],out var duration)&&duration>=0,"execution.durationMs","Use a nonnegative duration.");
        return (JsonObject)JevJson.Copy(execution,2*1024*1024)!;
    }
    public static JsonObject Package(JsonObject document,JsonObject run)
    {
        Require(String(run["status"])=="succeeded","runId","Select a successful local execution.");
        var old=(run["recipe"] as JsonObject)?.Clone();old?.Remove("examples");var current=document.Clone();current.Remove("examples");
        Require(JevJson.Hash(old)==JevJson.Hash(current),"runId","Saved changes require a matching successful execution.");var compiled=Compile(document,run["input"]);
        Require(JsonNode.DeepEquals(compiled,run["request"]),"runId","The execution request does not match this recipe.");
        Number(run["completedAt"],out var completed);var result=new JsonObject {["status"]="succeeded",["input"]=run["input"]?.DeepClone(),["prompt"]=run["request"]?["state"]?.DeepClone(),["answers"]=run["answers"]?.DeepClone(),["model"]=run["response"]?["model"]?.DeepClone()??compiled["model"]?.DeepClone(),["completedAt"]=DateTimeOffset.FromUnixTimeMilliseconds((long)(completed*1000)).ToString("yyyy-MM-ddTHH:mm:ss.fff+00:00",CultureInfo.InvariantCulture)};
        if(run["durationMs"]!=null)result["durationMs"]=run["durationMs"]?.DeepClone();return Validate(document,result);
    }
}
