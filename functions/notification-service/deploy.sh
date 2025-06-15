#!/bin/bash

# Stop on error
set -e

# Check if required arguments are provided
if [ "$#" -lt 2 ]; then
    echo "Usage: $0 <resource-group-name> <function-app-name>"
    exit 1
fi

# Variables from command-line arguments
RESOURCE_GROUP="$1"
FUNCTION_APP_NAME="$2"
ZIP_FILE="app.zip"

echo "Cleaning up previous build artifacts..."
rm -rf ./publish
rm -f app.zip

echo "Building and publishing the Azure Function..."
dotnet publish -c Release -o ./publish \
  --no-self-contained \
  -p:PublishSingleFile=false \
  -p:DebugType=None \
  -p:DebugSymbols=false

echo "Creating deployment package..."
cd publish
zip -r "../app.zip" . -x "*.pdb"
cd ..

echo "Checking if function app exists..."
if ! az functionapp show --name "$FUNCTION_APP_NAME" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
    echo "Error: Function app '$FUNCTION_APP_NAME' in resource group '$RESOURCE_GROUP' does not exist."
    echo "Please create the function app first or check the provided names."
    exit 1
fi

echo "Deploying Azure Function to existing function app..."
az functionapp deployment source config-zip --resource-group "$RESOURCE_GROUP" --name "$FUNCTION_APP_NAME" --src "$ZIP_FILE"

echo "Deployment complete. Your Azure Function is available at: https://$FUNCTION_APP_NAME.azurewebsites.net"
echo "Function app name: $FUNCTION_APP_NAME"
echo "Resource group: $RESOURCE_GROUP"
