#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.AiTests;

public class AiChatMigrationJevContractTests
{
    static JsonObject Vectors=>JsonNode.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"AiChatFixtures","jev-contract-vectors.json")))!.AsObject();
    static IEnumerable<TestCaseData> Hashes()=>Vectors["hashes"]!.AsArray().Select((x,i)=>new TestCaseData(x!.AsObject()).SetName("Jev_Python_document_hash_"+i));
    static IEnumerable<TestCaseData> Recipes()=>Vectors["recipes"]!.AsArray().Select(x=>new TestCaseData(x!.AsObject()).SetName("Jev_recipe_compile_normalize_"+x!["name"]!.GetValue<string>()));
    static JsonObject Starter=>Vectors["recipes"]!.AsArray().First(x=>x!["name"]!.GetValue<string>()=="support")!["document"]!.AsObject().Clone();
    [TestCaseSource(nameof(Hashes))]
    public void Document_hash_and_sorted_encoding_match_frozen_Python(JsonObject vector)
    {
        Assert.That(JevJson.Encode(vector["value"],true,true),Is.EqualTo(vector.GetString("encoded")));Assert.That(JevJson.Hash(vector["value"]),Is.EqualTo(vector.GetString("hash")));
    }
    [TestCaseSource(nameof(Recipes))]
    public void Bundled_recipes_compile_documentation_inputs_without_presentation_or_examples(JsonObject vector)
    {
        var doc=DecisionRecipeValidator.Validate(vector["document"]);var original=doc.ToJsonString();var examples=doc["examples"]!.AsArray();
        for(var i=0;i<examples.Count;i++){var request=DecisionRecipeValidator.Compile(doc,examples[i]!["input"]);Assert.That(JsonNode.DeepEquals(request,vector["compiled"]![i]),Is.True);Assert.That(request.ContainsKey("examples"),Is.False);Assert.That(request.ContainsKey("presentation"),Is.False);}
        Assert.That(JsonNode.DeepEquals(DecisionRecorded.Normalize(vector["response"],doc["questions"]!.AsObject()),vector["answers"]),Is.True);Assert.That(doc.ToJsonString(),Is.EqualTo(original));
    }
    [Test]
    public void Frozen_recorded_sharing_contract_accepts_valid_examples_and_rejects_private_or_malformed_results()
    {
        var cases=JsonNode.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"AiChatFixtures","jev-sharing-contract.json")))!.AsArray();
        foreach(var item in cases){var row=item!.AsObject();void Validate(){JevPaths.Filename(row.GetString("filename"));var doc=DecisionRecipeValidator.Validate(row["document"]);DecisionRecorded.Validate(doc,row["execution"]);}
            if(row.GetBool("valid"))Assert.DoesNotThrow(Validate,row.GetString("name"));else Assert.Catch<ArgumentException>(Validate,row.GetString("name"));
        }
    }
    [Test]
    public void Contract_rejects_remote_schema_reserved_keys_unknown_input_and_nonfinite_values_with_field_paths()
    {
        var doc=Starter;doc["inputSchema"]!["properties"]!["ticket"]!["$ref"]="https://example.com/schema";Assert.That(Assert.Throws<JevValidationException>(()=>DecisionRecipeValidator.Validate(doc))!.Path,Is.EqualTo("inputSchema.properties.ticket"));
        foreach(var change in new[]{"future","criteria","instructions","labels","owner"}) {
            doc=Starter;
            switch(change){case "future":doc["schemaVersion"]=2;break;case "criteria":doc["questions"]!["urgency"]!["criteria"]=new JsonArray("Only one");break;case "instructions":doc["questions"]!["team"]!["instructions"]=new JsonObject();break;case "labels":doc["presentation"]!["questions"]!["missing"]=new JsonObject {["label"]="Gone"};break;case "owner":doc["user"]="other";break;}
            Assert.Throws<JevValidationException>(()=>DecisionRecipeValidator.Validate(doc),change);
        }
        doc=Starter;var input=doc["examples"]![0]!["input"]!.AsObject().Clone();input["secret"]="extra";Assert.Throws<JevValidationException>(()=>DecisionRecipeValidator.Compile(doc,input));
        input.Remove("secret");input["ticket"]="  ";Assert.That(Assert.Throws<JevValidationException>(()=>DecisionRecipeValidator.Compile(doc,input))!.Path,Is.EqualTo("input.ticket"));
        Assert.Throws<JevValidationException>(()=>JevJson.Copy(JsonNode.Parse("""{"value":1e999}""")));
        doc=Starter;doc["inputSchema"]!["properties"]!["constructor"]=new JsonObject {["type"]="string"};Assert.Throws<JevValidationException>(()=>DecisionRecipeValidator.Validate(doc));
    }
    [Test]
    public void False_and_zero_are_required_values_and_text_mapping_remains_literal()
    {
        var doc=Starter;doc["examples"]=new JsonArray();doc["inputSchema"]=JsonNode.Parse("""{"type":"object","properties":{"enabled":{"type":"boolean"},"count":{"type":"integer"}},"required":["enabled","count"]}""");
        Assert.DoesNotThrow(()=>DecisionRecipeValidator.Compile(doc,new JsonObject {["enabled"]=false,["count"]=0}));
        doc=Starter;doc["state"]=new JsonObject {["mode"]="text",["field"]="ticket"};var input=doc["examples"]![0]!["input"]!.AsObject();Assert.That(DecisionRecipeValidator.Compile(doc,input)["state"]!.GetValue<string>(),Is.EqualTo(input["ticket"]!.GetValue<string>()));
        doc["state"]!["field"]="missing";Assert.Throws<JevValidationException>(()=>DecisionRecipeValidator.Validate(doc));
    }
    [Test]
    public void Normalization_rejects_missing_keys_bad_probabilities_and_changed_rubrics()
    {
        var vector=Vectors["recipes"]!.AsArray().First(x=>x!["name"]!.GetValue<string>()=="support")!.AsObject();var questions=vector["document"]!["questions"]!.AsObject();
        foreach(var mutation in new[]{"missing","sum","choice","confidence","score","legend","type","noul"}) {
            var response=vector["response"]!.AsObject().Clone();var answers=response["answers"]!.AsObject();
            switch(mutation){case "missing":answers.Remove("team");break;case "sum":answers["team"]!["probabilities"]!.AsObject().First().Value!.ReplaceWith(0.01);break;case "choice":answers["team"]!["choice"]="__missing";break;case "confidence":answers["team"]!["confidence"]=2;break;case "score":answers["urgency"]!["score"]=-1;break;case "legend":answers["urgency"]!["legend"]!["0"]="Changed";break;case "type":answers["is_bug"]!["type"]="choice";break;case "noul":answers["is_bug"]!["noul"]=-0.1;break;}
            Assert.Throws<JevValidationException>(()=>DecisionRecorded.Normalize(response,questions),mutation);
        }
    }
}
