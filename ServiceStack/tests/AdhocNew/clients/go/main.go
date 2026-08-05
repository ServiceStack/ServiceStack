package main

import (
	"encoding/json"
	"fmt"
	"os"
)

func main() {
	client := NewJsonServiceClient("http://localhost:5000")
	client.BearerToken = "ak-87949de37e894627a9f6173154e7cafa"

	request := ChatCompletion{
		Model: "openai/gpt-oss-120b",
		Messages: []AiMessage{
			{
				Role: "user",
				Content: []interface{}{
					AiTextContent{
						AiContent: AiContent{Type: "text"},
						Text:      "Capital of France?",
					},
				},
			},
		},
	}

	response, err := Send[ChatResponse](client, request)
	if err != nil {
		fmt.Printf("Error sending request: %v\n", err)
		os.Exit(1)
	}

	prettyJson, _ := json.MarshalIndent(response, "", "  ")
	fmt.Println(string(prettyJson))
}
