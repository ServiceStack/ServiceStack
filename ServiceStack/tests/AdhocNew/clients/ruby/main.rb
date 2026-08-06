# frozen_string_literal: true

require 'servicestack'
require_relative 'dtos'

client = ServiceStack::JsonServiceClient.new('http://localhost:5000')
client.set_bearer_token('ak-87949de37e894627a9f6173154e7cafa')

request = ChatCompletion.new(
  model: 'openai/gpt-oss-120b',
  messages: [
    AiMessage.new(
      role: 'user',
      content: [AiTextContent.new(type: 'text', text: 'Capital of France?')]
    )
  ]
)

response = client.send(request)

puts JSON.pretty_generate(response.to_hash)
