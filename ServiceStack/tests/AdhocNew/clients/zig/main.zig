const std = @import("std");
const ss = @import("servicestack");
const dtos = @import("dtos.zig");

pub fn main() !void {
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    defer _ = gpa.deinit();
    const allocator = gpa.allocator();

    var client = try ss.JsonServiceClient.init(allocator, "http://localhost:5000");
    defer client.deinit();

    client.setBearerToken("ak-87949de37e894627a9f6173154e7cafa");

    var part = std.json.ObjectMap.init(allocator);
    defer part.deinit();
    try part.put("type", std.json.Value{ .string = "text" });
    try part.put("text", std.json.Value{ .string = "Capital of France?" });

    var content_buf = [_]std.json.Value{.{ .object = part }};
    var messages_buf = [_]dtos.AiMessage{
        .{
            .role = "user",
            .content = content_buf[0..],
        },
    };

    const request = dtos.ChatCompletion{
        .model = "openai/gpt-oss-120b",
        .messages = messages_buf[0..],
    };

    var parsed = try client.send(request);
    defer parsed.deinit();

    if (parsed.value.choices.len > 0) {
        if (parsed.value.choices[0].message) |msg| {
            if (msg.content) |content| {
                std.debug.print("Content: {s}\n", .{content});
            }
            if (msg.reasoning) |reasoning| {
                std.debug.print("Reasoning: {s}\n", .{reasoning});
            }
        }
    }
}
