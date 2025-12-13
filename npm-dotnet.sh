#!/bin/bash
# Helper script to run npm commands with proper .NET environment

export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$HOME/.dotnet:$HOME/.dotnet/tools"

# Run the npm command passed as arguments
npm "$@"
