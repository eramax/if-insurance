using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace InsuranceManagementSystem.Functions.BillingService.Data
{
    /// <summary>
    /// Database context for Billing Service Function
    /// This service handles invoice generation and management
    /// </summary>
    public class InsuranceDbContext : DbContext
    {
        public InsuranceDbContext(DbContextOptions<InsuranceDbContext> options) : base(options)
        {
        }

        // Core entities needed for billing
        public DbSet<VehicleInsurance> VehicleInsurances { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<VehicleInsuranceCoverage> VehicleInsuranceCoverages { get; set; }
        public DbSet<Coverage> Coverages { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity types for SQL Server
            modelBuilder.Entity<VehicleInsurance>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<Invoice>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<VehicleInsuranceCoverage>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<Coverage>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<User>()
                .HasKey(e => e.Id);

        }
    }
}
