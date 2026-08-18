#nullable enable

using System;
using System.Text.Json.Nodes;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.Extensions.Tests;

public class McpConfirmationTokenTests
{
    private const string SecretKey = "test-secret-key-1234567890-test-secret";

    [Test]
    public void Token_can_be_generated_and_validated_successfully()
    {
        var manager = new McpConfirmationTokenManager(SecretKey, TimeSpan.FromMinutes(5));
        var args = new JsonObject
        {
            ["CustomerName"] = "Sam",
            ["Items"] = new JsonArray
            {
                new JsonObject { ["ProductId"] = 7, ["Quantity"] = 2 }
            }
        };

        var token = manager.CreateToken("user1@email.com", "api_call", "CreateCoffeeShopOrder", args);
        Assert.That(token, Does.StartWith("mcp_cf_"));

        var result = manager.ValidateToken(token, "user1@email.com", "api_call", "CreateCoffeeShopOrder", args);
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Payload, Is.Not.Null);
        Assert.That(result.Payload!.Sub, Is.EqualTo("user1@email.com"));
        Assert.That(result.Payload.Target, Is.EqualTo("CreateCoffeeShopOrder"));
    }

    [Test]
    public void Token_validation_fails_if_arguments_are_modified()
    {
        var manager = new McpConfirmationTokenManager(SecretKey, TimeSpan.FromMinutes(5));
        var originalArgs = new JsonObject
        {
            ["CustomerName"] = "Sam",
            ["Quantity"] = 2
        };

        var token = manager.CreateToken("user1@email.com", "api_call", "CreateCoffeeShopOrder", originalArgs);

        var modifiedArgs = new JsonObject
        {
            ["CustomerName"] = "Sam",
            ["Quantity"] = 100 // Tampered!
        };

        var result = manager.ValidateToken(token, "user1@email.com", "api_call", "CreateCoffeeShopOrder", modifiedArgs);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Arguments have been modified"));
    }

    [Test]
    public void Token_validation_fails_if_target_api_mismatches()
    {
        var manager = new McpConfirmationTokenManager(SecretKey, TimeSpan.FromMinutes(5));
        var args = new JsonObject { ["Id"] = 1 };

        var token = manager.CreateToken("user1@email.com", "api_call", "UpdateOrder", args);

        var result = manager.ValidateToken(token, "user1@email.com", "api_call", "DeleteOrder", args);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Target API mismatch"));
    }

    [Test]
    public void Token_validation_fails_if_user_mismatches()
    {
        var manager = new McpConfirmationTokenManager(SecretKey, TimeSpan.FromMinutes(5));
        var args = new JsonObject { ["Id"] = 1 };

        var token = manager.CreateToken("user1@email.com", "api_call", "UpdateOrder", args);

        var result = manager.ValidateToken(token, "user2@email.com", "api_call", "UpdateOrder", args);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("User mismatch"));
    }

    [Test]
    public void Token_validation_fails_if_signature_is_tampered()
    {
        var manager = new McpConfirmationTokenManager(SecretKey, TimeSpan.FromMinutes(5));
        var args = new JsonObject { ["Id"] = 1 };

        var token = manager.CreateToken("user1@email.com", "api_call", "UpdateOrder", args);
        var tamperedToken = token[..^4] + "AAAA";

        var result = manager.ValidateToken(tamperedToken, "user1@email.com", "api_call", "UpdateOrder", args);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Invalid token signature"));
    }

    [Test]
    public void Token_validation_fails_if_token_is_expired()
    {
        // Negative expiry time for expired token test
        var manager = new McpConfirmationTokenManager(SecretKey, TimeSpan.FromSeconds(-10));
        var args = new JsonObject { ["Id"] = 1 };

        var token = manager.CreateToken("user1@email.com", "api_call", "UpdateOrder", args);

        var result = manager.ValidateToken(token, "user1@email.com", "api_call", "UpdateOrder", args);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("expired"));
    }

    [Test]
    public void Token_cannot_be_replayed_once_used()
    {
        var manager = new McpConfirmationTokenManager(SecretKey, TimeSpan.FromMinutes(5));
        var args = new JsonObject { ["Id"] = 1 };

        var token = manager.CreateToken("user1@email.com", "api_call", "UpdateOrder", args);

        // First use: Success
        var firstResult = manager.ValidateToken(token, "user1@email.com", "api_call", "UpdateOrder", args);
        Assert.That(firstResult.IsValid, Is.True);

        // Second use: Replay blocked
        var secondResult = manager.ValidateToken(token, "user1@email.com", "api_call", "UpdateOrder", args);
        Assert.That(secondResult.IsValid, Is.False);
        Assert.That(secondResult.ErrorMessage, Does.Contain("already been used"));
    }

    [Test]
    public void Arguments_canonical_hash_is_order_independent_for_json_properties()
    {
        var args1 = new JsonObject
        {
            ["alpha"] = "1",
            ["beta"] = "2",
            ["nested"] = new JsonObject
            {
                ["x"] = 10,
                ["y"] = 20
            }
        };

        var args2 = new JsonObject
        {
            ["nested"] = new JsonObject
            {
                ["y"] = 20,
                ["x"] = 10
            },
            ["beta"] = "2",
            ["alpha"] = "1"
        };

        var hash1 = McpConfirmationTokenManager.ComputeArgumentsHash(args1);
        var hash2 = McpConfirmationTokenManager.ComputeArgumentsHash(args2);

        Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void Creates_well_structured_requires_confirmation_response()
    {
        var args = new JsonObject { ["CustomerName"] = "Sam" };
        var response = McpConfirmationTokenManager.CreateRequiresConfirmationResponse(
            "CreateCoffeeShopOrder",
            "Write",
            "mcp_cf_test_token",
            300,
            "Create validated coffee shop order",
            args);

        Assert.That(response["status"]!.GetValue<string>(), Is.EqualTo("requires_confirmation"));
        Assert.That(response["api"]!.GetValue<string>(), Is.EqualTo("CreateCoffeeShopOrder"));
        Assert.That(response["safety"]!.GetValue<string>(), Is.EqualTo("Write"));
        Assert.That(response["confirmationToken"]!.GetValue<string>(), Is.EqualTo("mcp_cf_test_token"));
        Assert.That(response["expiresInSeconds"]!.GetValue<int>(), Is.EqualTo(300));
        Assert.That(response["instruction"]!.GetValue<string>(), Does.Contain("Display this summary"));
        // instruction MUST NOT embed the raw token — the token is already in the structured field.
        Assert.That(response["instruction"]!.GetValue<string>(), Does.Not.Contain("mcp_cf_test_token"));
    }

    [Test]
    public void Arguments_hash_is_stable_across_numeric_reformatting()
    {
        // Models routinely re-emit JSON with different numeric formatting between Phase 1
        // and Phase 2. All of these must hash identically.
        var a = new JsonObject { ["Quantity"] = 2 };
        var b = new JsonObject { ["Quantity"] = 2.0 };
        var c = new JsonObject { ["Quantity"] = 2.00m };

        var h1 = McpConfirmationTokenManager.ComputeArgumentsHash(a);
        var h2 = McpConfirmationTokenManager.ComputeArgumentsHash(b);
        var h3 = McpConfirmationTokenManager.ComputeArgumentsHash(c);

        Assert.That(h1, Is.EqualTo(h2));
        Assert.That(h1, Is.EqualTo(h3));

        // A different value must still produce a different hash.
        var d = new JsonObject { ["Quantity"] = 3 };
        Assert.That(McpConfirmationTokenManager.ComputeArgumentsHash(d), Is.Not.EqualTo(h1));
    }

    [Test]
    public void Arguments_hash_is_stable_across_parsed_and_constructed_numbers()
    {
        // args reconstructed via JsonNode.Parse (JsonElement-backed) must hash the same as
        // args built up in-memory (raw-CLR-backed).
        var parsed = System.Text.Json.Nodes.JsonNode.Parse("{\"Quantity\":2,\"Ratio\":1.5}")!.AsObject();
        var built  = new JsonObject { ["Quantity"] = 2, ["Ratio"] = 1.5 };

        Assert.That(
            McpConfirmationTokenManager.ComputeArgumentsHash(parsed),
            Is.EqualTo(McpConfirmationTokenManager.ComputeArgumentsHash(built)));
    }

    [Test]
    public async System.Threading.Tasks.Task McpExtension_two_phase_confirmation_workflow()
    {
        var feature = new ChatFeature
        {
            RequireAuth = false,
        };
        feature.ChatAuth = new IdentityChatAuth(feature);
        var mcp = new McpExtension
        {
            ToolGroups = ["all"],
            ApprovalMode = McpApprovalMode.ConfirmationToken,
            SigningSecret = SecretKey,
        };
        feature.Extensions.Add(mcp);

        var extCtx = new ExtensionContext(feature, "mcp");
        mcp.Ctx = extCtx;

        var toolExecuted = false;
        extCtx.RegisterTool(new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = "delete_order",
                ["description"] = "Deletes an order",
                ["parameters"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["orderId"] = new JsonObject { ["type"] = "integer" }
                    }
                }
            }
        }, (args, ctx) =>
        {
            toolExecuted = true;
            return System.Threading.Tasks.Task.FromResult<object?>(new JsonObject { ["success"] = true, ["deleted"] = args["orderId"]?.GetValue<int>() });
        }, group: "test", approvalHandler: (args, ctx) =>
        {
            return System.Threading.Tasks.Task.FromResult<ChatToolApprovalRequest?>(new ChatToolApprovalRequest
            {
                Title = "delete_order",
                Description = "Delete order from database",
                Safety = ToolSafety.Destructive,
                Schema = new JsonObject(),
                Arguments = args.Clone()
            });
        }, safety: ToolSafety.Destructive);

        mcp.Install(extCtx);

        // Turn 1: Calling without token returns requires_confirmation
        var callArgs = new JsonObject
        {
            ["name"] = "delete_order",
            ["arguments"] = new JsonObject
            {
                ["orderId"] = 42
            }
        };

        var handleMethod = typeof(McpExtension).GetMethod("HandleMessageAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        var message1 = new JsonObject
        {
            ["id"] = 1,
            ["method"] = "tools/call",
            ["params"] = callArgs
        };

        var reqContext = new ChatRequestContext(feature, new ServiceStack.Host.BasicRequest(), new System.Collections.Generic.Dictionary<string, string>());
        var response1 = (JsonObject?)await (System.Threading.Tasks.Task<JsonObject?>)handleMethod.Invoke(mcp, [message1, reqContext])!;

        Assert.That(response1, Is.Not.Null, "response1 was null");
        Assert.That(response1!.ContainsKey("error"), Is.False, () => $"Error response: {response1.ToJsonString()}");
        var result1 = response1["result"]!.AsObject();
        var structured1 = result1["structuredContent"]!.AsObject();

        Assert.That(structured1["status"]!.GetValue<string>(), Is.EqualTo("requires_confirmation"));
        Assert.That(toolExecuted, Is.False); // Not executed yet!

        var token = structured1["confirmationToken"]!.GetValue<string>();
        Assert.That(token, Does.StartWith("mcp_cf_"));

        // Turn 2: Calling with token executes the tool!
        var message2 = new JsonObject
        {
            ["id"] = 2,
            ["method"] = "tools/call",
            ["params"] = new JsonObject
            {
                ["name"] = "delete_order",
                ["arguments"] = new JsonObject
                {
                    ["orderId"] = 42,
                    ["confirmationToken"] = token
                }
            }
        };

        var response2 = (JsonObject?)await (System.Threading.Tasks.Task<JsonObject?>)handleMethod.Invoke(mcp, [message2, reqContext])!;
        Assert.That(response2, Is.Not.Null);
        var result2 = response2!["result"]!.AsObject();
        var structured2 = result2["structuredContent"]!.AsObject();

        Assert.That(toolExecuted, Is.True); // Executed now!
        Assert.That(structured2["success"]!.GetValue<bool>(), Is.True);
        Assert.That(structured2["deleted"]!.GetValue<int>(), Is.EqualTo(42));
    }
}
