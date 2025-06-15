using System.ComponentModel.DataAnnotations;

namespace Shared.Models;

/// <summary>
/// Message for triggering invoice generation
/// </summary>
public class InvoiceGenerationMessage
{
    public Guid VehicleInsuranceId { get; set; }

    public DateTime? BillingPeriodStart { get; set; }
    public DateTime? BillingPeriodEnd { get; set; }
}