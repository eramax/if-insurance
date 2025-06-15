using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shared.Models;

/// <summary>
/// Represents a vehicle that can be insured
/// </summary>
[Table("Vehicles")]
[Index(nameof(LicensePlate), IsUnique = true)]
[Index(nameof(Vin), IsUnique = true)]
[Index(nameof(Make), nameof(Model), nameof(Year))]
public class Vehicle : BaseEntity
{
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string LicensePlate { get; set; } = string.Empty; [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Make { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Model { get; set; } = string.Empty;

    [Required]
    [Range(1900, 2030)]
    public int Year { get; set; }
    [Required]
    [StringLength(17, MinimumLength = 17)]
    public string Vin { get; set; } = string.Empty; // Vehicle Identification Number

}
