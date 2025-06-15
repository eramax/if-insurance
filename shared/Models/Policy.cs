using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shared.Models;

public enum PolicyType
{
    Health = 1,
    Vehicle = 2,
    Pet = 3
}

/// <summary>
/// Represents an insurance policy template with its associated coverages
/// </summary>
[Table("Policies")]
[Index(nameof(PolicyType))]
[Index(nameof(IsActive))]
[Index(nameof(CreatedAt))]
public class Policy : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public PolicyType PolicyType { get; set; }

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string InsuranceCompany { get; set; } = string.Empty;

    [MaxLength(5000)]
    public string TermsAndConditions { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string CancellationTerms { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string RenewalTerms { get; set; } = string.Empty;

    [Required]
    public bool IsActive { get; set; } = true;    // Navigation properties
    public virtual ICollection<PolicyCoverage> PolicyCoverages { get; set; } = new List<PolicyCoverage>();
}
