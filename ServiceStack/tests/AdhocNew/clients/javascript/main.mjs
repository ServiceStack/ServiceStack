#!/usr/bin/env bun

import { JsonServiceClient, Inspect } from './servicestack-client.mjs'
import { ChatCompletion } from './dtos.mjs'

let client = new JsonServiceClient('http://localhost:5000')
client.bearerToken = 'ak-87949de37e894627a9f6173154e7cafa'

;(async () => {
    const api = await client.api(new ChatCompletion({
        model: 'openai/gpt-oss-120b',
        messages: [
            { role: 'user', content: [{ type: 'text', text: 'Capital of France?' }] }
        ]
    }))
    Inspect.printDump(api.response)
})()