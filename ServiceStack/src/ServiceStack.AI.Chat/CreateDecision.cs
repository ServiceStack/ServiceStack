using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Nodes;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// https://openrouter.ai/docs/api/api-reference/alphadecisions/submit-a-decisions-questions-and-answers-request
/// </summary>
[Tag("AI")]
[DataContract]
[Description("Decisions API (OpenRouter)")]
[Notes("Ask a decision model, e.g. TypeSafe's Jev, narrow typed questions about some state. Your code owns the workflow.")]
[Route("/v1/decisions", "POST"), SystemJson(UseSystemJson.Never)]
public class CreateDecision : IPost, IReturn<DecisionResponse>
{
    [Description("The decision model, e.g. ~typesafe/jev-latest")]
    [DataMember(Name = "model")]
    public string Model { get; set; } = ChatDecisions.DefaultModel;

    [Description("The content to evaluate: a plain string, or a JSON object or array of related context")]
    [DataMember(Name = "state")]
    public object State { get; set; } = "";

    [Description("Named noul (yes/no probability), choice (one option) or score (ordered scale) questions")]
    [DataMember(Name = "questions")]
    public Dictionary<string, DecisionQuestion> Questions { get; set; } = [];

    [Description("A unique identifier for grouping related requests")]
    [DataMember(Name = "session_id")]
    public string? SessionId { get; set; }

    [Description("End-user identifier sent to OpenRouter. Omitted unless you set it.")]
    [DataMember(Name = "user")]
    public string? User { get; set; }

    [Description("OpenRouter provider routing preferences")]
    [DataMember(Name = "provider")]
    public Dictionary<string, object>? Provider { get; set; }
}

[DataContract]
public class DecisionQuestion
{
    [Description("noul, choice or score")]
    [DataMember(Name = "type")]
    public string Type { get; set; } = "";

    [Description("What to decide: text, or a JSON object/array of guidance")]
    [DataMember(Name = "instructions")]
    public object Instructions { get; set; } = "";

    [Description("noul: {true,false} descriptions; choice: {option: description}; score: ordered descriptions")]
    [DataMember(Name = "criteria")]
    public object? Criteria { get; set; }

    /// <summary>A probability from 0 (false) to 1 (true)</summary>
    public static DecisionQuestion Noul(string instructions, string whenTrue, string whenFalse) => new() {
        Type = "noul", Instructions = instructions,
        Criteria = new Dictionary<string, string> { ["true"] = whenTrue, ["false"] = whenFalse },
    };

    /// <summary>Pick one named option, with the full probability distribution</summary>
    public static DecisionQuestion Choice(string instructions, Dictionary<string, string> options) => new() {
        Type = "choice", Instructions = instructions, Criteria = options,
    };

    /// <summary>A position on an ordered scale (0 = first description), with its distribution</summary>
    public static DecisionQuestion Score(string instructions, params string[] scale) => new() {
        Type = "score", Instructions = instructions, Criteria = scale.ToList(),
    };
}

[DataContract]
public class DecisionResponse
{
    [DataMember(Name = "id")]
    public string? Id { get; set; }
    [DataMember(Name = "model")]
    public string Model { get; set; } = "";
    [DataMember(Name = "provider")]
    public string? Provider { get; set; }
    [Description("Answers by question name, validated against the submitted questions")]
    [DataMember(Name = "answers")]
    public Dictionary<string, DecisionAnswer> Answers { get; set; } = [];
    [DataMember(Name = "usage")]
    public DecisionUsage? Usage { get; set; }

    /// <summary>The probability (0-1) that a noul question is true</summary>
    public double Noul(string question) => Answer(question, "noul").Noul!.Value;
    /// <summary>The selected option of a choice question</summary>
    public string Choice(string question) => Answer(question, "choice").Choice!;
    /// <summary>The position (0 = first description) of a score question</summary>
    public double Score(string question) => Answer(question, "score").Score!.Value;

    DecisionAnswer Answer(string question, string type) =>
        Answers.TryGetValue(question, out var answer) && answer.Type == type ? answer
            : throw new KeyNotFoundException($"No {type} answer for '{question}'");
}

[DataContract]
public class DecisionAnswer
{
    [DataMember(Name = "type")]
    public string Type { get; set; } = "";
    [Description("noul: probability from 0 (no) to 1 (yes)")]
    [DataMember(Name = "noul")]
    public double? Noul { get; set; }
    [Description("choice: the selected option")]
    [DataMember(Name = "choice")]
    public string? Choice { get; set; }
    [Description("score: position on the scale, 0 = first description")]
    [DataMember(Name = "score")]
    public double? Score { get; set; }
    [DataMember(Name = "confidence")]
    public double? Confidence { get; set; }
    [Description("choice/score: probability of each option (score options are \"0\", \"1\", ...)")]
    [DataMember(Name = "probabilities")]
    public Dictionary<string, double>? Probabilities { get; set; }
    [Description("score: description of each scale position")]
    [DataMember(Name = "legend")]
    public Dictionary<string, string>? Legend { get; set; }
}

[DataContract]
public class DecisionUsage
{
    [DataMember(Name = "input_tokens")]
    public long InputTokens { get; set; }
    [DataMember(Name = "output_tokens")]
    public long OutputTokens { get; set; }
    [DataMember(Name = "cost")]
    public double? Cost { get; set; }
}

/// <summary>Typed DTOs to and from the Decisions API's JSON</summary>
public static class DecisionJson
{
    /// <summary>A JsonNode is used as-is; strings stay strings; other values use ServiceStack.Text's JSON</summary>
    static JsonNode? Node(object? value) => value switch {
        null => null,
        JsonNode node => node.DeepClone(),
        string text => JsonValue.Create(text),
        _ => JsonNode.Parse(value.ToJson()),
    };

    public static JsonObject ToJson(CreateDecision request)
    {
        var questions = new JsonObject();
        foreach (var (name, question) in request.Questions)
        {
            var json = new JsonObject { ["type"] = question.Type, ["instructions"] = Node(question.Instructions) };
            if (question.Criteria != null) json["criteria"] = Node(question.Criteria);
            questions[name] = json;
        }
        var body = new JsonObject {
            ["model"] = string.IsNullOrEmpty(request.Model) ? ChatDecisions.DefaultModel : request.Model,
            ["state"] = Node(request.State),
            ["questions"] = questions,
        };
        if (request.SessionId != null) body["session_id"] = request.SessionId;
        if (request.User != null) body["user"] = request.User;
        if (request.Provider != null) body["provider"] = Node(request.Provider);
        return body;
    }

    public static DecisionResponse FromJson(JsonObject response)
    {
        static double? Number(JsonNode? node) => node is JsonValue v && v.TryGetValue<double>(out var n) ? n : null;
        var result = new DecisionResponse {
            Id = response.GetString("id"), Model = response.GetString("model") ?? "", Provider = response.GetString("provider"),
        };
        foreach (var (name, node) in response.GetObject("answers") ?? [])
        {
            if (node is not JsonObject answer) continue;
            result.Answers[name] = new DecisionAnswer {
                Type = answer.GetString("type") ?? "",
                Noul = Number(answer["noul"]),
                Choice = answer.GetString("choice"),
                Score = Number(answer["score"]),
                Confidence = Number(answer["confidence"]),
                Probabilities = answer.GetObject("probabilities")?.ToDictionary(x => x.Key, x => Number(x.Value) ?? 0),
                Legend = answer.GetObject("legend")?.ToDictionary(x => x.Key, x => x.Value?.GetValue<string>() ?? ""),
            };
        }
        if (response.GetObject("usage") is { } usage)
            result.Usage = new DecisionUsage {
                InputTokens = usage.GetLong("input_tokens") ?? 0,
                OutputTokens = usage.GetLong("output_tokens") ?? 0,
                Cost = Number(usage["cost"]),
            };
        return result;
    }
}
