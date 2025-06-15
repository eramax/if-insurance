using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shared.Models;

public enum InvoiceStatus
{
    UnderCalculation = 1,
    Pending = 2,
    Paid = 3,
    Cancelled = 4,
    Overdue = 5,
    PartiallyPaid = 6
}

/// <summary>
/// Represents an invoice for insurance premiums
/// </summary>
[Table("Invoices")]
[Index(nameof(VehicleInsuranceId))]
[Index(nameof(Status))]
[Index(nameof(DueDate))]
[Index(nameof(IssuedDate))]
[Index(nameof(InvoiceNumber), IsUnique = true)]
public class Invoice : BaseEntity
{
    [Required]
    public Guid VehicleInsuranceId { get; set; } // Refers to VehicleInsurance.Id

    [Required]
    public InvoiceStatus Status { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; } = 0;

    [Required]
    public DateTime IssuedDate { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    [Required]
    public DateTime StartDate { get; set; } // Invoice period start

    [Required]
    public DateTime EndDate { get; set; }   // Invoice period end

    [MaxLength(100)]
    public string PaymentMethod { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? TransactionRef { get; set; }

    public DateTime? PaidAt { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? Url { get; set; } // URL to the invoice pdf

    [Required]
    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountAmount { get; set; }    // Navigation properties
    [ForeignKey(nameof(VehicleInsuranceId))]
    public virtual VehicleInsurance VehicleInsurance { get; set; } = null!;
}
