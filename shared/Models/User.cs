using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shared.Models;

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3
}

/// <summary>
/// Represents a user/customer in the insurance system
/// </summary>
[Table("Users")]
[Index(nameof(Email), IsUnique = true)]
[Index(nameof(PersonalId), IsUnique = true)]
[Index(nameof(PhoneNumber))]
[Index(nameof(CreatedAt))]
public class User : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string PersonalId { get; set; } = string.Empty; // SSN, National ID, etc.

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public UserStatus Status { get; set; }

    [MaxLength(500)]
    public string? ProfilePictureUrl { get; set; }
    [Required]
    public DateTime DateOfBirth { get; set; }    // Navigation properties
    public virtual ICollection<VehicleInsurance> VehicleInsurances { get; set; } = new List<VehicleInsurance>();
}
