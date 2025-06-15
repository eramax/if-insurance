using Shared.Models;
using InsuranceManagement.Models;

namespace InsuranceManagement.Mappers;

public static class DtoMapper
{
    public static UserDto ToDto(this User user) =>
        new(user.Id, user.PersonalId, user.Name, user.Email,
            user.PhoneNumber, user.Address, user.Status.ToString(), user.DateOfBirth);

    public static VehicleDto ToDto(this Vehicle vehicle) =>
        new(vehicle.Id, vehicle.LicensePlate, vehicle.Make, vehicle.Model, vehicle.Year, vehicle.Vin);

    public static CoverageDto ToDto(this Coverage coverage, DateTime startDate, DateTime endDate) =>
        new(coverage.Id, coverage.Name, coverage.Tier.ToString(), coverage.Description,
            coverage.Price, coverage.DurationInMonths, coverage.Status.ToString(), startDate, endDate);

    public static InvoiceDto ToDto(this Invoice invoice) =>
        new(invoice.Id, invoice.Status.ToString(), invoice.Amount, invoice.PaidAmount,
            invoice.IssuedDate, invoice.DueDate, invoice.InvoiceNumber);

    public static PolicyDto ToDto(this Policy policy) =>
        new(policy.Id, policy.Name, policy.PolicyType.ToString(), policy.Description,
            policy.InsuranceCompany, policy.IsActive,
            [.. policy.PolicyCoverages.Select(pc => pc.Coverage.ToDto(DateTime.MinValue, DateTime.MinValue))]);

    public static VehicleInsuranceDto ToDto(this VehicleInsurance vi) =>
        new(vi.Id, vi.Status.ToString(), vi.StartDate, vi.EndDate, vi.RenewalDate,
            vi.TotalPremium, vi.Deductible, vi.Notes,
            vi.Vehicle?.ToDto(),
            vi.Policy?.ToDto(),
            vi.VehicleInsuranceCoverages.Select(vic => vic.Coverage.ToDto(vic.StartDate, vic.EndDate)).ToList(),
            [.. vi.Invoices.Select(inv => inv.ToDto())]);

    public static UserInsuranceDetailsDto ToDetailsDto(this User user, List<VehicleInsurance> insurances) =>
        new(user.ToDto(), [.. insurances.Select(vi => vi.ToDto())]);
}
