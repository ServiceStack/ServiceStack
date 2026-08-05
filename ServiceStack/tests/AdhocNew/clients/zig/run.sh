#!/bin/bash
set -e
./build.sh
zig run main.zig
