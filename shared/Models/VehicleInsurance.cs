using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shared.Models;

public enum InsuranceStatus
{
    Active = 1,
    Expired = 2,
    Cancelled = 3,
    Suspended = 4,
    PendingPayment = 5
}

/// <summary>
/// Represents an active insurance contract between a user and a policy for a specific vehicle
/// </summary>
[Table("VehicleInsurances")]
[Index(nameof(UserId))]
[Index(nameof(PolicyId))]
[Index(nameof(VehicleId))]
[Index(nameof(Status))]
[Index(nameof(PolicyNumber), IsUnique = true)]
[Index(nameof(StartDate), nameof(EndDate))]
[Index(nameof(RenewalDate))]
public class VehicleInsurance : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid PolicyId { get; set; }

    [Required]
    public Guid VehicleId { get; set; }

    [Required]
    public InsuranceStatus Status { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
    [Required]
    public DateTime RenewalDate { get; set; }

    [MaxLength(100)]
    public string? PolicyNumber { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPremium { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Deductible { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(PolicyId))]
    public virtual Policy Policy { get; set; } = null!;

    [ForeignKey(nameof(VehicleId))]
    public virtual Vehicle Vehicle { get; set; } = null!;

    public virtual ICollection<VehicleInsuranceCoverage> VehicleInsuranceCoverages { get; set; } = new List<VehicleInsuranceCoverage>();
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
