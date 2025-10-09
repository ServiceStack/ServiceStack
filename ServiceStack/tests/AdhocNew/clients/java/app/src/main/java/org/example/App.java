package org.example;

import net.servicestack.client.JsonServiceClient;
import net.servicestack.client.Inspect;
import net.servicestack.client.Utils;
import org.example.dtos.*;
import java.util.ArrayList;

public class App {
    public static void main(String[] args) {
        // Create a ServiceStack JSON client

        var client = new JsonServiceClient("http://localhost:5166");
        client.setBearerToken("ak-87949de37e894627a9f6173154e7cafa");

        // Create request
        var request = new dtos.ChatCompletion();
        request.setModel("gemini-flash-latest")
            .setMessages(Utils.createList(
                new dtos.AiMessage()
                    .setRole("user")
                    .setContent(Utils.createList(
                        new dtos.AiTextContent()
                            .setText("What's the capital of France?")
                            .setType("text")
                    ))
                ))
            .setMaxCompletionTokens(50);

        var response = client.send(request);
        
        Inspect.printDump(response);
    }
}