# Integration Tests

This project contains integration tests for the Insurance Management System services. The tests verify the end-to-end functionality of the Vehicle and Insurance services.

## Latest Test Results

**Test Execution Date**: Current session execution

### ❌ Test Summary

- **Total Tests**: 4
- **Passed**: 0 ❌
- **Failed**: 4 ❌
- **Skipped**: 0 ⏭️
- **Duration**: 9.5 seconds
- **Status**: ALL TESTS FAILED

### Build Information

- **Build Status**: ❌ Failed (4 errors, 4 warnings)
- **Framework**: .NET 8.0
- **Test Framework**: xUnit.net

### Failure Analysis

All tests failed due to connection issues with the target services:

- **Error**: `No connection could be made because the target machine actively refused it. (localhost:7001)`
- **Root Cause**: Services are not running on expected ports (Integration tests expect ports 7001/7002, but actual services run on ports 5223/52864)
- **Required Services**:
  - Vehicle Service (expected: localhost:7001, actual: localhost:52864)
  - Insurance Service (expected: localhost:7002, actual: localhost:5223)

### Failed Tests

1. `VehicleServiceIntegrationTests.GetAllVehicles_ShouldReturnVehiclesList_WhenCalled`
2. `InsuranceServiceIntegrationTests.CreateInsurance_ShouldReturnCreatedInsurance_WhenValidDataProvided`
3. `VehicleServiceIntegrationTests.CreateVehicle_ShouldReturnCreatedVehicle_WhenValidDataProvided`
4. `InsuranceServiceIntegrationTests.GetInsuranceById_ShouldReturnInsurance_WhenInsuranceExists`

## Test Coverage

### Vehicle Service Tests

- **CreateVehicle_ShouldReturnCreatedVehicle_WhenValidDataProvided**: Tests vehicle creation with valid data
- **GetAllVehicles_ShouldReturnVehiclesList_WhenCalled**: Tests retrieval of all vehicles

### Insurance Service Tests

- **CreateInsurance_ShouldReturnCreatedInsurance_WhenValidDataProvided**: Tests insurance creation for a vehicle
- **GetInsuranceById_ShouldReturnInsurance_WhenInsuranceExists**: Tests insurance retrieval by ID

## Prerequisites for Integration Tests

⚠️ **IMPORTANT**: These tests require the actual services to be running before execution.

### Service Dependencies

1. **Vehicle Service**: Must be running on `localhost:7001` (or update test config to use actual port `localhost:52864`)
2. **Insurance Service**: Must be running on `localhost:7002` (or update test config to use actual port `localhost:5223`)

### Starting Services Locally

Before running integration tests, start the required services:

```bash
# Terminal 1 - Start Vehicle Service
cd services/vehicle-insurance
dotnet run

# Terminal 2 - Start Insurance Management Service
cd services/insurance-management
dotnet run
```

### Azure Deployment

If testing against Azure-deployed services:

1. Update URLs in `appsettings.json` to point to your Azure endpoints
2. Use `appsettings.azure.json` as a template

## Configuration

The tests are configured through `appsettings.json` with the following settings:

```json
{
  "TestConfiguration": {
    "VehicleServiceUrl": "https://localhost:7001",
    "InsuranceServiceUrl": "https://localhost:7002",
    "TestUserId": "33333333-3333-3333-3333-333333333333",
    "TestPolicyId": "55555555-5555-5555-5555-555555555555",
    "TestCoverageIds": [
      "33333333-3333-3333-3333-333333333333",
      "44444444-4444-4444-4444-444444444444"
    ]
  }
}
```

**Note**: The URLs above use ports 7001/7002, but the actual services run on different ports (52864 for Vehicle, 5223 for Insurance). Update the URLs to match your actual service configuration.

**Important**: Update the URLs in `appsettings.json` to point to your actual deployed services or running local services before running the tests.

For Azure-deployed services, you can use the provided `appsettings.azure.json` as a template and rename it to `appsettings.json`.

## Running Tests

### Prerequisites

- .NET 8.0 SDK
- Access to the deployed Azure services (URLs configured in appsettings.json)

### Run All Integration Tests

```bash
# Ensure services are running first, then:
dotnet test Tests/IntegrationTests/IntegrationTests.csproj

# With verbose output
dotnet test Tests/IntegrationTests/IntegrationTests.csproj --verbosity normal
```

### Using Batch/Shell Scripts

The project includes convenience scripts:

```bash
# Windows
run-tests.bat

# Linux/macOS
./run-tests.sh
```

## Troubleshooting Integration Tests

### Connection Failures

If tests fail with "connection refused" errors:

1. **Check Service Status**: Ensure both services are running 
```bash

   # Check if services are listening on expected ports (7001/7002) or actual ports (52864/5223)

   netstat -an | findstr :7001
   netstat -an | findstr :7002
   netstat -an | findstr :52864
   netstat -an | findstr :5223

   ```

2. **Verify Service URLs**: Confirm services are accessible
   ```bash
   curl http://localhost:52864/vehicles
   curl http://localhost:5223/insurances
   ```

3. **Update Configuration**: Modify `appsettings.json` to use actual service ports instead of expected ports 7001/7002

### Common Issues

- **Services not started**: Most common cause of test failures
- **Port conflicts**: Services may start on different ports
- **Database issues**: Services may fail to start due to database connection problems
- **Firewall blocking**: Local firewall may block service connections

### Debugging Tips

1. Start services individually and verify they respond to HTTP requests
2. Check service logs for startup errors
3. Verify database connections are working
4. Use browser or Postman to test endpoints manually before running integration tests

## Test Infrastructure

### Dependencies

- xUnit.net testing framework
- Microsoft.AspNetCore.Mvc.Testing
- System.Net.Http for HTTP client testing
- Custom HttpTestHelper for API interactions

### Test Data Management

- Tests create and clean up their own test data
- Each test is isolated and doesn't depend on other tests
- Test data includes sample vehicles and insurance policies


### Run Specific Test Class

```bash
dotnet test Tests/IntegrationTests/IntegrationTests.csproj --filter "VehicleServiceIntegrationTests"
dotnet test Tests/IntegrationTests/IntegrationTests.csproj --filter "InsuranceServiceIntegrationTests"
```

### Run Specific Test Method

```bash
dotnet test Tests/IntegrationTests/IntegrationTests.csproj --filter "CreateVehicle_ShouldReturnCreatedVehicle_WhenValidDataProvided"
```

## Test Approach

The integration tests follow a pattern similar to the `test.http` file:

1. **Vehicle Tests**: Create vehicles with random data to avoid conflicts
2. **Insurance Tests**: First create a vehicle, then create insurance for that vehicle
3. **HTTP Client**: Uses a custom `HttpTestHelper` to make REST API calls
4. **Assertions**: Verify HTTP status codes and response data

## Dependencies

- **xUnit**: Testing framework
- **Microsoft.AspNetCore.Mvc.Testing**: For integration testing support
- **Newtonsoft.Json**: For JSON serialization/deserialization
- **Microsoft.Extensions.Configuration**: For reading configuration

## Notes

- Tests create real data in the services, so they should be run against a test environment
- Each test uses random data (VIN, license plates) to avoid conflicts
- Tests are designed to be independent and can run in any order
- The tests mirror the functionality tested in the `test.http` file but in an automated way
