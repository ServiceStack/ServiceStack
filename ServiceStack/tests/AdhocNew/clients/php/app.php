<?php
require_once __DIR__ . '/vendor/autoload.php'; // Autoload files using Composer autoload

use ServiceStack\JsonServiceClient;
use ServiceStack\Inspect;
use dtos\ChatCompletion;
use dtos\AiMessage;
use dtos\AiTextContent;

$client = new JsonServiceClient("https://localhost:5001");
$client->bearerToken = "ak-87949de37e894627a9f6173154e7cafa";

/** @var {OpenAiChatCompletionResponse} $response */
$response = $client->send(new ChatCompletion(
    model: "gemini-flash-latest",
    messages: [
        new AiMessage(
            role: "user",
            content: [
                new AiTextContent(
                    type: "text",
                    text: "Capital of France?"
                )
            ]
        )
    ],
    max_completion_tokens: 50
));

Inspect::printDump($response);