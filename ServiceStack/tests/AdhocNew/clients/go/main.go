package main

import (
	"encoding/json"
	"fmt"
	"os"

	ss "github.com/ServiceStack/servicestack-go"

	"adhocnew/clients/go/dtos"
)

func main() {
	client := ss.NewClient("http://localhost:5000")
	client.SetBearerToken("ak-87949de37e894627a9f6173154e7cafa")

	request := dtos.ChatCompletion{
		Model: "openai/gpt-oss-120b",
		Messages: []dtos.AiMessage{
			{
				Role: "user",
				Content: []interface{}{
					dtos.AiTextContent{
						AiContent: dtos.AiContent{Type: "text"},
						Text:      "Capital of France?",
					},
				},
			},
		},
	}

	response, err := ss.Send(client, request)
	if err != nil {
		fmt.Printf("Error sending request: %v\n", err)
		os.Exit(1)
	}

	prettyJson, _ := json.MarshalIndent(response, "", "  ")
	fmt.Println(string(prettyJson))
}
