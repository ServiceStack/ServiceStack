using System.Text;
using System.Text.RegularExpressions;

namespace ServiceStack.AI;


/// <summary>
/// Generates typed C# classes from a JSON document or the JSON Schema describing it — the C# port of the
/// Chat UI's <c>jsonTypes.mjs</c>, and the single implementation behind both the Admin UI's Code view and
/// the <c>pdf</c> AppTask, so what's shown is exactly what's written to the project.
/// <para>
/// Deterministic and instant, no model required. A JSON example only carries JSON's six types, so passing
/// the matching schema produces better output: <c>required</c> becomes non-nullable, <c>multipleOf: 0.01</c>
/// becomes <c>decimal</c>, <c>format</c> becomes date/Guid types, <c>enum</c> becomes a real enum and
/// <c>description</c> becomes doc comments.
/// </para>
/// </summary>
public static partial class JsonTypes
{
    // ── Naming ──

    static readonly HashSet<string> CSharpReserved =
    [
        "abstract","as","base","bool","break","byte","case","catch","char","checked","class","const",
        "continue","decimal","default","delegate","do","double","else","enum","event","explicit","extern",
        "false","finally","fixed","float","for","foreach","goto","if","implicit","in","int","interface",
        "internal","is","lock","long","namespace","new","null","object","operator","out","override",
        "params","private","protected","public","readonly","ref","return","sbyte","sealed","short",
        "sizeof","stackalloc","static","string","struct","switch","this","throw","true","try","typeof",
        "uint","ulong","unchecked","unsafe","ushort","using","virtual","void","volatile","while",
    ];

    /// <summary>Insert a break between a lowercase/digit and an uppercase letter, so camelCase splits</summary>
    [GeneratedRegex(@"(\p{Ll}|\p{N})(\p{Lu})")]
    private static partial Regex CamelBoundaryRegex();

    /// <summary>Anything that can't be part of an identifier is a word separator</summary>
    [GeneratedRegex(@"[^\p{L}\p{N}]+")]
    private static partial Regex NonWordRegex();

    [GeneratedRegex(@"^[\p{L}_][\p{L}\p{N}_]*$")]
    private static partial Regex IdentifierRegex();

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9 _-]*$")]
    private static partial Regex EnumMemberRegex();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}$")]
    private static partial Regex IsoDateRegex();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}(:\d{2}(\.\d+)?)?(Z|[+-]\d{2}:?\d{2})?$")]
    private static partial Regex IsoDateTimeRegex();

    [GeneratedRegex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")]
    private static partial Regex UuidRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex BlankLinesRegex();

    static List<string> Words(string? value) => NonWordRegex()
        .Split(CamelBoundaryRegex().Replace(value ?? "", "$1 $2"))
        .Where(x => x.Length > 0)
        .ToList();

    /// <summary>"line-items" → "LineItems". Falls back to "Value" so a name is always usable.</summary>
    public static string Pascal(string? value)
    {
        var sb = new StringBuilder();
        foreach (var word in Words(value))
            sb.Append(char.ToUpperInvariant(word[0])).Append(word[1..]);
        var name = sb.ToString();
        // '2fa' → '_2fa': keeps the caller's name instead of falling back to a meaningless one
        if (name.Length > 0 && char.IsDigit(name[0]))
            name = "_" + name;
        return name.Length > 0 ? name : "Value";
    }

    /// <summary>items → Item, addresses → Address, status → Status</summary>
    public static string Singular(string name)
    {
        if (name.EndsWith("ies", StringComparison.OrdinalIgnoreCase) && name.Length > 4)
            return name[..^3] + "y";
        if (name.EndsWith("ss", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("us", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("is", StringComparison.OrdinalIgnoreCase))
            return name;
        if (name.EndsWith("ches", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("shes", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("xes", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("zes", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("ses", StringComparison.OrdinalIgnoreCase))
            return name[..^2];
        if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase) && name.Length > 2)
            return name[..^1];
        return name;
    }

    /// <summary>"line-items" → "lineItems", for the local variables the examples declare</summary>
    public static string Camel(string? value)
    {
        var name = Pascal(value);
        return Safe(char.ToLowerInvariant(name[0]) + name[1..], "value");
    }

    /// <summary>An identifier C# will accept, escaping keywords rather than renaming them</summary>
    static string Safe(string name, string fallback = "Value")
    {
        var to = IdentifierRegex().IsMatch(name) ? name : fallback;
        return CSharpReserved.Contains(to) ? "@" + to : to;
    }

    // ── Model ──

    /// <summary>Every kind a <see cref="JsonTypeRef"/> can take</summary>
    public static class Kinds
    {
        public const string String = "string";
        public const string Integer = "integer";
        public const string Long = "long";
        public const string Double = "double";
        public const string Decimal = "decimal";
        public const string Boolean = "boolean";
        public const string Date = "date";
        public const string DateTime = "datetime";
        public const string Uuid = "uuid";
        public const string Any = "any";
        public const string Array = "array";
        public const string Map = "map";
        public const string Tuple = "tuple";
        public const string Ref = "ref";
    }

    /// <summary>A reference to a type: a scalar kind, a container, or a named type in the model</summary>
    public class JsonTypeRef
    {
        public string Kind { get; set; } = Kinds.Any;
        /// <summary>Named type this points at, when <see cref="Kind"/> is "ref"</summary>
        public string? Name { get; set; }
        /// <summary>Element type of an array, or value type of a map</summary>
        public JsonTypeRef? Of { get; set; }
        /// <summary>Member types of a tuple</summary>
        public List<JsonTypeRef>? Items { get; set; }
        public bool Nullable { get; set; }

        public JsonTypeRef WithNullable(bool nullable) => new()
        {
            Kind = Kind, Name = Name, Of = Of, Items = Items, Nullable = nullable,
        };
    }

    public class JsonTypeField
    {
        /// <summary>Property name as it appears in the JSON</summary>
        public string Key { get; set; } = null!;
        public string? Description { get; set; }
        public bool Optional { get; set; }
        public bool Deprecated { get; set; }
        public JsonTypeRef Type { get; set; } = null!;
    }

    public class JsonTypeDef
    {
        /// <summary>"object", "enum" or "alias" — an alias names a root that isn't an object</summary>
        public string Kind { get; set; } = "object";
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public List<JsonTypeField> Fields { get; set; } = [];
        public List<string> Values { get; set; } = [];
        public JsonTypeRef? Type { get; set; }
    }

    /// <summary>A language-neutral set of named types (children first) and the type the document itself is</summary>
    public class JsonTypesModel
    {
        public List<JsonTypeDef> Types { get; set; } = [];
        public JsonTypeRef Root { get; set; } = new();

        /// <summary>The root's class name, or null when the document isn't an object</summary>
        public string? RootTypeName => Root.Kind == Kinds.Ref ? Root.Name : null;
    }

    static JsonTypeRef Prim(string kind) => new() { Kind = kind };
    static JsonTypeRef Any() => new() { Kind = Kinds.Any };

    static string TypeKey(JsonTypeRef t) => t.Kind switch
    {
        Kinds.Array => $"[{(t.Of != null ? TypeKey(t.Of) : Kinds.Any)}]",
        Kinds.Map => $"{{{(t.Of != null ? TypeKey(t.Of) : Kinds.Any)}}}",
        Kinds.Ref => t.Name ?? Kinds.Any,
        _ => t.Kind,
    };

    /// <summary>
    /// Builds the model. Structurally identical objects collapse into one type, so an array of rows yields
    /// a single row class rather than one per element.
    /// </summary>
    class ModelBuilder
    {
        public readonly List<JsonTypeDef> Types = [];
        readonly Dictionary<string, string> byShape = new();
        readonly Dictionary<string, string> byRef = new();
        readonly HashSet<string> names = [];
        public string RootName = "Root";

        /// <summary>Claim a name before its fields are built, so a recursive $ref can point back at it</summary>
        public string Reserve(string preferred, bool isRoot = false)
        {
            var name = UniqueName(preferred, "", isRoot);
            names.Add(name);
            return name;
        }

        public string UniqueName(string preferred, string shapeKey, bool isRoot = false)
        {
            var name = Pascal(preferred);
            // the root keeps the name derived from the file — nested types give way to it
            if (!isRoot && name == RootName)
                name += "Info";
            if (!names.Contains(name))
                return name;
            // same name, different shape → suffix rather than silently merging
            for (var i = 2; i < 100; i++)
            {
                var candidate = name + i;
                if (!names.Contains(candidate))
                    return candidate;
            }
            return $"{name}_{shapeKey.Length}";
        }

        public JsonTypeRef PushObject(string name, List<JsonTypeField> fields)
        {
            Types.Add(new JsonTypeDef { Kind = "object", Name = name, Fields = fields });
            return new JsonTypeRef { Kind = Kinds.Ref, Name = name };
        }

        public JsonTypeRef AddObject(List<JsonTypeField> fields, string preferredName, bool isRoot = false)
        {
            var shapeKey = string.Join("",
                fields.Select(f => $"{f.Key}{TypeKey(f.Type)}{f.Optional}"));
            if (byShape.TryGetValue(shapeKey, out var existing))
                return new JsonTypeRef { Kind = Kinds.Ref, Name = existing };

            var name = UniqueName(preferredName, shapeKey, isRoot);
            names.Add(name);
            byShape[shapeKey] = name;
            Types.Add(new JsonTypeDef { Kind = "object", Name = name, Fields = fields });
            return new JsonTypeRef { Kind = Kinds.Ref, Name = name };
        }

        public JsonTypeRef AddEnum(List<string> values, string preferredName, string? description)
        {
            var shapeKey = "enum:" + string.Join("", values);
            if (byShape.TryGetValue(shapeKey, out var existing))
                return new JsonTypeRef { Kind = Kinds.Ref, Name = existing };

            var name = UniqueName(preferredName, shapeKey);
            names.Add(name);
            byShape[shapeKey] = name;
            Types.Add(new JsonTypeDef
            {
                Kind = "enum", Name = name, Values = values, Description = description,
            });
            return new JsonTypeRef { Kind = Kinds.Ref, Name = name };
        }

        public bool TryGetRef(string refPath, out string name) => byRef.TryGetValue(refPath, out name!);
        public void SetRef(string refPath, string name) => byRef[refPath] = name;
    }

    // ── Inference: JSON Schema ──

    static JsonObject AsObject(JsonNode? node) => node as JsonObject ?? new JsonObject();

    /// <summary>Right-biased shallow merge onto a fresh object, since a node can only have one parent</summary>
    static JsonObject Merge(JsonObject left, JsonObject right)
    {
        var to = new JsonObject();
        foreach (var entry in left)
            to[entry.Key] = entry.Value?.DeepClone();
        foreach (var entry in right)
            to[entry.Key] = entry.Value?.DeepClone();
        return to;
    }

    static JsonObject DerefSchema(JsonObject schema, JsonObject root, HashSet<string> seen)
    {
        var refPath = schema.GetString("$ref");
        if (refPath == null || !seen.Add(refPath))
            return schema;

        JsonNode? target = root;
        var path = refPath.StartsWith("#/", StringComparison.Ordinal) ? refPath[2..] : refPath;
        foreach (var part in path.Split('/'))
        {
            var key = Uri.UnescapeDataString(part.Replace("~1", "/").Replace("~0", "~"));
            target = target is JsonObject obj ? obj[key] : null;
            if (target == null)
                break;
        }
        if (target is not JsonObject resolved)
            return schema;

        // properties on the $ref'd node itself (description, nullable, …) win over the target's
        var rest = new JsonObject();
        foreach (var entry in schema)
        {
            if (entry.Key != "$ref")
                rest[entry.Key] = entry.Value?.DeepClone();
        }
        return DerefSchema(Merge(resolved, rest), root, seen);
    }

    static JsonObject MergeAllOf(JsonObject schema, JsonObject root)
    {
        var allOf = schema.GetArray("allOf");
        if (allOf == null || allOf.Count == 0)
            return schema;

        var acc = new JsonObject();
        foreach (var entry in schema)
        {
            if (entry.Key != "allOf")
                acc[entry.Key] = entry.Value?.DeepClone();
        }

        foreach (var part in allOf)
        {
            var sub = Resolve(part, root);
            var properties = Merge(acc.GetObject("properties") ?? new JsonObject(),
                sub.GetObject("properties") ?? new JsonObject());
            var required = StringList(acc.GetArray("required"))
                .Concat(StringList(sub.GetArray("required")))
                .Distinct()
                .ToList();

            acc = Merge(acc, sub);
            if (properties.Count > 0)
                acc["properties"] = properties;
            if (required.Count > 0)
                acc["required"] = new JsonArray(required.Select(x => (JsonNode?)JsonValue.Create(x)).ToArray());
        }
        return acc;
    }

    static JsonObject Resolve(JsonNode? schema, JsonObject root) =>
        MergeAllOf(DerefSchema(AsObject(schema), root, []), root);

    static List<string> StringList(JsonArray? array) => array?
        .Select(x => x is JsonValue v && v.TryGetValue<string>(out var s) ? s : null)
        .Where(x => x != null)
        .Select(x => x!)
        .ToList() ?? [];

    /// <summary>The JS <c>typeof</c> of a JSON value, which is what the schema rules are written against</summary>
    static string JsTypeOf(JsonNode? node)
    {
        if (node is not JsonValue value)
            return "object";
        if (value.TryGetValue<bool>(out _)) return "boolean";
        if (value.TryGetValue<string>(out _)) return "string";
        return "number";
    }

    static string SchemaTypeOf(JsonObject s)
    {
        var type = s["type"] is JsonArray types
            ? StringList(types).FirstOrDefault(x => x != "null")
            : s.GetString("type");
        if (!string.IsNullOrEmpty(type))
            return type!;

        if (s.ContainsKey("properties") || s.ContainsKey("additionalProperties")) return "object";
        if (s.ContainsKey("items") || s.ContainsKey("prefixItems")) return "array";
        if (s["enum"] is JsonArray { Count: > 0 } values) return JsTypeOf(values[0]);
        if (s.ContainsKey("const")) return JsTypeOf(s["const"]);
        return "string";
    }

    static bool SchemaNullable(JsonObject s) =>
        s.GetBool("nullable") || (s["type"] is JsonArray types && StringList(types).Contains("null"));

    /// <summary>Every value is a name we could safely use as an enum member</summary>
    static bool TryEnumValues(JsonArray? values, out List<string> to)
    {
        to = [];
        if (values == null || values.Count == 0)
            return false;
        foreach (var value in values)
        {
            if (value is not JsonValue v || !v.TryGetValue<string>(out var s) || !EnumMemberRegex().IsMatch(s))
                return false;
            to.Add(s);
        }
        return true;
    }

    static JsonNode? SampleAt(JsonNode? sample, string key) =>
        sample is JsonObject obj && obj.TryGetPropertyValue(key, out var node) ? node : null;

    static JsonNode? SampleAt(JsonNode? sample, int index) =>
        sample is JsonArray array && index < array.Count ? array[index] : null;

    static JsonNode? FirstValue(JsonNode? sample) =>
        sample is JsonObject { Count: > 0 } obj ? obj.First().Value : null;

    /// <summary>
    /// A schema is only a claim about the data — when an example disagrees with a string <c>format</c>,
    /// believe the example, so the generated types can still parse the document sitting next to them.
    /// </summary>
    static bool FormatFits(string? format, JsonNode? sample)
    {
        if (sample is not JsonValue value || !value.TryGetValue<string>(out var s))
            return true;
        return format switch
        {
            "date" => IsoDateRegex().IsMatch(s) || IsoDateTimeRegex().IsMatch(s),
            "date-time" => IsoDateTimeRegex().IsMatch(s),
            "uuid" => UuidRegex().IsMatch(s),
            _ => true,
        };
    }

    static JsonTypeRef ScalarFromSchema(JsonObject s, JsonNode? sample = null)
    {
        switch (SchemaTypeOf(s))
        {
            case "boolean":
                return Prim(Kinds.Boolean);
            case "integer":
                return Prim(s.GetDouble("maximum") > int.MaxValue || s.GetDouble("minimum") < int.MinValue
                    ? Kinds.Long
                    : Kinds.Integer);
            case "number":
                // a fractional multipleOf is the schema telling us this is a money-like decimal
                return Prim(s.GetDouble("multipleOf") is { } multipleOf && multipleOf < 1
                    ? Kinds.Decimal
                    : Kinds.Double);
            case "null":
                return Prim(Kinds.Any);
        }

        var format = s.GetString("format");
        return (FormatFits(format, sample) ? format : null) switch
        {
            "date" => Prim(Kinds.Date),
            "date-time" => Prim(Kinds.DateTime),
            "uuid" => Prim(Kinds.Uuid),
            _ => Prim(Kinds.String),
        };
    }

    static JsonTypeRef FromSchema(JsonNode? schema, JsonObject root, ModelBuilder model, string name,
        bool isRoot = false, JsonNode? sample = null)
    {
        var s = Resolve(schema, root);
        var nullable = SchemaNullable(s);
        var title = s.GetString("title");
        var typeName = !string.IsNullOrEmpty(title) ? Pascal(title) : Pascal(name);

        var enumValues = s.GetArray("enum");
        if (TryEnumValues(enumValues, out var values))
            return model.AddEnum(values, typeName, s.GetString("description")).WithNullable(nullable);
        if (enumValues != null || s.ContainsKey("const"))
            return ScalarFromSchema(s).WithNullable(nullable);

        switch (SchemaTypeOf(s))
        {
            case "object":
            {
                if (s.GetObject("properties") is { } properties)
                {
                    // Resolve() returns a fresh object each call, so recursion is tracked by $ref, not identity
                    var refPath = (schema as JsonObject).GetString("$ref");
                    if (refPath != null && model.TryGetRef(refPath, out var seen))
                        return new JsonTypeRef { Kind = Kinds.Ref, Name = seen, Nullable = nullable };

                    // a $ref'd schema is a named type: claim its name up front so it can reference itself
                    string? declared = null;
                    if (refPath != null)
                    {
                        declared = model.Reserve(title ?? refPath.LastRightPart('/'), isRoot);
                        model.SetRef(refPath, declared);
                    }

                    var required = new HashSet<string>(StringList(s.GetArray("required")));
                    var fields = new List<JsonTypeField>();
                    foreach (var entry in properties)
                    {
                        var p = Resolve(entry.Value, root);
                        fields.Add(new JsonTypeField
                        {
                            Key = entry.Key,
                            Description = p.GetString("description"),
                            Optional = !required.Contains(entry.Key),
                            Deprecated = p.GetBool("deprecated"),
                            Type = FromSchema(entry.Value, root, model, Singular(Pascal(entry.Key)),
                                sample: SampleAt(sample, entry.Key)),
                        });
                    }

                    return declared != null
                        ? model.PushObject(declared, fields).WithNullable(nullable)
                        : model.AddObject(fields, typeName, isRoot).WithNullable(nullable);
                }

                var extra = s["additionalProperties"];
                var value = extra is JsonObject
                    ? FromSchema(extra, root, model, typeName + "Value", sample: FirstValue(sample))
                    : Any();
                return new JsonTypeRef { Kind = Kinds.Map, Of = value, Nullable = nullable };
            }
            case "array":
            {
                if (s.GetArray("prefixItems") is { Count: > 0 } prefixItems)
                {
                    return new JsonTypeRef
                    {
                        Kind = Kinds.Tuple,
                        Items = prefixItems
                            .Select((p, i) => FromSchema(p, root, model, $"{typeName}{i + 1}",
                                sample: SampleAt(sample, i)))
                            .ToList(),
                        Nullable = nullable,
                    };
                }
                var items = s.ContainsKey("items")
                    ? FromSchema(s["items"], root, model, Singular(typeName), sample: SampleAt(sample, 0))
                    : Any();
                return new JsonTypeRef { Kind = Kinds.Array, Of = items, Nullable = nullable };
            }
            default:
                return ScalarFromSchema(s, sample).WithNullable(nullable);
        }
    }

    // ── Inference: JSON example ──

    static JsonTypeRef FromJson(JsonNode? value, ModelBuilder model, string name, bool isRoot = false)
    {
        if (value == null)
            return Any().WithNullable(true);

        if (value is JsonArray array)
        {
            if (array.Count == 0)
                return new JsonTypeRef { Kind = Kinds.Array, Of = Any() };

            // every element an object → merge their shapes into one type with optional fields
            if (array.All(x => x is JsonObject))
            {
                var order = new List<string>();
                var byKey = new Dictionary<string, List<JsonNode?>>();
                foreach (var item in array.Cast<JsonObject>())
                {
                    foreach (var entry in item)
                    {
                        if (!byKey.TryGetValue(entry.Key, out var list))
                        {
                            order.Add(entry.Key);
                            byKey[entry.Key] = list = [];
                        }
                        list.Add(entry.Value);
                    }
                }
                var fields = order.Map(key => new JsonTypeField
                {
                    Key = key,
                    Optional = byKey[key].Count < array.Count || byKey[key].Any(x => x == null),
                    Type = FromJsonMany(byKey[key], model, Singular(Pascal(key))),
                });
                return new JsonTypeRef
                {
                    Kind = Kinds.Array,
                    Of = model.AddObject(fields, Singular(Pascal(name))),
                };
            }
            return new JsonTypeRef
            {
                Kind = Kinds.Array,
                Of = FromJsonMany(array.ToList(), model, Singular(Pascal(name))),
            };
        }

        if (value is JsonObject obj)
        {
            var fields = obj.Map(entry => new JsonTypeField
            {
                Key = entry.Key,
                Optional = entry.Value == null,
                Type = FromJson(entry.Value, model, Singular(Pascal(entry.Key))),
            });
            return model.AddObject(fields, Pascal(name), isRoot);
        }

        if (value is JsonValue scalar)
        {
            if (scalar.TryGetValue<bool>(out _))
                return Prim(Kinds.Boolean);
            if (scalar.TryGetValue<string>(out var s))
            {
                if (UuidRegex().IsMatch(s)) return Prim(Kinds.Uuid);
                if (IsoDateTimeRegex().IsMatch(s)) return Prim(Kinds.DateTime);
                if (IsoDateRegex().IsMatch(s)) return Prim(Kinds.Date);
                return Prim(Kinds.String);
            }
            if (scalar.TryGetValue<long>(out var l))
                return Prim(l > int.MaxValue || l < int.MinValue ? Kinds.Long : Kinds.Integer);
            return Prim(Kinds.Double);
        }
        return Prim(Kinds.String);
    }

    /// <summary>The common type of several values, widening where they disagree</summary>
    static JsonTypeRef FromJsonMany(List<JsonNode?> values, ModelBuilder model, string name)
    {
        var present = values.Where(x => x != null).ToList();
        if (present.Count == 0)
            return Any().WithNullable(true);

        var types = present.Map(x => FromJson(x, model, name));
        var first = types[0];
        if (types.All(x => TypeKey(x) == TypeKey(first)))
            return first.WithNullable(present.Count < values.Count);
        // int + double → double, anything else → object
        if (types.All(x => x.Kind is Kinds.Integer or Kinds.Double))
            return Prim(Kinds.Double);
        return Any().WithNullable(true);
    }

    // ── Build ──

    static bool LooksLikeSchema(JsonNode? value) => value is JsonObject obj
        && (obj.ContainsKey("$schema") || obj.ContainsKey("properties") || obj.GetString("type") != null);

    /// <summary>Strips the ".ui.json"/".json" a template's companion files carry</summary>
    public static string StemOf(string? fileName)
    {
        var name = fileName ?? "";
        if (name.EndsWith(CoreToolsExtension.SchemaSuffix, StringComparison.OrdinalIgnoreCase))
            name = name[..^CoreToolsExtension.SchemaSuffix.Length];
        else if (name.LastIndexOf('.') > 0)
            name = name[..name.LastIndexOf('.')];
        return name.Length > 0 ? name : "Root";
    }

    /// <summary>
    /// Builds the type model for a document. The schema wins when supplied — the JSON example is still
    /// used to sanity-check its string formats. <paramref name="name"/> is the source file name, which
    /// names the root type (e.g. "invoice.ui.json" → Invoice).
    /// </summary>
    public static JsonTypesModel BuildModel(string name, JsonNode? json = null, JsonNode? schema = null)
    {
        var model = new ModelBuilder();
        var rootName = Pascal(StemOf(name));
        model.RootName = rootName;

        JsonTypeRef root;
        if (LooksLikeSchema(schema))
        {
            var schemaObj = (JsonObject)schema!;
            var title = schemaObj.GetString("title");
            root = FromSchema(schemaObj, schemaObj, model,
                !string.IsNullOrEmpty(title) ? Pascal(title) : rootName, isRoot: true, sample: json);

            // the document's own description has nowhere else to land — a nested type's would only repeat
            // the doc comment already on the property that points at it
            if (root.Kind == Kinds.Ref && schemaObj.GetString("description") is { } description
                && model.Types.FirstOrDefault(x => x.Name == root.Name) is { } rootType)
                rootType.Description = description;
        }
        else
        {
            root = FromJson(json, model, rootName, isRoot: true);
        }

        // an unnamed root (array or scalar) still needs a home
        if (root.Kind != Kinds.Ref)
            model.Types.Add(new JsonTypeDef
            {
                Kind = "alias", Name = model.UniqueName(rootName, "alias"), Type = root,
            });

        return new JsonTypesModel { Types = model.Types, Root = root };
    }

    // ── C# emitter ──

    /// <summary>How the C# emitter frames the types it writes</summary>
    public class CSharpOptions
    {
        /// <summary>File-scoped namespace the types are emitted into, or null for none</summary>
        public string? Namespace { get; set; }

        /// <summary>Extra usings, added after the ones the types themselves need</summary>
        public List<string> Usings { get; set; } = [];

        /// <summary>Attributes to emit on the root type, without brackets (e.g. <c>Pdf("invoice")</c>)</summary>
        public List<string> RootAttributes { get; set; } = [];

        /// <summary>Comment block emitted above the usings, e.g. an auto-generated header</summary>
        public string? Header { get; set; }
    }

    static readonly Dictionary<string, string> CSharpTypes = new()
    {
        [Kinds.String] = "string", [Kinds.Integer] = "int", [Kinds.Long] = "long",
        [Kinds.Double] = "double", [Kinds.Decimal] = "decimal", [Kinds.Boolean] = "bool",
        [Kinds.Date] = "DateTime", [Kinds.DateTime] = "DateTime", [Kinds.Uuid] = "Guid",
        [Kinds.Any] = "object",
    };

    static readonly string[] CSharpValueKinds =
    [
        Kinds.Integer, Kinds.Long, Kinds.Double, Kinds.Decimal, Kinds.Boolean,
        Kinds.Date, Kinds.DateTime, Kinds.Uuid,
    ];

    static string CSharpType(JsonTypeRef t) => t.Kind switch
    {
        Kinds.Array => $"List<{CSharpType(t.Of ?? Any())}>",
        Kinds.Map => $"Dictionary<string, {CSharpType(t.Of ?? Any())}>",
        Kinds.Tuple => $"({string.Join(", ", (t.Items ?? []).Select(CSharpType))})",
        Kinds.Ref => t.Name ?? "object",
        _ => CSharpTypes.TryGetValue(t.Kind, out var cs) ? cs : "object",
    };

    /// <summary>Enums are value types too, so they must never be initialised with <c>= null!</c></summary>
    static bool IsCSharpValueType(JsonTypeRef t, JsonTypesModel model) =>
        CSharpValueKinds.Contains(t.Kind)
        || t.Kind == Kinds.Tuple
        || (t.Kind == Kinds.Ref && model.Types.Any(x => x.Name == t.Name && x.Kind == "enum"));

    /// <summary>Every kind the model actually uses, so only the needed usings are emitted</summary>
    static HashSet<string> KindsUsed(List<JsonTypeDef> types)
    {
        var kinds = new HashSet<string>();
        void Visit(JsonTypeRef? t)
        {
            if (t == null) return;
            kinds.Add(t.Kind);
            if (t.Of != null) Visit(t.Of);
            if (t.Items != null) t.Items.Each(Visit);
        }
        foreach (var type in types)
        {
            if (type.Kind == "object") type.Fields.Each(x => Visit(x.Type));
            else if (type.Kind == "alias") Visit(type.Type);
            else kinds.Add(type.Kind);
        }
        return kinds;
    }

    static void AppendDoc(StringBuilder sb, string? text, string indent)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;
        var lines = text!.Replace("\r\n", "\n").Split('\n')
            .Select(x => x.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"))
            .ToList();
        if (lines.Count == 1)
        {
            sb.Append(indent).Append("/// <summary>").Append(lines[0]).AppendLine("</summary>");
            return;
        }
        sb.Append(indent).AppendLine("/// <summary>");
        foreach (var line in lines)
            sb.Append(indent).Append("/// ").AppendLine(line);
        sb.Append(indent).AppendLine("/// </summary>");
    }

    /// <summary>Emits the model as C# classes, enums and doc comments</summary>
    public static string ToCSharp(JsonTypesModel model, CSharpOptions? options = null)
    {
        options ??= new CSharpOptions();
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(options.Header))
        {
            sb.AppendLine(options.Header!.TrimEnd());
            sb.AppendLine();
        }

        var kinds = KindsUsed(model.Types);
        var usings = new List<string>();
        // DateTime/Guid live in System, which isn't implicit in every project
        if (kinds.Overlaps([Kinds.Date, Kinds.DateTime, Kinds.Uuid]))
            usings.Add("System");
        if (kinds.Overlaps([Kinds.Array, Kinds.Map]))
            usings.Add("System.Collections.Generic");
        usings.Add("System.Text.Json.Serialization");
        foreach (var ns in options.Usings)
        {
            if (!string.IsNullOrEmpty(ns) && !usings.Contains(ns))
                usings.Add(ns);
        }
        usings.Each(x => sb.Append("using ").Append(x).AppendLine(";"));
        sb.AppendLine();

        if (!string.IsNullOrEmpty(options.Namespace))
        {
            sb.Append("namespace ").Append(options.Namespace).AppendLine(";");
            sb.AppendLine();
        }

        var rootTypeName = model.RootTypeName;
        foreach (var type in model.Types)
        {
            if (type.Kind == "alias")
            {
                sb.Append("// root: ").AppendLine(CSharpType(type.Type ?? Any()));
                sb.AppendLine();
                continue;
            }

            AppendDoc(sb, type.Description, "");
            if (type.Name == rootTypeName)
                options.RootAttributes.Each(x => sb.Append('[').Append(x).AppendLine("]"));

            if (type.Kind == "enum")
            {
                sb.Append("public enum ").AppendLine(type.Name);
                sb.AppendLine("{");
                foreach (var value in type.Values)
                    sb.Append("    ").Append(Safe(Pascal(value))).AppendLine(",");
                sb.AppendLine("}");
                sb.AppendLine();
                continue;
            }

            sb.Append("public class ").AppendLine(type.Name);
            sb.AppendLine("{");
            for (var i = 0; i < type.Fields.Count; i++)
            {
                var field = type.Fields[i];
                if (i > 0)
                    sb.AppendLine();
                AppendDoc(sb, field.Description, "    ");
                if (field.Deprecated)
                    sb.AppendLine("    [System.Obsolete]");
                sb.Append("    [JsonPropertyName(\"").Append(field.Key.Replace("\\", "\\\\").Replace("\"", "\\\""))
                    .AppendLine("\")]");

                var name = MemberName(type, field);
                var optional = field.Optional || field.Type.Nullable;
                var cs = CSharpType(field.Type);
                var init = optional || IsCSharpValueType(field.Type, model)
                    ? ""
                    : field.Type.Kind is Kinds.Array or Kinds.Map
                        ? " = new();"
                        : " = null!;";
                sb.Append("    public ").Append(optional ? cs + "?" : cs).Append(' ').Append(name)
                    .Append(" { get; set; }").AppendLine(init);
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }

        var source = sb.ToString().Replace("\r\n", "\n");
        return BlankLinesRegex().Replace(source, "\n\n").Trim() + "\n";
    }

    /// <summary>Build and emit in one step</summary>
    public static string GenerateCSharp(string name, JsonNode? json = null, JsonNode? schema = null,
        CSharpOptions? options = null) => ToCSharp(BuildModel(name, json, schema), options);

    /// <summary>The property name a field is emitted as, which the initialiser has to agree with</summary>
    static string MemberName(JsonTypeDef type, JsonTypeField field)
    {
        var name = Safe(Pascal(field.Key));
        // a member can't share its enclosing type's name
        return name == type.Name ? name + "Value" : name;
    }

    /// <summary>How deep an initialiser expands before it settles for <c>new X()</c></summary>
    const int MaxInitDepth = 4;

    /// <summary>
    /// A placeholder object initialiser for a value of this type, spelling out every member the template
    /// reads — an unset member is omitted from the JSON entirely and typst fails on the missing key, so a
    /// stub that names them all is the point of the copy/paste examples.
    /// <para><paramref name="indent"/> is the leading whitespace of the line the value starts on.</para>
    /// </summary>
    public static string ToCSharpInitializer(JsonTypesModel model, JsonTypeRef type, string indent = "",
        int depth = 0, HashSet<string>? path = null)
    {
        path ??= [];
        switch (type.Kind)
        {
            case Kinds.Ref:
            {
                var def = model.Types.FirstOrDefault(x => x.Name == type.Name);
                if (def == null || def.Kind != "object")
                    return "default"; // an enum, or a name we never emitted
                // a recursive document would otherwise expand forever, and past a few levels the stub
                // stops being something anyone reads
                if (def.Fields.Count == 0 || depth >= MaxInitDepth || !path.Add(def.Name))
                    return $"new {def.Name}()";

                var sb = new StringBuilder();
                sb.Append("new ").Append(def.Name).Append('\n').Append(indent).Append('{');
                foreach (var field in def.Fields)
                {
                    var value = ToCSharpInitializer(model, field.Type, indent + "    ", depth + 1, path);
                    sb.Append('\n').Append(indent).Append("    ").Append(MemberName(def, field))
                        // a value that opens on its own line mustn't leave a space dangling on this one
                        .Append(value.StartsWith('\n') ? " =" : " = ").Append(value).Append(',');
                }
                sb.Append('\n').Append(indent).Append('}');
                path.Remove(def.Name);
                return sb.ToString();
            }
            case Kinds.Array:
            {
                var of = type.Of ?? Any();
                if (of.Kind != Kinds.Ref || depth >= MaxInitDepth)
                    return "[]";
                // one example element, so the row type's members are visible too
                var element = ToCSharpInitializer(model, of, indent + "    ", depth + 1, path);
                return element == "default"
                    ? "[]"
                    : $"\n{indent}[\n{indent}    {element},\n{indent}]";
            }
            case Kinds.Map: return "new()";
            case Kinds.String: return "\"\"";
            case Kinds.Integer:
            case Kinds.Long: return "0";
            case Kinds.Double: return "0";
            case Kinds.Decimal: return "0m";
            case Kinds.Boolean: return "false";
            case Kinds.Date: return "DateTime.Today";
            case Kinds.DateTime: return "DateTime.UtcNow";
            case Kinds.Uuid: return "Guid.NewGuid()";
            default: return "default";
        }
    }
}
