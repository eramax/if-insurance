using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace InsuranceManagement.Data
{
    /// <summary>
    /// Database context for Insurance Management Service
    /// Handles users, policies, coverages, vehicle insurances, and related entities
    /// Does not include Vehicle entity as it's managed by Vehicle Insurance Service
    /// </summary>
    public class InsuranceManagementDbContext : DbContext
    {
        public InsuranceManagementDbContext(DbContextOptions<InsuranceManagementDbContext> options) : base(options)
        {
        }        // Core entities for insurance management
        public DbSet<User> Users { get; set; }
        public DbSet<Policy> Policies { get; set; }
        public DbSet<Coverage> Coverages { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }

        // Junction and relationship entities
        public DbSet<PolicyCoverage> PolicyCoverages { get; set; }
        public DbSet<VehicleInsurance> VehicleInsurances { get; set; }
        public DbSet<VehicleInsuranceCoverage> VehicleInsuranceCoverages { get; set; }

        // Billing related entities
        public DbSet<Invoice> Invoices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure audit fields with default values for all entities
            ConfigureAuditFields(modelBuilder);

            // Configure specific relationships that might need explicit configuration
            ConfigureRelationships(modelBuilder);

            // --- Seed Data ---
            // Use fixed GUIDs for referential integrity
            var user1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var user2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var user3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var coverageBasicId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var coverageComplementaryId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            var policyId = Guid.Parse("55555555-5555-5555-5555-555555555555");
            var vi1Id = Guid.Parse("66666666-6666-6666-6666-666666666666"); // Alice's insurance
            var vi2Id = Guid.Parse("77777777-7777-7777-7777-777777777777"); // Bob's insurance
            var vehicle1Id = Guid.Parse("88888888-1111-1111-1111-111111111111"); // Alice's vehicle
            var vehicle2Id = Guid.Parse("88888888-2222-2222-2222-222222222222"); // Bob's vehicle
            var vehicle3Id = Guid.Parse("88888888-3333-3333-3333-333333333333"); // Charlie's vehicle

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = user1Id,
                    PersonalId = "U1001",
                    Name = "Alice Smith",
                    Address = "123 Main St, Cityville",
                    PhoneNumber = "1234567890",
                    Email = "alice@example.com",
                    Status = UserStatus.Active,
                    DateOfBirth = new DateTime(1990, 1, 1),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = user2Id,
                    PersonalId = "U1002",
                    Name = "Bob Johnson",
                    Address = "456 Elm St, Townsville",
                    PhoneNumber = "0987654321",
                    Email = "bob@example.com",
                    Status = UserStatus.Active,
                    DateOfBirth = new DateTime(1985, 5, 15),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = user3Id,
                    PersonalId = "U1003",
                    Name = "Charlie Brown",
                    Address = "789 Oak St, Newcity",
                    PhoneNumber = "5551234567",
                    Email = "charlie@example.com",
                    Status = UserStatus.Active,
                    DateOfBirth = new DateTime(1992, 8, 20),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            modelBuilder.Entity<Coverage>().HasData(
                new Coverage
                {
                    Id = coverageBasicId,
                    Name = "Basic Vehicle Coverage",
                    Description = "Covers basic vehicle insurance requirements.",
                    Tier = CoverageTier.Basic,
                    Price = 100.00m,
                    DurationInMonths = 12,
                    Status = CoverageStatus.Active,
                    TermsSpecificToCoverage = "Basic terms.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Coverage
                {
                    Id = coverageComplementaryId,
                    Name = "Complementary Vehicle Coverage",
                    Description = "Covers additional vehicle insurance needs.",
                    Tier = CoverageTier.Complementary,
                    Price = 50.00m,
                    DurationInMonths = 12,
                    Status = CoverageStatus.Active,
                    TermsSpecificToCoverage = "Complementary terms.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            modelBuilder.Entity<Policy>().HasData(
                new Policy
                {
                    Id = policyId,
                    Name = "Standard Vehicle Policy",
                    PolicyType = PolicyType.Vehicle,
                    Description = "Standard policy for vehicle insurance.",
                    InsuranceCompany = "if.se",
                    TermsAndConditions = "Standard T&C.",
                    CancellationTerms = "Standard cancellation.",
                    RenewalTerms = "Standard renewal.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            modelBuilder.Entity<PolicyCoverage>().HasData(
                new PolicyCoverage
                {
                    Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    PolicyId = policyId,
                    CoverageId = coverageBasicId,
                    PremiumAmount = 30.00m,
                    IsRequired = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PolicyCoverage
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    PolicyId = policyId,
                    CoverageId = coverageComplementaryId,
                    PremiumAmount = 20.00m,
                    IsRequired = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            // --- Vehicles ---
            modelBuilder.Entity<Vehicle>().HasData(
                new Vehicle
                {
                    Id = vehicle1Id,
                    LicensePlate = "ABC123",
                    Make = "Toyota",
                    Model = "Corolla",
                    Year = 2020,
                    Vin = "VINALICE123456789", // 17 chars
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Vehicle
                {
                    Id = vehicle2Id,
                    LicensePlate = "XYZ789",
                    Make = "Honda",
                    Model = "Civic",
                    Year = 2019,
                    Vin = "VINBOB0987654321", // 16 chars
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Vehicle
                {
                    Id = vehicle3Id,
                    LicensePlate = "LMN456",
                    Make = "Ford",
                    Model = "Focus",
                    Year = 2021,
                    Vin = "VINCHARLIE123456",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            // --- Vehicle Insurances ---
            var insuranceStart1 = DateTime.UtcNow.AddMonths(-2).Date;
            var insuranceEnd1 = insuranceStart1.AddYears(1).AddDays(-1);
            var insuranceStart2 = DateTime.UtcNow.AddMonths(-2).Date;
            var insuranceEnd2 = insuranceStart2.AddYears(1).AddDays(-1);
            modelBuilder.Entity<VehicleInsurance>().HasData(
                new VehicleInsurance
                {
                    Id = vi1Id,
                    UserId = user1Id,
                    PolicyId = policyId,
                    VehicleId = vehicle1Id,
                    StartDate = insuranceStart1,
                    EndDate = insuranceEnd1,
                    RenewalDate = insuranceEnd1,
                    Status = InsuranceStatus.Active,
                    CreatedAt = insuranceStart1,
                    UpdatedAt = insuranceStart1
                },
                new VehicleInsurance
                {
                    Id = vi2Id,
                    UserId = user2Id,
                    PolicyId = policyId,
                    VehicleId = vehicle2Id,
                    StartDate = insuranceStart2,
                    EndDate = insuranceEnd2,
                    RenewalDate = insuranceEnd2,
                    Status = InsuranceStatus.Active,
                    CreatedAt = insuranceStart2,
                    UpdatedAt = insuranceStart2
                }
            );

            // --- Invoices for each insurance (previous two months) ---
            var invoice1aId = Guid.Parse("bbbbbbb1-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var invoice1bId = Guid.Parse("bbbbbbb2-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var invoice2aId = Guid.Parse("bbbbbbb3-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var invoice2bId = Guid.Parse("bbbbbbb4-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var now = DateTime.UtcNow.Date;
            var month1Start = now.AddMonths(-2).Date;
            var month1End = month1Start.AddMonths(1).AddDays(-1);
            var month2Start = now.AddMonths(-1).Date;
            var month2End = month2Start.AddMonths(1).AddDays(-1);
            modelBuilder.Entity<Invoice>().HasData(
                // Alice's insurance
                new Invoice
                {
                    Id = invoice1aId,
                    VehicleInsuranceId = vi1Id,
                    Status = InvoiceStatus.Paid,
                    Amount = 100.00m,
                    PaidAmount = 100.00m,
                    IssuedDate = month1Start,
                    DueDate = month1Start.AddDays(10),
                    StartDate = month1Start,
                    EndDate = month1End,
                    PaymentMethod = "Credit Card",
                    TransactionRef = "TXNALICE1A",
                    PaidAt = month1Start.AddDays(5),
                    InvoiceNumber = "INV-ALICE-001",
                    TaxAmount = 0,
                    DiscountAmount = 0,
                    CreatedAt = month1Start,
                    UpdatedAt = month1Start
                },
                new Invoice
                {
                    Id = invoice1bId,
                    VehicleInsuranceId = vi1Id,
                    Status = InvoiceStatus.Paid,
                    Amount = 100.00m,
                    PaidAmount = 100.00m,
                    IssuedDate = month2Start,
                    DueDate = month2Start.AddDays(10),
                    StartDate = month2Start,
                    EndDate = month2End,
                    PaymentMethod = "Credit Card",
                    TransactionRef = "TXNALICE1B",
                    PaidAt = month2Start.AddDays(5),
                    InvoiceNumber = "INV-ALICE-002",
                    TaxAmount = 0,
                    DiscountAmount = 0,
                    CreatedAt = month2Start,
                    UpdatedAt = month2Start
                },
                // Bob's insurance
                new Invoice
                {
                    Id = invoice2aId,
                    VehicleInsuranceId = vi2Id,
                    Status = InvoiceStatus.Paid,
                    Amount = 100.00m,
                    PaidAmount = 100.00m,
                    IssuedDate = month1Start,
                    DueDate = month1Start.AddDays(10),
                    StartDate = month1Start,
                    EndDate = month1End,
                    PaymentMethod = "Bank Transfer",
                    TransactionRef = "TXNBOB2A",
                    PaidAt = month1Start.AddDays(6),
                    InvoiceNumber = "INV-BOB-001",
                    TaxAmount = 0,
                    DiscountAmount = 0,
                    CreatedAt = month1Start,
                    UpdatedAt = month1Start
                },
                new Invoice
                {
                    Id = invoice2bId,
                    VehicleInsuranceId = vi2Id,
                    Status = InvoiceStatus.Paid,
                    Amount = 100.00m,
                    PaidAmount = 100.00m,
                    IssuedDate = month2Start,
                    DueDate = month2Start.AddDays(10),
                    StartDate = month2Start,
                    EndDate = month2End,
                    PaymentMethod = "Bank Transfer",
                    TransactionRef = "TXNBOB2B",
                    PaidAt = month2Start.AddDays(6),
                    InvoiceNumber = "INV-BOB-002",
                    TaxAmount = 0,
                    DiscountAmount = 0,
                    CreatedAt = month2Start,
                    UpdatedAt = month2Start
                }
            );

            // --- Vehicle Insurance Coverages ---
            modelBuilder.Entity<VehicleInsuranceCoverage>().HasData(
                // Alice: both coverages
                new VehicleInsuranceCoverage
                {
                    Id = Guid.Parse("aaaaaaa1-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    VehicleInsuranceId = vi1Id,
                    CoverageId = coverageBasicId,
                    StartDate = insuranceStart1,
                    EndDate = insuranceEnd1,
                    Status = VehicleInsuranceCoverageStatus.Active,
                    CreatedAt = insuranceStart1,
                    UpdatedAt = insuranceStart1
                },
                new VehicleInsuranceCoverage
                {
                    Id = Guid.Parse("aaaaaaa2-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    VehicleInsuranceId = vi1Id,
                    CoverageId = coverageComplementaryId,
                    StartDate = insuranceStart1,
                    EndDate = insuranceEnd1,
                    Status = VehicleInsuranceCoverageStatus.Active,
                    CreatedAt = insuranceStart1,
                    UpdatedAt = insuranceStart1
                },
                // Bob: only basic
                new VehicleInsuranceCoverage
                {
                    Id = Guid.Parse("aaaaaaa3-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    VehicleInsuranceId = vi2Id,
                    CoverageId = coverageBasicId,
                    StartDate = insuranceStart2,
                    EndDate = insuranceEnd2,
                    Status = VehicleInsuranceCoverageStatus.Active,
                    CreatedAt = insuranceStart2,
                    UpdatedAt = insuranceStart2
                }
            );
        }
        private void ConfigureAuditFields(ModelBuilder modelBuilder)
        {
            // Configure audit fields for all BaseEntity derived entities
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType, builder =>
                    {
                        // Use datetime('now') for SQLite compatibility instead of GETUTCDATE()
                        builder.Property(nameof(BaseEntity.CreatedAt))
                            .HasDefaultValueSql("datetime('now')");

                        builder.Property(nameof(BaseEntity.UpdatedAt))
                            .HasDefaultValueSql("datetime('now')");
                    });
                }
            }
        }
        private void ConfigureRelationships(ModelBuilder modelBuilder)
        {            // Configure VehicleInsurance relationships
            modelBuilder.Entity<VehicleInsurance>(entity =>
            {
                // VehicleInsurance -> User (Primary User)
                entity.HasOne(vi => vi.User)
                    .WithMany(u => u.VehicleInsurances)
                    .HasForeignKey(vi => vi.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // VehicleInsurance -> Policy
                entity.HasOne(vi => vi.Policy)
                    .WithMany()
                    .HasForeignKey(vi => vi.PolicyId)
                    .OnDelete(DeleteBehavior.Restrict);

                // VehicleInsurance -> Vehicle
                entity.HasOne(vi => vi.Vehicle)
                    .WithMany()
                    .HasForeignKey(vi => vi.VehicleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure VehicleInsuranceCoverage relationships
            modelBuilder.Entity<VehicleInsuranceCoverage>(entity =>
            {
                entity.HasOne(vic => vic.VehicleInsurance)
                    .WithMany(vi => vi.VehicleInsuranceCoverages)
                    .HasForeignKey(vic => vic.VehicleInsuranceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(vic => vic.Coverage)
                    .WithMany(c => c.VehicleInsuranceCoverages)
                    .HasForeignKey(vic => vic.CoverageId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure PolicyCoverage relationships
            modelBuilder.Entity<PolicyCoverage>(entity =>
            {
                entity.HasOne(pc => pc.Policy)
                    .WithMany(p => p.PolicyCoverages)
                    .HasForeignKey(pc => pc.PolicyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pc => pc.Coverage)
                    .WithMany(c => c.PolicyCoverages)
                    .HasForeignKey(pc => pc.CoverageId)
                    .OnDelete(DeleteBehavior.Restrict);
            });            // Configure Invoice relationships
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasOne(i => i.VehicleInsurance)
                    .WithMany(vi => vi.Invoices)
                    .HasForeignKey(i => i.VehicleInsuranceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
