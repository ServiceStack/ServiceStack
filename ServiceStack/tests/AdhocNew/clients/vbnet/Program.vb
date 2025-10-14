Imports System
Imports ServiceStack
Imports ServiceStack.AI

Module Program
    Sub Main(args As String())
        Dim client = New JsonApiClient("https://localhost:5001")
        client.BearerToken = "ak-87949de37e894627a9f6173154e7cafa"

        Dim api = client.Api(New ChatCompletion With {
            .Model = "gemini-flash-latest",
            .Messages = New List(Of AiMessage) From {
                New AiMessage With {
                    .Role = "user",
                    .Content = New List(Of AiContent) From {
                        New AiTextContent With {
                            .Type = "text",
                            .Text = "Capital of France?"
                        }
                    }
                }
            }
        })

        ClientConfig.PrintDump(api.Response)
    End Sub
End Module
