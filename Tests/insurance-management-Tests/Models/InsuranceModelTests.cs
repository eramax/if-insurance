using Xunit;
using FluentAssertions;
using InsuranceManagement.Models;

namespace insurance_management_Tests.Models;

public class InsuranceModelTests
{
    [Fact]
    public void CreateInsuranceRequest_WithValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var coverageId = Guid.NewGuid();
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddMonths(12);
        var renewalDate = startDate.AddMonths(11);

        // Act
        var request = new CreateInsuranceRequest(
            UserId: userId,
            PolicyId: policyId,
            VehicleId: vehicleId,
            StartDate: startDate,
            EndDate: endDate,
            RenewalDate: renewalDate,
            CoverageIds: [coverageId],
            Deductible: 500.00m,
            Notes: "Test insurance policy"
        );

        // Assert
        request.UserId.Should().Be(userId);
        request.PolicyId.Should().Be(policyId);
        request.VehicleId.Should().Be(vehicleId);
        request.StartDate.Should().Be(startDate);
        request.EndDate.Should().Be(endDate);
        request.RenewalDate.Should().Be(renewalDate);
        request.CoverageIds.Should().Contain(coverageId);
        request.Deductible.Should().Be(500.00m);
        request.Notes.Should().Be("Test insurance policy");
    }

    [Fact]
    public void CreateInsuranceRequest_WithNullOptionalFields_ShouldCreateSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var coverageId = Guid.NewGuid();
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddMonths(12);
        var renewalDate = startDate.AddMonths(11);

        // Act
        var request = new CreateInsuranceRequest(
            UserId: userId,
            PolicyId: policyId,
            VehicleId: vehicleId,
            StartDate: startDate,
            EndDate: endDate,
            RenewalDate: renewalDate,
            CoverageIds: [coverageId]
        );

        // Assert
        request.Deductible.Should().BeNull();
        request.Notes.Should().BeNull();
        request.CoverageIds.Should().NotBeEmpty();
    }

    [Fact]
    public void VehicleInsuranceDto_WithAllProperties_ShouldMapCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var vehicleDto = new VehicleDto(
            Id: Guid.NewGuid(),
            LicensePlate: "ABC123",
            Make: "Toyota",
            Model: "Camry",
            Year: 2020,
            Vin: "1HGBH41JXMN109186"
        );
        
        var policyDto = new PolicyDto(
            Id: Guid.NewGuid(),
            Name: "Test Policy",
            PolicyType: "Comprehensive",
            Description: "Test Description",
            InsuranceCompany: "Test Insurance",
            IsActive: true,
            Coverages: []
        );

        var coverageDto = new CoverageDto(
            Id: Guid.NewGuid(),
            Name: "Test Coverage",
            Tier: "Basic",
            Description: "Test Coverage Description",
            Price: 100.00m,
            DurationInMonths: 12,
            Status: "Active",
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddMonths(12)
        );

        var invoiceDto = new InvoiceDto(
            Id: Guid.NewGuid(),
            Status: "Pending",
            Amount: 100.00m,
            PaidAmount: 0.00m,
            IssuedDate: DateTime.UtcNow,
            DueDate: DateTime.UtcNow.AddDays(30),
            InvoiceNumber: "INV-001"
        );

        // Act
        var insuranceDto = new VehicleInsuranceDto(
            Id: id,
            Status: "Active",
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddMonths(12),
            RenewalDate: DateTime.UtcNow.AddMonths(11),
            TotalPremium: 100.00m,
            Deductible: 500.00m,
            Notes: "Test notes",
            Vehicle: vehicleDto,
            Policy: policyDto,
            SelectedCoverages: [coverageDto],
            Invoices: [invoiceDto]
        );

        // Assert
        insuranceDto.Id.Should().Be(id);
        insuranceDto.Status.Should().Be("Active");
        insuranceDto.TotalPremium.Should().Be(100.00m);
        insuranceDto.Deductible.Should().Be(500.00m);
        insuranceDto.Notes.Should().Be("Test notes");
        insuranceDto.Vehicle.Should().NotBeNull();
        insuranceDto.Policy.Should().NotBeNull();
        insuranceDto.SelectedCoverages.Should().HaveCount(1);
        insuranceDto.Invoices.Should().HaveCount(1);
    }

    [Fact]
    public void UserInsuranceDetailsDto_ShouldContainUserAndInsurances()
    {
        // Arrange
        var userDto = new UserDto(
            Id: Guid.NewGuid(),
            PersonalId: "123456789",
            Name: "John Doe",
            Email: "john@example.com",
            PhoneNumber: "555-0123",
            Address: "123 Main St",
            Status: "Active",
            DateOfBirth: new DateTime(1990, 1, 1)
        );

        var insuranceDto = new VehicleInsuranceDto(
            Id: Guid.NewGuid(),
            Status: "Active",
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddMonths(12),
            RenewalDate: DateTime.UtcNow.AddMonths(11),
            TotalPremium: 100.00m,
            Deductible: null,
            Notes: null,
            Vehicle: null,
            Policy: null,
            SelectedCoverages: [],
            Invoices: []
        );

        // Act
        var userDetailsDto = new UserInsuranceDetailsDto(
            User: userDto,
            Insurances: [insuranceDto]
        );

        // Assert
        userDetailsDto.User.Should().Be(userDto);
        userDetailsDto.Insurances.Should().HaveCount(1);
        userDetailsDto.Insurances[0].Should().Be(insuranceDto);
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("Inactive")]
    [InlineData("Pending")]
    [InlineData("Cancelled")]
    public void InsuranceStatus_ShouldAcceptValidValues(string status)
    {
        // Arrange & Act
        var insuranceDto = new VehicleInsuranceDto(
            Id: Guid.NewGuid(),
            Status: status,
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddMonths(12),
            RenewalDate: DateTime.UtcNow.AddMonths(11),
            TotalPremium: 100.00m,
            Deductible: null,
            Notes: null,
            Vehicle: null,
            Policy: null,
            SelectedCoverages: [],
            Invoices: []
        );

        // Assert
        insuranceDto.Status.Should().Be(status);
    }
}
