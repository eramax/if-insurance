#!/bin/bash

# Stop on error
set -e

# Check if required arguments are provided
if [ "$#" -lt 2 ]; then
    echo "Usage: $0 <resource-group-name> <app-name>"
    exit 1
fi

# Variables from command-line arguments
RESOURCE_GROUP="$1"
APP_NAME="$2"
ZIP_FILE="app.zip"

echo "Cleaning up previous build artifacts..."
rm -rf ./publish
rm -f app.zip

echo "Building and publishing the application..."
dotnet publish -c Release -o ./publish \
  --no-self-contained \
  -p:PublishSingleFile=false \
  -p:DebugType=None \
  -p:DebugSymbols=false

echo "Creating deployment package..."
cd publish
zip -r "../app.zip" . -x "*.pdb"
cd ..


echo "Checking if web app exists..."
if ! az webapp show --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
    echo "Error: Web app '$APP_NAME' in resource group '$RESOURCE_GROUP' does not exist."
    echo "Please create the web app first or check the provided names."
    exit 1
fi

echo "Deploying application to existing web app..."
az webapp deploy --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" --src-path "$ZIP_FILE" --type zip

echo "Deployment complete. Your application is available at: https://$APP_NAME.azurewebsites.net"