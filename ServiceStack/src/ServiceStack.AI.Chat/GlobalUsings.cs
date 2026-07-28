// The chat pipeline models all UI API payloads as System.Text.Json nodes;
// alias them project-wide to avoid clashing with ServiceStack.Text.JsonObject/JsonValue.
global using JsonNode = System.Text.Json.Nodes.JsonNode;
global using JsonObject = System.Text.Json.Nodes.JsonObject;
global using JsonArray = System.Text.Json.Nodes.JsonArray;
global using JsonValue = System.Text.Json.Nodes.JsonValue;
