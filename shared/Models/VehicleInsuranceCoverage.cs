using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shared.Models;

public enum VehicleInsuranceCoverageStatus
{
    Active = 1,
    Inactive = 2,
    Cancelled = 3,
    Expired = 4
}

/// <summary>
/// Represents a specific coverage applied to a vehicle insurance policy
/// Junction table between VehicleInsurance and Coverage with additional properties
/// </summary>
[Table("VehicleInsuranceCoverages")]
[Index(nameof(VehicleInsuranceId))]
[Index(nameof(CoverageId))]
[Index(nameof(Status))]
[Index(nameof(VehicleInsuranceId), nameof(CoverageId), IsUnique = true)]
public class VehicleInsuranceCoverage : BaseEntity
{
    [Required]
    public Guid VehicleInsuranceId { get; set; }

    [Required]
    public Guid CoverageId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public VehicleInsuranceCoverageStatus Status { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PremiumAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CoverageLimit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Deductible { get; set; }

    [MaxLength(500)]
    public string? SpecialTerms { get; set; }    // Navigation properties
    [ForeignKey(nameof(VehicleInsuranceId))]
    public virtual VehicleInsurance VehicleInsurance { get; set; } = null!;

    [ForeignKey(nameof(CoverageId))]
    public virtual Coverage Coverage { get; set; } = null!;
}
