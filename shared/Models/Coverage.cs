using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shared.Models;

public enum CoverageTier
{
    Basic = 1,
    Complementary = 2
}

public enum CoverageStatus
{
    Active = 1,
    Inactive = 2
}

/// <summary>
/// Represents an insurance coverage type (e.g., liability, collision, comprehensive)
/// </summary>
[Table("Coverages")]
[Index(nameof(Tier))]
[Index(nameof(Status))]
[Index(nameof(Name))]
public class Coverage : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public CoverageTier Tier { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Required]
    [Range(1, 120)]
    public int DurationInMonths { get; set; }

    [Required]
    public CoverageStatus Status { get; set; }

    [MaxLength(2000)]
    public string? TermsSpecificToCoverage { get; set; }

    // Navigation properties
    public virtual ICollection<PolicyCoverage> PolicyCoverages { get; set; } = new List<PolicyCoverage>();
    public virtual ICollection<VehicleInsuranceCoverage> VehicleInsuranceCoverages { get; set; } = new List<VehicleInsuranceCoverage>();
}


