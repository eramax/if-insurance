using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models;

/// <summary>
/// Document type enum
/// </summary>
public enum DocumentType
{
    Invoice = 1,
    Policy = 2,
    Certificate = 3,
    Other = 4
}

/// <summary>
/// Insurance document entity
/// </summary>
[Table("InsuranceInvoices")]
public class InsuranceInvoices
{
    [Key]
    public Guid Id { get; set; }

    public Guid RelatedEntityId { get; set; }

    public DocumentType Type { get; set; }

    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
