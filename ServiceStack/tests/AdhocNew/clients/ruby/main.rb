# frozen_string_literal: true

require_relative 'dtos'
require_relative 'client'

client = JsonServiceClient.new("http://localhost:5000")
client.bearer_token = "ak-87949de37e894627a9f6173154e7cafa"

message = AiMessage.new
message.role = "user"

content_item = AiTextContent.new
content_item.type = "text"
content_item.text = "Capital of France?"

message.content = [content_item]

request = ChatCompletion.new
request.model = "openai/gpt-oss-120b"
request.messages = [message]

response = client.post(request)

puts JSON.pretty_generate(client.__send__(:object_to_hash, response))
