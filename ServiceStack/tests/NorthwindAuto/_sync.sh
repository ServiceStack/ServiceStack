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
