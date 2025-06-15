# Insurance Management Tests

This project contains comprehensive unit tests for the Insurance Management service components. The tests cover data access, business logic, models, and API endpoints using xUnit testing framework.

## Test Categories

### Data Layer Tests (`Data/`)

- **InsuranceManagementDbContextTests.cs**: Tests for database context operations, entity relationships, and data integrity

### Endpoints Tests (`Endpoints/`)

- **UserInsuranceEndpointsTests.cs**: Tests for API endpoints including HTTP request/response handling, routing, and controller actions

### Models Tests (`Models/`)

- **InsuranceModelTests.cs**: Tests for data model validation, business rules, and model behavior

### Services Tests (`Services/`)

- **InsuranceServiceTests.cs**: Tests for business logic, service layer operations, and integration with repositories

## Latest Test Results

**Test Execution Date**: Latest run from conversation summary

### ✅ Test Summary

- **Total Tests**: 30
- **Passed**: 30 ✅
- **Failed**: 0 ❌
- **Skipped**: 0 ⏭️
- **Duration**: 14.3 seconds
- **Status**: ALL TESTS PASSED

### Build Information

- **Build Status**: ✅ Succeeded
- **Framework**: .NET 8.0
- **Test Framework**: xUnit.net
- **Warnings**: 0
- **Errors**: 0

## Test Infrastructure

### Dependencies

- xUnit.net testing framework
- Microsoft.EntityFrameworkCore.InMemory for database testing
- Microsoft.AspNetCore.Mvc.Testing for API testing
- Moq for mocking dependencies

### Test Data

The tests use in-memory databases and mock data to ensure isolation and repeatability.

## Running the Tests

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code with C# extension

### Command Line

```bash
# Run all tests in this project
dotnet test Tests/insurance-management-Tests/insurance-management-Tests.csproj

# Run with verbose output
dotnet test Tests/insurance-management-Tests/insurance-management-Tests.csproj --verbosity normal

# Run specific test category
dotnet test Tests/insurance-management-Tests/insurance-management-Tests.csproj --filter "Category=Unit"
```

### Visual Studio

1. Open the solution in Visual Studio
2. Go to Test Explorer (Test > Test Explorer)
3. Run all tests or select specific tests to run

## Test Configuration

### In-Memory Database

Tests use Entity Framework's in-memory database provider for fast, isolated testing without requiring a real database connection.

### Mocking Strategy

- Repository pattern with mock implementations
- Service dependencies mocked using Moq framework
- HTTP context and controllers tested with ASP.NET Core testing utilities

## Code Coverage

The test suite provides comprehensive coverage across:

- ✅ Data Access Layer
- ✅ Business Logic Layer
- ✅ API Controllers and Endpoints
- ✅ Data Models and Validation
- ✅ Error Handling and Edge Cases

## Troubleshooting

### Common Issues

1. **Build Errors**: Ensure all NuGet packages are restored (`dotnet restore`)
2. **Test Discovery**: Clean and rebuild the solution if tests don't appear
3. **Database Issues**: In-memory database is automatically configured - no setup required

### Getting Help

- Check the main solution README for overall project setup
- Review individual test files for specific test documentation
- Ensure the shared project dependencies are properly built
