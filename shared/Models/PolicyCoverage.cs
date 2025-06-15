using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shared.Models;

/// <summary>
/// Junction table for Many-to-Many relationship between Policy and Coverage
/// </summary>
[Table("PolicyCoverages")]
[Index(nameof(PolicyId))]
[Index(nameof(CoverageId))]
[Index(nameof(PolicyId), nameof(CoverageId), IsUnique = true)]
public class PolicyCoverage : BaseEntity
{
    [Required]
    public Guid PolicyId { get; set; }

    [Required]
    public Guid CoverageId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PremiumAmount { get; set; }

    [Required]
    public bool IsRequired { get; set; } = false;

    // Navigation properties
    [ForeignKey(nameof(PolicyId))]
    public virtual Policy Policy { get; set; } = null!;

    [ForeignKey(nameof(CoverageId))]
    public virtual Coverage Coverage { get; set; } = null!;
}
