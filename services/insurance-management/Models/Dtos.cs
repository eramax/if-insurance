using Shared.Models;

namespace InsuranceManagement.Models;

public record UserDto(
    Guid Id,
    string PersonalId,
    string Name,
    string Email,
    string PhoneNumber,
    string Address,
    string Status,
    DateTime DateOfBirth);

public record VehicleDto(
    Guid Id,
    string LicensePlate,
    string Make,
    string Model,
    int Year,
    string Vin);

public record PolicyDto(
    Guid Id,
    string Name,
    string PolicyType,
    string Description,
    string InsuranceCompany,
    bool IsActive,
    List<CoverageDto> Coverages);

public record CoverageDto(
    Guid Id,
    string Name,
    string Tier,
    string Description,
    decimal Price,
    int DurationInMonths,
    string Status,
    DateTime StartDate,
    DateTime EndDate);

public record InvoiceDto(
    Guid Id,
    string Status,
    decimal Amount,
    decimal PaidAmount,
    DateTime IssuedDate,
    DateTime DueDate,
    string InvoiceNumber);

public record VehicleInsuranceDto(
    Guid Id,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    DateTime RenewalDate,
    decimal TotalPremium,
    decimal? Deductible,
    string? Notes,
    VehicleDto? Vehicle,
    PolicyDto? Policy,
    List<CoverageDto> SelectedCoverages,
    List<InvoiceDto> Invoices);

public record UserInsuranceDetailsDto(
    UserDto User,
    List<VehicleInsuranceDto> Insurances);

public record CreateInsuranceRequest(
    Guid UserId,
    Guid PolicyId,
    Guid VehicleId,
    DateTime StartDate,
    DateTime EndDate,
    DateTime RenewalDate,
    List<Guid> CoverageIds,
    decimal? Deductible = null,
    string? Notes = null);
