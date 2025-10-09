import Foundation
#if canImport(FoundationNetworking)
import FoundationNetworking
#endif
import ServiceStack

// Main application
func main() {
    print("ServiceStack Swift Console App Example")
    print("======================================\n")

    // Create a ServiceStack client
    let client = JsonServiceClient(baseUrl: "http://localhost:5166")
    client.bearerToken = "ak-87949de37e894627a9f6173154e7cafa"

    // Create a ChatCompletion request
    let request = ChatCompletion()
    request.model = "gemini-flash-latest"

    let message = AiMessage()
    message.role = "user"

    let textContent = AiTextContent()
    textContent.type = "text"
    textContent.text = "What's the capital of France?"

    message.content = [textContent]
    request.messages = [message]
    request.max_completion_tokens = 50

    // Make the request
    do {
        let response: ChatResponse = try client.send(request)
        print("Response received:")
        Inspect.printDump(response)
    } catch {
        print("Error: \(error)")
    }

}

// Run the application
main()