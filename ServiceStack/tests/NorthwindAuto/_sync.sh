#!/usr/bin/bash

FROM=/home/mythz/src/ServiceStack/llms/llms/ui

FILES=(
  "Analytics.mjs"
  "App.mjs"
  "Avatar.mjs"
  "Brand.mjs"
  "ChatPrompt.mjs"
  "fav.svg"
  "Main.mjs"
  "markdown.mjs"
  "ModelSelector.mjs"
  "OAuthSignIn.mjs"
  "ProviderIcon.mjs"
  "ProviderStatus.mjs"
  "Recents.mjs"
  "SettingsDialog.mjs"
  "Sidebar.mjs"
  "SignIn.mjs"
  "SystemPromptEditor.mjs"
  "SystemPromptSelector.mjs"
  "threadStore.mjs"
  "typography.css"
  "utils.mjs"
  "Welcome.mjs"
)

for file in ${FILES[@]}; do
    cp $FROM/$file ./wwwroot/chat/$file
done

FILES=(
  "llms.json"
  "ui.json"
)
  
FROM=/home/mythz/src/ServiceStack/llms/llms

for file in ${FILES[@]}; do
    cp $FROM/$file ./wwwroot/chat/$file
done

# Add okai provider to llms.json
OKAI_PROVIDER='{
 "enabled": false,
 "type": "OpenAiProvider",
 "base_url": "http://okai.servicestack.com",
 "api_key": "$SERVICESTACK_LICENSE",
 "models": {
   "gemini-flash-latest": "gemini-flash-latest",
   "gemini-flash-lite-latest": "gemini-flash-lite-latest",
   "kimi-k2": "kimi-k2",
   "kimi-k2-thinking": "kimi-k2-thinking",
   "minimax-m2": "minimax-m2",
   "glm-4.6": "glm-4.6",
   "gpt-oss:20b": "gpt-oss:20b",
   "gpt-oss:120b": "gpt-oss:120b",
   "llama4:400b": "llama4:400b",
   "mistral-small3.2:24b": "mistral-small3.2:24b"
 }
}'

jq --argjson okai "$OKAI_PROVIDER" '.providers.servicestack = $okai' ./wwwroot/chat/llms.json > ./wwwroot/chat/llms.json.tmp && mv ./wwwroot/chat/llms.json.tmp ./wwwroot/chat/llms.json
