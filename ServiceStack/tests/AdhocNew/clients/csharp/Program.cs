using ServiceStack;
using ServiceStack.AI;

var client = new JsonApiClient("http://localhost:5000");
client.BearerToken = "ak-87949de37e894627a9f6173154e7cafa";

var api = client.Api(new ChatCompletion {
    Model = "openai/gpt-oss-120b",
    Messages = [
        new AiMessage {
            Role = "user",
            Content = [
                new AiTextContent {
                    Type = "text",
                    Text = "Capital of France?"
                }
            ]
        }
    ]
});

ClientConfig.PrintDump(api.Response);