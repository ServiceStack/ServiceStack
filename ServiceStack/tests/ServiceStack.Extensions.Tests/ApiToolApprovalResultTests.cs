#nullable enable

using System.Reflection;
using System.Text.Json.Nodes;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.Extensions.Tests;

public class ApiToolApprovalResultTests
{
    [Test]
    public void Tool_result_tells_the_model_when_the_user_changed_arguments()
    {
        var result = ToolResult(
            new JsonObject { ["Text"] = "approval test", ["IsFinished"] = false },
            new JsonObject { ["Text"] = "approval test", ["IsFinished"] = true });

        var approval = result["approval"]!.AsObject();
        Assert.That(approval["decision"]!.GetValue<string>(), Is.EqualTo("approved"));
        Assert.That(approval["argumentsModifiedByUser"]!.GetValue<bool>(), Is.True);
        Assert.That(approval["message"]!.GetValue<string>(), Does.Contain("supersede proposedArguments"));
        Assert.That(result["proposedArguments"]!["IsFinished"]!.GetValue<bool>(), Is.False);
        Assert.That(result["effectiveArguments"]!["IsFinished"]!.GetValue<bool>(), Is.True);
        Assert.That(result["arguments"]!["IsFinished"]!.GetValue<bool>(), Is.True);
    }

    [Test]
    public void Tool_result_tells_the_model_when_arguments_were_unchanged()
    {
        var proposed = new JsonObject { ["Text"] = "approval test", ["IsFinished"] = false };
        var result = ToolResult(proposed, proposed.Clone());

        var approval = result["approval"]!.AsObject();
        Assert.That(approval["argumentsModifiedByUser"]!.GetValue<bool>(), Is.False);
        Assert.That(approval["message"]!.GetValue<string>(), Does.Contain("without changes"));
    }

    static JsonObject ToolResult(JsonObject proposed, JsonObject effective)
    {
        var method = typeof(ApiToolApprovalCoordinator).GetMethod("ToolResult",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        var json = (string)method.Invoke(null,
            ["approved", "CreateTodo", proposed, effective, new JsonObject { ["id"] = 1 }, null])!;
        return ChatJson.ParseObject(json);
    }
}
