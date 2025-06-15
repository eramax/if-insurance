@echo off
REM Integration Test Runner for Insurance Management System
REM This script helps run the integration tests

echo ========================================
echo Insurance Management System - Integration Tests
echo ========================================

REM Check if services are configured
echo Checking configuration...
if not exist "Tests\IntegrationTests\appsettings.json" (
    echo ❌ appsettings.json not found. Please configure your service URLs.
    echo    You can copy appsettings.azure.json to appsettings.json and update the URLs.
    exit /b 1
)

echo ✅ Configuration found

REM Build the test project
echo.
echo Building integration tests...
dotnet build Tests\IntegrationTests\IntegrationTests.csproj

if %errorlevel% neq 0 (
    echo ❌ Build failed
    exit /b 1
)

echo ✅ Build successful

REM Run the tests
echo.
echo Running integration tests...
echo Note: Make sure your services are running at the configured URLs
echo.

dotnet test Tests\IntegrationTests\IntegrationTests.csproj --verbosity normal

echo.
echo Integration test run completed.
