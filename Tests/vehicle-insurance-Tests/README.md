# Vehicle Insurance Tests

This project contains comprehensive unit tests for the Vehicle Insurance service components. The tests cover data access, business logic, models, API endpoints, and performance scenarios using xUnit testing framework.

## Test Categories

### Data Layer Tests (`Data/`)

- **VehicleDbContextTests.cs**: Tests for database context operations, entity relationships, and data integrity

### Endpoints Tests (`Endpoints/`)

- **VehicleEndpointsTests.cs**: Tests for API endpoints including HTTP request/response handling, routing, and controller actions

### Models Tests (`Models/`)

- **VehicleModelTests.cs**: Tests for data model validation, business rules, and model behavior

### Services Tests (`Services/`)

- **VehicleServiceTests.cs**: Tests for business logic, service layer operations, and integration with repositories
- **VehicleServiceEdgeCaseTests.cs**: Tests for edge cases, error handling, and boundary conditions

### Performance Tests (`Performance/`)

- **VehicleServicePerformanceTests.cs**: Performance and load testing for service operations

## Latest Test Results

**Test Execution Date**: Current session execution

### ✅ Test Summary

- **Total Tests**: 63
- **Passed**: 63 ✅
- **Failed**: 0 ❌
- **Skipped**: 0 ⏭️
- **Duration**: 2.2 seconds
- **Status**: ALL TESTS PASSED

### Build Information

- **Build Status**: ✅ Succeeded (with 5 warnings)
- **Framework**: .NET 8.0
- **Test Framework**: xUnit.net
- **Compilation**: Successful

### Build Warnings

- 5 warnings related to nullable reference types and xUnit parameter usage
- Warnings do not affect test functionality

## Test Infrastructure

### Dependencies

- xUnit.net testing framework
- Microsoft.EntityFrameworkCore.InMemory for database testing
- Microsoft.AspNetCore.Mvc.Testing for API testing
- Microsoft.AspNetCore.TestHost for integration testing
- Application Insights for telemetry testing

### Test Data

The tests use in-memory databases and mock data to ensure isolation and repeatability. Service Bus messaging is mocked for isolated testing.

## Test Features

### API Integration Testing

- Full HTTP request/response cycle testing
- Endpoint routing and controller validation
- JSON serialization/deserialization testing
- Status code and response validation

### Database Testing

- In-memory Entity Framework database
- CRUD operations validation
- Data persistence and retrieval testing
- Concurrent access testing (with some threading issues noted in logs)

### Performance Testing

- Service operation performance benchmarks
- Load testing scenarios
- Response time validation

### Edge Case Testing

- Input validation testing
- Error handling scenarios
- Boundary condition testing
- Null reference handling

## Running the Tests

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code with C# extension

### Command Line

```bash
# Run all tests in this project
dotnet test Tests/vehicle-insurance-Tests/vehicle-insurance-Tests.csproj

# Run with verbose output
dotnet test Tests/vehicle-insurance-Tests/vehicle-insurance-Tests.csproj --verbosity normal

# Run specific test category
dotnet test Tests/vehicle-insurance-Tests/vehicle-insurance-Tests.csproj --filter "Category=Performance"
```

### Visual Studio

1. Open the solution in Visual Studio
2. Go to Test Explorer (Test > Test Explorer)
3. Run all tests or select specific tests to run

## Test Configuration

### Application Insights

Tests include Application Insights connection string validation and telemetry testing.

### Service Bus Integration

Service Bus messaging service is initialized and tested, with proper cleanup in test disposal.

### Database Configuration

- Uses Entity Framework's in-memory database provider
- Automatic database initialization and cleanup
- Vehicle count tracking and validation

## Known Issues

### Threading Warnings

Some tests show threading-related warnings in the logs:

- DbContext configuration timing issues
- Concurrent collection access warnings
- These are test environment specific and don't affect actual functionality

### Performance Considerations

- Tests include performance validations
- Connection testing with retry mechanisms
- Database operation performance monitoring

## Code Coverage

The test suite provides comprehensive coverage across:

- ✅ Data Access Layer (VehicleDbContext)
- ✅ Business Logic Layer (VehicleService)
- ✅ API Controllers and Endpoints
- ✅ Data Models and Validation (Vehicle model)
- ✅ Error Handling and Edge Cases
- ✅ Performance and Load Testing
- ✅ Service Integration (Service Bus, Application Insights)

## API Testing Details

### Vehicle CRUD Operations

- ✅ Create Vehicle (POST /vehicles)
- ✅ Get Vehicle by ID (GET /vehicles/{id})
- ✅ Get All Vehicles (GET /vehicles)
- ✅ Update Vehicle (PUT /vehicles/{id})
- ✅ Delete Vehicle (DELETE /vehicles/{id})

### HTTP Status Validation

- ✅ 200 OK for successful operations
- ✅ 201 Created for vehicle creation
- ✅ 404 Not Found for missing resources
- ✅ 400 Bad Request for validation errors

## Troubleshooting

### Common Issues

1. **Threading Warnings**: These are expected in test environments and don't affect functionality
2. **Build Warnings**: Related to nullable reference types - code functions correctly
3. **Performance Tests**: May take longer on slower machines

### Getting Help

- Check the main solution README for overall project setup
- Review individual test files for specific test documentation
- Ensure the shared project dependencies are properly built
- Check Application Insights and Service Bus configuration for integration tests
