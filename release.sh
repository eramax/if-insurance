#!/bin/bash

# Generic deployment script for all services and functions
# Usage: ./release.sh <resource-group-name> [stage]

set -e

# Check if required arguments are provided
if [ "$#" -lt 1 ]; then
    echo "Usage: $0 <resource-group-name> [stage]"
    echo "  resource-group-name: The Azure resource group name"
    echo "  stage: Environment stage (optional, defaults to 'dev')"
    exit 1
fi

# Variables from command-line arguments
RESOURCE_GROUP="$1"
STAGE="${2:-dev}"

echo "=== Insurance Management System - Release Deployment ==="
echo "Resource Group: $RESOURCE_GROUP"
echo "Stage: $STAGE"
echo ""

# Generate unique string (same logic as deployment.sh)
echo "Generating unique resource names..."
RG_ID=$(az group show --name "$RESOURCE_GROUP" --query id -o tsv)
UNIQUE_STRING=$(echo -n "$RG_ID" | sha1sum | cut -c1-8)

echo "Generated unique string: $UNIQUE_STRING"

# Generate expected resource names using the same naming convention as deployment.sh
INSURANCE_MGMT_APP="${STAGE}-insurance-${UNIQUE_STRING}"
VEHICLE_INSURANCE_APP="${STAGE}-vehicle-${UNIQUE_STRING}"
BILLING_FUNCTION="${STAGE}-billing-${UNIQUE_STRING}"
NOTIFICATION_FUNCTION="${STAGE}-notify-${UNIQUE_STRING}"

echo ""
echo "=== Target Resources ==="
echo "Insurance Management App: $INSURANCE_MGMT_APP"
echo "Vehicle Insurance App: $VEHICLE_INSURANCE_APP"
echo "Billing Function: $BILLING_FUNCTION"
echo "Notification Function: $NOTIFICATION_FUNCTION"
echo ""

# Function to deploy a service (web app)
deploy_service() {
    local service_path="$1"
    local app_name="$2"
    local service_name="$3"
    
    echo "🔨 Building $service_name..."
    
    if [ ! -d "$service_path" ]; then
        echo "❌ Service directory '$service_path' not found. Skipping..."
        return 1
    fi
    
    cd "$service_path"
    
    # Clean up silently
    rm -rf ./publish ./app.zip 2>/dev/null || true
    
    # Build with minimal output
    if ! dotnet publish -c Release -o ./publish \
      --no-self-contained \
      -p:PublishSingleFile=false \
      -p:DebugType=None \
      -p:DebugSymbols=false \
      --verbosity quiet --nologo > /dev/null 2>&1; then
        echo "❌ Build failed for $service_name"
        cd - > /dev/null
        return 1
    fi
    
    # Create package silently
    cd publish
    if ! zip -r "../app.zip" . -x "*.pdb" > /dev/null 2>&1; then
        echo "❌ Package creation failed for $service_name"
        cd - > /dev/null
        return 1
    fi
    cd ..
    
    # Validate resource exists
    if ! az webapp show --name "$app_name" --resource-group "$RESOURCE_GROUP" > /dev/null 2>&1; then
        echo "❌ Web app '$app_name' not found in resource group '$RESOURCE_GROUP'"
        cd - > /dev/null
        return 1
    fi
    
    echo "🚀 Deploying $service_name..."
    if az webapp deploy --resource-group "$RESOURCE_GROUP" --name "$app_name" --src-path "app.zip" --type zip --async > /dev/null 2>&1; then
        echo "✅ $service_name deployed successfully!"
    else
        echo "❌ Deployment failed for $service_name"
        cd - > /dev/null
        return 1
    fi
    
    cd - > /dev/null
}

# Function to deploy a function app
deploy_function() {
    local function_path="$1"
    local function_app_name="$2"
    local function_name="$3"
    
    echo "🔨 Building $function_name..."
    
    if [ ! -d "$function_path" ]; then
        echo "❌ Function directory '$function_path' not found. Skipping..."
        return 1
    fi
    
    cd "$function_path"
    
    # Clean up silently
    rm -rf ./publish ./app.zip 2>/dev/null || true
    
    # Build with minimal output
    if ! dotnet publish -c Release -o ./publish \
      --no-self-contained \
      -p:PublishSingleFile=false \
      -p:DebugType=None \
      -p:DebugSymbols=false \
      --verbosity quiet --nologo > /dev/null 2>&1; then
        echo "❌ Build failed for $function_name"
        cd - > /dev/null
        return 1
    fi
    
    # Create package silently
    cd publish
    if ! zip -r "../app.zip" . -x "*.pdb" > /dev/null 2>&1; then
        echo "❌ Package creation failed for $function_name"
        cd - > /dev/null
        return 1
    fi
    cd ..
    
    # Validate resource exists
    if ! az functionapp show --name "$function_app_name" --resource-group "$RESOURCE_GROUP" > /dev/null 2>&1; then
        echo "❌ Function app '$function_app_name' not found in resource group '$RESOURCE_GROUP'"
        cd - > /dev/null
        return 1
    fi
    
    echo "🚀 Deploying $function_name..."
    if az functionapp deployment source config-zip --resource-group "$RESOURCE_GROUP" --name "$function_app_name" --src "app.zip" > /dev/null 2>&1; then
        echo "✅ $function_name deployed successfully!"
    else
        echo "❌ Deployment failed for $function_name"
        cd - > /dev/null
        return 1
    fi
    
    cd - > /dev/null
}

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Deploy all services and functions in parallel
echo "🚀 Starting parallel deployment of all services and functions..."
echo ""

# Start all deployments in parallel
(deploy_service "$SCRIPT_DIR/services/insurance-management" "$INSURANCE_MGMT_APP" "Insurance Management Service") &
PID1=$!

(deploy_service "$SCRIPT_DIR/services/vehicle-insurance" "$VEHICLE_INSURANCE_APP" "Vehicle Insurance Service") &
PID2=$!

(deploy_function "$SCRIPT_DIR/functions/billing-service" "$BILLING_FUNCTION" "Billing Service Function") &
PID3=$!

(deploy_function "$SCRIPT_DIR/functions/notification-service" "$NOTIFICATION_FUNCTION" "Notification Service Function") &
PID4=$!

# Wait for all deployments to complete
echo "⏳ Waiting for all deployments to complete..."
echo ""

FAILED_DEPLOYMENTS=()

# Check each deployment result
if ! wait $PID1; then
    FAILED_DEPLOYMENTS+=("Insurance Management Service")
fi

if ! wait $PID2; then
    FAILED_DEPLOYMENTS+=("Vehicle Insurance Service")
fi

if ! wait $PID3; then
    FAILED_DEPLOYMENTS+=("Billing Service Function")
fi

if ! wait $PID4; then
    FAILED_DEPLOYMENTS+=("Notification Service Function")
fi

echo ""
echo "=== Deployment Summary ==="

if [ ${#FAILED_DEPLOYMENTS[@]} -eq 0 ]; then
    echo "🎉 All deployments completed successfully!"
    echo ""
    echo "📋 Service URLs:"
    echo "• Insurance Management: https://$INSURANCE_MGMT_APP.azurewebsites.net"
    echo "• Vehicle Insurance: https://$VEHICLE_INSURANCE_APP.azurewebsites.net"
    echo ""
    echo "⚡ Function URLs:"
    echo "• Billing Service: https://$BILLING_FUNCTION.azurewebsites.net"
    echo "• Notification Service: https://$NOTIFICATION_FUNCTION.azurewebsites.net"
    echo ""
    echo "✨ Release deployment complete!"
else
    echo "⚠️  Some deployments failed:"
    for failed in "${FAILED_DEPLOYMENTS[@]}"; do
        echo "  ❌ $failed"
    done
    echo ""
    echo "Please check the error messages above and retry if needed."
fi

# Clean up all temporary files and directories
echo ""
echo "🧹 Cleaning up temporary files..."

# Clean up services
for service_dir in "$SCRIPT_DIR/services"/*; do
    if [ -d "$service_dir" ]; then
        rm -rf "$service_dir/publish" "$service_dir/app.zip" 2>/dev/null || true
    fi
done

# Clean up functions
for function_dir in "$SCRIPT_DIR/functions"/*; do
    if [ -d "$function_dir" ]; then
        rm -rf "$function_dir/publish" "$function_dir/app.zip" 2>/dev/null || true
    fi
done

echo "✅ Cleanup completed!"

# Exit with appropriate code
if [ ${#FAILED_DEPLOYMENTS[@]} -eq 0 ]; then
    exit 0
else
    exit 1
fi
