package org.example

import net.servicestack.client.JsonServiceClient
import net.servicestack.client.Inspect
import net.servicestack.client.Utils

fun main() {
    // Create a ServiceStack JSON client
    val client = JsonServiceClient("http://localhost:5000")
    client.bearerToken = "ak-87949de37e894627a9f6173154e7cafa"

    // Create request
    val request = ChatCompletion().apply {
        model = "gemini-flash-latest"
        messages = arrayListOf(
            AiMessage().apply {
                role = "user"
                content = arrayListOf(
                    AiTextContent().apply {
                        text = "What's the capital of France?"
                        Type = "text"
                    }
                )
            }
        )
        maxCompletionTokens = 50
    }

    val response = client.send(request)
    
    Inspect.printDump(response)
}

