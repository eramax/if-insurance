# Insurance Management System - Test Suite

This directory contains comprehensive test coverage for the Insurance Management System, including unit tests, integration tests, and performance tests.

## Test Projects Overview

### 1. 📋 Insurance Management Tests

**Location**: `insurance-management-Tests/`
**Status**: ✅ ALL TESTS PASSING

- **Tests**: 30 tests covering the insurance management service
- **Duration**: 14.3 seconds
- **Coverage**: Data layer, endpoints, models, and services
- **Framework**: xUnit.net with Entity Framework In-Memory testing

### 2. 🚗 Vehicle Insurance Tests

**Location**: `vehicle-insurance-Tests/`
**Status**: ✅ ALL TESTS PASSING

- **Tests**: 63 tests covering the vehicle insurance service
- **Duration**: 2.2 seconds
- **Coverage**: Data layer, endpoints, models, services, and performance tests
- **Framework**: xUnit.net with ASP.NET Core testing utilities

### 3. 🔗 Integration Tests

**Location**: `IntegrationTests/`
**Status**: ❌ REQUIRES SERVICE SETUP

- **Tests**: 4 end-to-end integration tests
- **Requires**: Running services on localhost:7001 and localhost:7002
- **Coverage**: Cross-service functionality and API integration
- **Framework**: xUnit.net with HTTP client testing

## Test Results Summary

| Project              | Total Tests | Passed | Failed | Skipped | Duration  | Status         |
| -------------------- | ----------- | ------ | ------ | ------- | --------- | -------------- |
| Insurance Management | 30          | 30 ✅  | 0      | 0       | 14.3s     | ✅ PASS        |
| Vehicle Insurance    | 63          | 63 ✅  | 0      | 0       | 2.2s      | ✅ PASS        |
| Integration Tests    | 4           | 0      | 4 ❌   | 0       | 9.5s      | ❌ NEEDS SETUP |
| **TOTAL**            | **97**      | **93** | **4**  | **0**   | **26.0s** | **96% PASS**   |

## Quick Start - Running All Tests

### Unit Tests Only (No Service Dependencies)

```bash
# From solution root
dotnet test Tests/insurance-management-Tests/
dotnet test Tests/vehicle-insurance-Tests/

# Or run both together
dotnet test Tests/insurance-management-Tests/ Tests/vehicle-insurance-Tests/
```

### All Tests Including Integration

```bash
# 1. Start required services first
cd services/vehicle-insurance && dotnet run &
cd services/insurance-management && dotnet run &

# 2. Run all tests
dotnet test
```

## Test Categories

### 🧪 Unit Tests

- **Insurance Management**: Business logic, data access, API endpoints
- **Vehicle Insurance**: CRUD operations, validation, performance testing
- **Shared Components**: Repository patterns, extensions, configurations

### 🔄 Integration Tests

- **Cross-Service Communication**: Vehicle and Insurance service integration
- **End-to-End Workflows**: Complete business processes
- **API Integration**: REST API testing with real HTTP calls

### ⚡ Performance Tests

- **Load Testing**: Service performance under load
- **Response Time Validation**: API response time benchmarks
- **Concurrency Testing**: Multi-threaded operation validation

## Test Infrastructure

### Frameworks Used

- **xUnit.net**: Primary testing framework
- **Entity Framework In-Memory**: Database testing
- **ASP.NET Core Testing**: Web API testing
- **Microsoft Test Host**: Integration testing
- **Moq**: Mocking framework

### Testing Patterns

- **Arrange-Act-Assert**: Standard test structure
- **Repository Pattern**: Data access testing
- **Dependency Injection**: Service layer testing
- **In-Memory Databases**: Isolated data testing

## Development Workflow

### Before Committing Code

```bash
# Run unit tests (fast)
dotnet test Tests/insurance-management-Tests/ Tests/vehicle-insurance-Tests/
```

### Before Deployment

```bash
# Start services
./start-services.sh

# Run all tests including integration
dotnet test

# Clean up
./stop-services.sh
```

### Continuous Integration

The test suite is designed for CI/CD pipelines:

- Unit tests run without external dependencies
- Integration tests can be configured with environment-specific URLs
- Performance tests validate deployment quality

## Test Configuration

### AppSettings Management

- **Unit Tests**: Use in-memory databases and mocked services
- **Integration Tests**: Configurable service URLs via appsettings.json
- **CI/CD**: Environment-specific configuration overrides

### Database Testing

- **In-Memory Provider**: Fast, isolated unit testing
- **Test Data Builders**: Consistent test data creation
- **Clean State**: Each test starts with fresh data

## Troubleshooting

### Common Issues

1. **Integration Test Failures**

   - ✅ **Solution**: Ensure services are running on expected ports
   - Check: `netstat -an | findstr :7001` and `:7002`

2. **Build Errors**

   - ✅ **Solution**: Restore NuGet packages: `dotnet restore`
   - Clean and rebuild: `dotnet clean && dotnet build`

3. **Test Discovery Issues**
   - ✅ **Solution**: Rebuild test projects and refresh test explorer

### Performance Considerations

- Unit tests should complete in under 30 seconds
- Integration tests may take longer due to service startup
- Performance tests validate response times under load

## Contributing

### Adding New Tests

1. Follow existing naming conventions (`*Tests.cs`)
2. Use appropriate test categories (Unit, Integration, Performance)
3. Include both positive and negative test cases
4. Add documentation for complex test scenarios

### Test Requirements

- All public methods should have corresponding tests
- Error handling paths must be tested
- Performance-critical code needs performance tests
- Integration scenarios require end-to-end validation

## Resources

- [xUnit Documentation](https://xunit.net/)
- [ASP.NET Core Testing](https://docs.microsoft.com/en-us/aspnet/core/test/)
- [Entity Framework Testing](https://docs.microsoft.com/en-us/ef/core/testing/)
- [Project Testing Guidelines](../README.md)
