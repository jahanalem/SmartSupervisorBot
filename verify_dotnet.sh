#!/bin/bash

# Ensure the correct .NET SDK version is installed
if ! command -v dotnet &> /dev/null
then
    echo ".NET is not installed. Installing..."
    # Install .NET SDK here, if needed
else
    echo ".NET is installed."
fi
