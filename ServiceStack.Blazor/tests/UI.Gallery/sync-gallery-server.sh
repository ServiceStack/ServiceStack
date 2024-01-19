#!/bin/bash

# Define source and target base directories
SOURCE_DIR="./Gallery/MyApp"
TARGET_DIR="../../../../../NetCoreApps/BlazorGallery/MyApp"

# Function to sync directories
sync_dir() {
    local src="$1"
    local dest="$2"
    mkdir -p "$dest"          # Create target directory if it doesn't exist
    cp -a "$src/." "$dest"    # Copy all contents recursively, preserving attributes
}

# Sync specific directories
sync_dir "$SOURCE_DIR/ServiceInterface" "$TARGET_DIR/ServiceInterface"
sync_dir "$SOURCE_DIR/ServiceModel" "$TARGET_DIR/ServiceModel"
sync_dir "$SOURCE_DIR/Components" "$TARGET_DIR/Components"
sync_dir "$SOURCE_DIR/wwwroot" "$TARGET_DIR/wwwroot"
sync_dir "$SOURCE_DIR/Migrations" "$TARGET_DIR/Migrations"
sync_dir "$SOURCE_DIR/Data" "$TARGET_DIR/Data"

# Copy individual files
cp "$SOURCE_DIR/Create.cs" "$TARGET_DIR/Create.cs"

echo "Sync complete."
