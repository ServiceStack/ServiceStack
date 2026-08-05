open ServiceStack
open ServiceStack.AI

[<EntryPoint>]
let main argv =
    let client = new JsonApiClient("http://localhost:5000")
    client.BearerToken <- "ak-87949de37e894627a9f6173154e7cafa"

    let textContent = new AiTextContent(Type = "text", Text = "Capital of France?")
    let content = ResizeArray<AiContent>([textContent :> AiContent])

    let message = new AiMessage(Role = "user", Content = content)
    let messages = ResizeArray<AiMessage>([message])

    let request = new ChatCompletion(Model = "openai/gpt-oss-120b", Messages = messages)

    let api = client.Api(request)

    ClientConfig.PrintDump(api.Response)
    0 // return an integer exit code

