using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ServiceStack.AI;

public sealed class JevValidationException(string path,string message):ArgumentException(path+": "+message)
{
    public string Path {get;}=path;
    public string Detail {get;}=message;
}
/// <summary>The restricted v1 contract, shared by saved recipes, downloads and model authoring.</summary>
public static class DecisionRecipeValidator
{
    public const int MaxBytes=512*1024;
    public static readonly string[] Models=["~typesafe/jev-latest","typesafe/jev-1.13","typesafe/jev-1.13-20260917"];
    static readonly HashSet<string> SchemaKeys=new(["type","title","description","default","properties","required","additionalProperties","items","enum","format","minLength","maxLength","minimum","maximum","minItems","maxItems"]);
    public static void Require(bool valid,string path,string message){if(!valid)throw new JevValidationException(path,message);}
    public static string? String(JsonNode? value)=>value is JsonValue scalar&&scalar.TryGetValue<string>(out var text)?text:null;
    public static bool Boolean(JsonNode? value,out bool result){result=false;return value is JsonValue scalar&&scalar.TryGetValue<bool>(out result);}
    public static bool Number(JsonNode? value,out double result)
    {
        result=0;if(value is not JsonValue scalar)return false;
        try {if(scalar.TryGetValue<double>(out result))return double.IsFinite(result);var raw=scalar.ToJsonString();if(raw is "true" or "false"||raw.StartsWith('"'))return false;return double.TryParse(raw,NumberStyles.Float,CultureInfo.InvariantCulture,out result)&&double.IsFinite(result);}
        catch(Exception e) when(e is FormatException or InvalidOperationException or System.Text.Json.JsonException){return false;}
    }
    public static bool Integer(JsonNode? value,out long result)
    {
        result=0;if(value is not JsonValue scalar)return false;try {var raw=scalar.ToJsonString();return Regex.IsMatch(raw,@"\A-?[0-9]+\z")&&long.TryParse(raw,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out result);}catch{return false;}
    }
    public static string Text(JsonNode? value,string path,int max=8000,bool empty=false)
    {
        var text=String(value);Require(text!=null&&JevJson.Length(text)<=max&&(empty||!string.IsNullOrWhiteSpace(text)),path,$"Enter {(empty?"text":"nonempty text")} of at most {max} characters.");return text!;
    }
    static void Key(string name,string path)=>Require(Regex.IsMatch(name,@"\A[A-Za-z][A-Za-z0-9_]{0,63}\z")&&name is not ("constructor" or "prototype" or "__proto__"),path,"Use a letter followed by letters, numbers or underscores (up to 64 characters).");
    static void Allowed(JsonObject value,IEnumerable<string> allowed,string path,string message)=>Require(!value.Any(p=>!allowed.Contains(p.Key)),path,message);
    public static void Schema(JsonNode? value,string path="inputSchema",int depth=0)
    {
        Require(value is JsonObject,path,"Expected an input schema object.");var schema=(JsonObject)value!;
        Require(depth<=4,path,"Use at most four levels of nesting.");Allowed(schema,SchemaKeys,path,"Unsupported schema keywords.");
        var kind=String(schema["type"]);Require(kind is "object" or "string" or "number" or "integer" or "boolean" or "array",path+".type","Choose a supported type.");
        foreach(var name in new[]{"title","description"})if(schema.ContainsKey(name))Text(schema[name],path+"."+name,empty:true);
        if(schema.ContainsKey("format"))Require(kind=="string"&&String(schema["format"]) is "textarea" or "date" or "email",path+".format","Supported hints are textarea, date and email.");
        foreach(var (low,high,type) in new[]{("minLength","maxLength","string"),("minItems","maxItems","array"),("minimum","maximum","numeric")}) {
            foreach(var name in new[]{low,high})if(schema.ContainsKey(name)){
                Require((type=="numeric"?kind is "number" or "integer":kind==type)&&Number(schema[name],out _),path+"."+name,"Invalid bound for this type.");
                if(type!="numeric")Require(Integer(schema[name],out var n)&&n>=0,path+"."+name,"Use a nonnegative integer.");
            }
            if(schema.ContainsKey(low)&&schema.ContainsKey(high)){Number(schema[low],out var l);Number(schema[high],out var h);Require(l<=h,path,"The lower bound exceeds the upper bound.");}
        }
        if(kind=="object") {
            var props=Default(schema,"properties",new JsonObject());Require(props is JsonObject o&&o.Count<=32,path+".properties","Use at most 32 fields per object.");
            var required=Default(schema,"required",new JsonArray());Require(required is JsonArray r&&r.All(x=>String(x) is { } name&&((JsonObject)props).ContainsKey(name)),path+".required","Required fields must exist in properties.");
            Require(((JsonArray)required).Select(String).Distinct().Count()==((JsonArray)required).Count,path+".required","Remove duplicate required fields.");
            Require(!schema.ContainsKey("additionalProperties")||Boolean(schema["additionalProperties"],out _),path+".additionalProperties","Use true or false.");
            foreach(var prop in (JsonObject)props){Key(prop.Key,path+".properties."+prop.Key);Schema(prop.Value,path+".properties."+prop.Key,depth+1);}
        }else Require(!schema.Any(p=>p.Key is "properties" or "required" or "additionalProperties"),path,"Object keywords require an object type.");
        if(kind=="array"){Schema(schema["items"],path+".items",depth+1);Require(String(schema["items"]?["type"]) is not ("object" or "array"),path+".items","Arrays currently support primitive values.");}
        else Require(!schema.ContainsKey("items"),path,"items requires an array type.");
        if(schema.ContainsKey("enum")) {
            Require(kind is not ("object" or "array")&&schema["enum"] is JsonArray a&&a.Count is >=1 and <=255,path+".enum","Use 1–255 primitive options.");var values=(JsonArray)schema["enum"]!;
            Require(values.Select(x=>JevJson.Encode(x,false,true)).Distinct().Count()==values.Count,path+".enum","Remove duplicate options.");
            var test=schema.Clone();test.Remove("enum");test.Remove("default");foreach(var option in values)Input(test,option,path+".enum");
        }
        if(schema.ContainsKey("default")){var test=schema.Clone();test.Remove("default");Input(test,schema["default"],path+".default",false);}
    }
    static JsonNode? Default(JsonObject obj,string name,JsonNode fallback)=>obj.ContainsKey(name)?obj[name]:fallback;
    static double Bound(JsonObject schema,string name,double fallback)=>schema.ContainsKey(name)&&Number(schema[name],out var result)?result:fallback;
    public static void Input(JsonObject schema,JsonNode? value,string path="input",bool required=true)
    {
        var kind=String(schema["type"]);
        switch(kind) {
            case "object":
                Require(value is JsonObject,path,"Expected an object.");var obj=(JsonObject)value!;var props=schema["properties"] as JsonObject??new JsonObject();var fields=(schema["required"] as JsonArray??new JsonArray()).Select(String).ToHashSet();
                Require(Boolean(schema["additionalProperties"],out var allow)&&allow||obj.All(p=>props.ContainsKey(p.Key)),path,"Remove unknown input fields.");
                foreach(var name in fields)Require(obj.ContainsKey(name!)&&obj[name!]!=null,path+"."+name,"This field is required.");
                foreach(var pair in obj)if(props[pair.Key] is JsonObject property)Input(property,pair.Value,path+"."+pair.Key,fields.Contains(pair.Key));break;
            case "array":
                Require(value is JsonArray,path,"Expected a list.");var array=(JsonArray)value!;Require(array.Count>=Bound(schema,"minItems",0)&&array.Count<=Bound(schema,"maxItems",500),path,"List length is outside the allowed range.");
                for(var i=0;i<array.Count;i++)Input((JsonObject)schema["items"]!,array[i],path+"."+i);break;
            case "string":
                Require(String(value)!=null,path,"Expected text.");var text=String(value)!;Require(!required||!string.IsNullOrWhiteSpace(text),path,"This field is required.");
                if(text.Length>0||required)Require(JevJson.Length(text)>=Bound(schema,"minLength",0)&&JevJson.Length(text)<=Bound(schema,"maxLength",MaxBytes),path,"Text length is outside the allowed range.");break;
            case "boolean":Require(Boolean(value,out _),path,"Expected true or false.");break;
            default:
                Require(Number(value,out var number)&&(kind!="integer"||number==Math.Truncate(number)),path,$"Expected a finite {kind}.");
                Require(number>=Bound(schema,"minimum",double.NegativeInfinity)&&number<=Bound(schema,"maximum",double.PositiveInfinity),path,"Number is outside the allowed range.");break;
        }
        if(schema["enum"] is JsonArray options)Require(options.Any(x=>Equivalent(value,x)),path,"Choose one of the listed options.");
    }
    public static bool Equivalent(JsonNode? a,JsonNode? b)
    {
        if(Number(a,out var x)&&Number(b,out var y))return x==y;
        return JsonNode.DeepEquals(a,b);
    }
    public static JsonObject Validate(JsonNode? value)
    {
        var copied=JevJson.Copy(value);Require(copied is JsonObject,"recipe","Expected a recipe object.");var doc=(JsonObject)copied!;
        Allowed(doc,["schemaVersion","name","description","tags","content","decisionModel","inputSchema","state","questions","presentation","examples"],"recipe","Remove unsupported or server-owned recipe fields.");
        Require(Integer(doc["schemaVersion"],out var version)&&version==1,"schemaVersion","Only recipe version 1 is supported.");Text(doc["name"],"name",120);Text(Default(doc,"description",JsonValue.Create("")!),"description",2000,true);
        if(doc.ContainsKey("content"))Text(doc["content"],"content",40,true);
        var tags=Default(doc,"tags",new JsonArray());var maxTags=doc.ContainsKey("content")?3:12;Require(tags is JsonArray a&&a.Count<=maxTags,"tags",$"Use up to {maxTags} tags.");foreach(var tag in (JsonArray)tags)Text(tag,"tags",40);
        Require(Models.Contains(String(doc["decisionModel"])),"decisionModel","Choose a supported Jev decision model.");Schema(doc["inputSchema"]);Require(String(doc["inputSchema"]?["type"])=="object","inputSchema.type","The form root must be an object.");
        var state=Default(doc,"state",new JsonObject {["mode"]="object"});Require(state is JsonObject&&String(state["mode"]) is "object" or "text","state","Use object or text mode.");Allowed((JsonObject)state,["mode","field"],"state","Unsupported state mapping.");
        if(String(state["mode"])=="text")Require(String(state["field"]) is { } field&&String(doc["inputSchema"]?["properties"]?[field]?["type"])=="string","state.field","Choose a top-level text field.");
        Require(doc["questions"] is JsonObject q&&q.Count is >=1 and <=32,"questions","Use 1–32 independent questions.");var questions=(JsonObject)doc["questions"]!;
        foreach(var (name,valueQuestion) in questions) {
            var path="questions."+name;Key(name,path);Require(valueQuestion is JsonObject,path,"Use type, instructions and criteria only.");var question=(JsonObject)valueQuestion!;Allowed(question,["type","instructions","criteria"],path,"Use type, instructions and criteria only.");Text(question["instructions"],path+".instructions");var kind=String(question["type"]);var criteria=question["criteria"];
            Require(kind is "choice" or "score" or "noul",path+".type","Choose Choice, Score or Noul.");
            if(kind=="score"){Require(criteria is JsonArray c&&c.Count is >=2 and <=10,path+".criteria","Use 2–10 ordered descriptions.");foreach(var item in (JsonArray)criteria!)Text(item,path+".criteria");}
            else if(kind=="choice"){Require(criteria is JsonObject c&&c.Count is >=2 and <=255,path+".criteria","Use 2–255 named options.");foreach(var option in (JsonObject)criteria!){Key(option.Key,path+".criteria."+option.Key);Text(option.Value,path+".criteria."+option.Key);}}
            else if(criteria!=null){Require(criteria is JsonObject c&&c.Count==2&&c.ContainsKey("true")&&c.ContainsKey("false"),path+".criteria","Describe both true and false.");foreach(var item in (JsonObject)criteria)Text(item.Value,path+".criteria");}
        }
        var presentation=Default(doc,"presentation",new JsonObject());Require(presentation is JsonObject,"presentation","Expected question labels.");Allowed((JsonObject)presentation,["questions"],"presentation","Expected question labels.");var labels=Default((JsonObject)presentation!,"questions",new JsonObject());Require(labels is JsonObject l&&l.All(p=>questions.ContainsKey(p.Key)),"presentation.questions","Labels must refer to existing questions.");
        foreach(var label in (JsonObject)labels){Require(label.Value is JsonObject,"presentation."+label.Key,"Use label and optionLabels.");var spec=(JsonObject)label.Value!;Allowed(spec,["label","optionLabels"],"presentation."+label.Key,"Use label and optionLabels.");if(spec.ContainsKey("label"))Text(spec["label"],"presentation."+label.Key+".label",200);
            if(spec.ContainsKey("optionLabels")){Require(String(questions[label.Key]?["type"])=="choice"&&spec["optionLabels"] is JsonObject opts&&opts.All(p=>((JsonObject)questions[label.Key]!["criteria"]!).ContainsKey(p.Key)),"presentation."+label.Key,"Labels must refer to existing choice options.");foreach(var option in (JsonObject)spec["optionLabels"]!)Text(option.Value,"presentation."+label.Key,200);}
        }
        var examples=Default(doc,"examples",new JsonArray());Require(examples is JsonArray e&&e.Count<=30,"examples","Use up to 30 example cases.");var ids=new HashSet<string>();
        for(var i=0;i<((JsonArray)examples).Count;i++) {
            var path="examples."+i;Require(examples[i] is JsonObject,path,"Unsupported example fields.");var example=(JsonObject)examples[i]!;Allowed(example,["id","label","input","expected","provenance","notes","execution"],path,"Unsupported example fields.");var id=Text(example["id"],path+".id",80);Require(ids.Add(id),path+".id","Example IDs must be unique.");Text(example["label"],path+".label",120);Input((JsonObject)doc["inputSchema"]!,example["input"],path+".input");
            var expected=Default(example,"expected",new JsonObject());Require(expected is JsonObject exp&&exp.All(p=>questions.ContainsKey(p.Key)),path+".expected","Expectations must refer to existing questions.");
            foreach(var answer in (JsonObject)expected){var question=(JsonObject)questions[answer.Key]!;var kind=String(question["type"]);Require(kind=="choice"?String(answer.Value) is { } choice&&((JsonObject)question["criteria"]!).ContainsKey(choice):kind=="noul"?Boolean(answer.Value,out _):Number(answer.Value,out var number)&&number>=0&&number<=((JsonArray)question["criteria"]!).Count-1,path+".expected."+answer.Key,"Expected answer does not match this question's options/scale.");}
            Require(String(Default(example,"provenance",JsonValue.Create("authored")!)) is "authored" or "ai-suggested" or "user-reviewed",path+".provenance","Choose expectation provenance.");
            if(example.ContainsKey("notes"))Text(example["notes"],path+".notes",2000,true);
            if(example.ContainsKey("execution")){var stripped=doc.Clone();stripped["examples"]=new JsonArray();var execution=DecisionRecorded.Validate(stripped,example["execution"]);Require(JevJson.Hash(execution["input"])==JevJson.Hash(example["input"]),path+".execution.input","Recorded input must match the example.");}
        }
        return doc;
    }
    public static JsonObject Compile(JsonNode? document,JsonNode? inputs)
    {
        var doc=Validate(document);var copied=JevJson.Copy(inputs);Input((JsonObject)doc["inputSchema"]!,copied);
        var mapping=doc["state"] as JsonObject??new JsonObject {["mode"]="object"};var state=String(mapping["mode"])=="object"?copied:copied?[String(mapping["field"])!]?.DeepClone()??JsonValue.Create("");
        Require(String(state)!="","input","Provide text to evaluate.");return new JsonObject {["model"]=doc["decisionModel"]?.DeepClone(),["state"]=state?.DeepClone(),["questions"]=doc["questions"]?.DeepClone()};
    }
}
