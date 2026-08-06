#!/bin/bash
./build.sh

# Install the servicestack gem if it's missing
bundle check >/dev/null 2>&1 || bundle install

echo "Running Ruby main.rb..."
bundle exec ruby main.rb
