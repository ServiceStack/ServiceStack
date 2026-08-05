#!/bin/bash
if [ -d "$HOME/.local/share/mise/installs/java/21.0.2" ]; then
    export JAVA_HOME="$HOME/.local/share/mise/installs/java/21.0.2"
fi
./gradlew run
