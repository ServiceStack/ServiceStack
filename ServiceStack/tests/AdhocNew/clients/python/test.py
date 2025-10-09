#!/usr/bin/env python3

from servicestack import JsonServiceClient
from dtos import *

client = JsonServiceClient('http://localhost:5166')
client.bearer_token = 'ak-87949de37e894627a9f6173154e7cafa'
request = ChatCompletion(
    model='gemini-flash-latest',
    messages=[
        AiMessage(role='user', content=[
            AiTextContent(type="text",text='Capital of France?')
        ])
    ]
)

response = client.send(request)
printdump(response)