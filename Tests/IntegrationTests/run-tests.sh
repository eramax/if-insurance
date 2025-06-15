#!/bin/bash

# Integration Test Runner for Insurance Management System
# This script helps run the integration tests

echo "========================================"
echo "Insurance Management System - Integration Tests"
echo "========================================"

# Check if services are configured
echo "Checking configuration..."
if [ ! -f "Tests/IntegrationTests/appsettings.json" ]; then
    echo "❌ appsettings.json not found. Please configure your service URLs."
    echo "   You can copy appsettings.azure.json to appsettings.json and update the URLs."
    exit 1
fi

echo "✅ Configuration found"

# Build the test project
echo ""
echo "Building integration tests..."
dotnet build Tests/IntegrationTests/IntegrationTests.csproj

if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

echo "✅ Build successful"

# Run the tests
echo ""
echo "Running integration tests..."
echo "Note: Make sure your services are running at the configured URLs"
echo ""

dotnet test Tests/IntegrationTests/IntegrationTests.csproj --verbosity normal

echo ""
echo "Integration test run completed."
