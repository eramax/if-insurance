using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace VehicleInsurance.Data;

/// <summary>
/// Database context for Vehicle Insurance Service
/// Handles only Vehicle entities since this service is focused on vehicle management
/// </summary>
public class VehicleDbContext : DbContext
{
    public VehicleDbContext(DbContextOptions<VehicleDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles { get; set; }

}
