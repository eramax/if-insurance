using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using FluentAssertions;
using Xunit;
using Shared.Models;
using InsuranceManagement.Data;

namespace insurance_management_Tests.Data;

// Test-specific DbContext that doesn't seed data
public class TestInsuranceManagementDbContext : InsuranceManagementDbContext
{
    public TestInsuranceManagementDbContext(DbContextOptions<InsuranceManagementDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Call base method to configure relationships and audit fields
        // but override to prevent seeding
        ConfigureBaseModelWithoutSeeding(modelBuilder);
    }

    private void ConfigureBaseModelWithoutSeeding(ModelBuilder modelBuilder)
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

        // Configure VehicleInsurance relationships
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
        });

        // Configure Invoice relationships
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasOne(i => i.VehicleInsurance)
                .WithMany(vi => vi.Invoices)
                .HasForeignKey(i => i.VehicleInsuranceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

public class InsuranceManagementDbContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<InsuranceManagementDbContext> _contextOptions; public InsuranceManagementDbContextTests()
    {
        // Create a unique in-memory database for each test
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<InsuranceManagementDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Create the schema without seeding data
        using var context = new TestInsuranceManagementDbContext(_contextOptions);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private TestInsuranceManagementDbContext CreateContext() => new(_contextOptions);

    [Fact]
    public async Task SaveChanges_WithDuplicateUser_ThrowsDbUpdateException()
    {
        // Arrange
        using var context = CreateContext();

        var user1 = new User
        {
            Id = Guid.NewGuid(),
            PersonalId = "UNIQUE123",
            Name = "User One",
            Email = "unique1@example.com",
            PhoneNumber = "555-0001",
            Address = "123 Main St",
            Status = UserStatus.Active,
            DateOfBirth = new DateTime(1990, 1, 1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            Id = Guid.NewGuid(),
            PersonalId = "UNIQUE124",
            Name = "User Two",
            Email = "unique1@example.com", // Duplicate email
            PhoneNumber = "555-0002",
            Address = "456 Elm St",
            Status = UserStatus.Active,
            DateOfBirth = new DateTime(1985, 5, 15),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user1);
        await context.SaveChangesAsync();

        context.Users.Add(user2);

        // Act & Assert
        await FluentActions.Invoking(async () => await context.SaveChangesAsync())
            .Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Users_CanBeQueriedAndFiltered()
    {
        // Arrange
        using var context = CreateContext();

        var activeUser = new User
        {
            Id = Guid.NewGuid(),
            PersonalId = "ACTIVE001",
            Name = "Active User",
            Email = "activeuser@example.com",
            PhoneNumber = "555-0123",
            Address = "123 Main St",
            Status = UserStatus.Active,
            DateOfBirth = new DateTime(1990, 1, 1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var inactiveUser = new User
        {
            Id = Guid.NewGuid(),
            PersonalId = "INACTIVE001",
            Name = "Inactive User",
            Email = "inactiveuser@example.com",
            PhoneNumber = "555-0124",
            Address = "456 Oak St",
            Status = UserStatus.Inactive,
            DateOfBirth = new DateTime(1985, 5, 15),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var deletedUser = new User
        {
            Id = Guid.NewGuid(),
            PersonalId = "DELETED001",
            Name = "Deleted User",
            Email = "deleteduser@example.com",
            PhoneNumber = "555-0125",
            Address = "789 Pine St",
            Status = UserStatus.Active,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            DateOfBirth = new DateTime(1988, 3, 20),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(activeUser, inactiveUser, deletedUser);
        await context.SaveChangesAsync();

        // Act
        var activeUsers = await context.Users
            .Where(u => u.Status == UserStatus.Active && !u.IsDeleted)
            .ToListAsync();

        var allUsers = await context.Users.ToListAsync();

        // Assert
        activeUsers.Should().HaveCount(1);
        activeUsers.First().Name.Should().Be("Active User");
        allUsers.Should().HaveCount(3);
    }
    [Fact]
    public async Task VehicleInsurances_WithRelatedEntities_CanBeQueriedWithIncludes()
    {
        // Arrange
        using var context = CreateContext();

        var user = new User
        {
            Id = Guid.NewGuid(),
            PersonalId = "VIU001",
            Name = "Vehicle Owner",
            Email = "vehicleowner@example.com",
            PhoneNumber = "555-0200",
            Address = "100 Vehicle St",
            Status = UserStatus.Active,
            DateOfBirth = new DateTime(1985, 6, 15),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var policy = new Policy
        {
            Id = Guid.NewGuid(),
            Name = "Vehicle Test Policy",
            Description = "Test vehicle policy",
            PolicyType = PolicyType.Vehicle,
            InsuranceCompany = "Test Insurance Co",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var coverage1 = new Coverage
        {
            Id = Guid.NewGuid(),
            Name = "Test Coverage 1",
            Description = "Test coverage 1",
            Price = 100.00m,
            Tier = CoverageTier.Basic,
            Status = CoverageStatus.Active,
            DurationInMonths = 12,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var coverage2 = new Coverage
        {
            Id = Guid.NewGuid(),
            Name = "Test Coverage 2",
            Description = "Test coverage 2",
            Price = 200.00m,
            Tier = CoverageTier.Complementary,
            Status = CoverageStatus.Active,
            DurationInMonths = 12,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }; context.Users.Add(user);
        context.Policies.Add(policy);
        context.Coverages.AddRange(coverage1, coverage2);
        await context.SaveChangesAsync();

        // Create the Vehicle entity first to satisfy foreign key constraint
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            LicensePlate = "TST-123",
            Make = "Toyota",
            Model = "Corolla",
            Year = 2020,
            Vin = "VIN12345678901234",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();

        var vehicleInsurance = new VehicleInsurance
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            VehicleId = vehicle.Id, // Use the actual vehicle ID
            PolicyId = policy.Id,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(12),
            RenewalDate = DateTime.Today.AddMonths(11),
            Status = InsuranceStatus.Active,
            PolicyNumber = "POL001VI",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.VehicleInsurances.Add(vehicleInsurance);
        await context.SaveChangesAsync();

        var vehicleInsuranceCoverage1 = new VehicleInsuranceCoverage
        {
            VehicleInsuranceId = vehicleInsurance.Id,
            CoverageId = coverage1.Id
        };

        var vehicleInsuranceCoverage2 = new VehicleInsuranceCoverage
        {
            VehicleInsuranceId = vehicleInsurance.Id,
            CoverageId = coverage2.Id
        };

        context.VehicleInsuranceCoverages.AddRange(vehicleInsuranceCoverage1, vehicleInsuranceCoverage2);
        await context.SaveChangesAsync();

        // Act
        var result = await context.VehicleInsurances
            .Include(vi => vi.User)
            .Include(vi => vi.Policy)
            .Include(vi => vi.VehicleInsuranceCoverages)
                .ThenInclude(vic => vic.Coverage)
            .FirstOrDefaultAsync(vi => vi.Id == vehicleInsurance.Id);

        // Assert
        result.Should().NotBeNull();
        result!.User.Should().NotBeNull();
        result.Policy.Should().NotBeNull();
        result.VehicleInsuranceCoverages.Should().HaveCount(2);
        result.VehicleInsuranceCoverages.All(vic => vic.Coverage != null).Should().BeTrue();
    }
    [Fact]
    public async Task Policies_CanBeCreatedWithCoverages()
    {
        // Arrange
        using var context = CreateContext();

        var policy = new Policy
        {
            Id = Guid.NewGuid(),
            Name = "Test Policy with Coverages",
            Description = "Test policy description",
            PolicyType = PolicyType.Vehicle,
            InsuranceCompany = "Test Insurance Co",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var coverage1 = new Coverage
        {
            Id = Guid.NewGuid(),
            Name = "Basic Coverage",
            Description = "Basic coverage description",
            Price = 300.00m,
            Tier = CoverageTier.Basic,
            Status = CoverageStatus.Active,
            DurationInMonths = 12,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var coverage2 = new Coverage
        {
            Id = Guid.NewGuid(),
            Name = "Premium Coverage",
            Description = "Premium coverage description",
            Price = 500.00m,
            Tier = CoverageTier.Complementary,
            Status = CoverageStatus.Active,
            DurationInMonths = 12,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Policies.Add(policy);
        context.Coverages.AddRange(coverage1, coverage2);
        await context.SaveChangesAsync();

        var policyCoverage1 = new PolicyCoverage
        {
            PolicyId = policy.Id,
            CoverageId = coverage1.Id
        };

        var policyCoverage2 = new PolicyCoverage
        {
            PolicyId = policy.Id,
            CoverageId = coverage2.Id
        };

        context.PolicyCoverages.AddRange(policyCoverage1, policyCoverage2);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var savedPolicy = await context.Policies
            .Include(p => p.PolicyCoverages)
                .ThenInclude(pc => pc.Coverage)
            .FirstOrDefaultAsync(p => p.Id == policy.Id);

        savedPolicy.Should().NotBeNull();
        savedPolicy!.PolicyCoverages.Should().HaveCount(2);
        savedPolicy.PolicyCoverages.All(pc => pc.Coverage != null).Should().BeTrue();
    }
    [Fact]
    public async Task Invoices_CanBeCreatedWithVehicleInsuranceReference()
    {
        // Arrange
        using var context = CreateContext();

        var user = new User
        {
            Id = Guid.NewGuid(),
            PersonalId = "INVOICE001",
            Name = "Invoice User",
            Email = "invoiceuser@example.com",
            PhoneNumber = "555-0300",
            Address = "300 Invoice St",
            Status = UserStatus.Active,
            DateOfBirth = new DateTime(1990, 1, 1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var policy = new Policy
        {
            Id = Guid.NewGuid(),
            Name = "Invoice Test Policy",
            Description = "Test policy for invoice",
            PolicyType = PolicyType.Vehicle,
            InsuranceCompany = "Test Insurance Co",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }; context.Users.Add(user);
        context.Policies.Add(policy);
        await context.SaveChangesAsync();

        // Create the Vehicle entity first to satisfy foreign key constraint
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            LicensePlate = "INV-456",
            Make = "Honda",
            Model = "Civic",
            Year = 2019,
            Vin = "VIN98765432109876",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();

        var vehicleInsurance = new VehicleInsurance
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            VehicleId = vehicle.Id, // Use the actual vehicle ID
            PolicyId = policy.Id,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(12),
            RenewalDate = DateTime.Today.AddMonths(11),
            Status = InsuranceStatus.Active,
            PolicyNumber = "POL001INV",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.VehicleInsurances.Add(vehicleInsurance);
        await context.SaveChangesAsync();

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            VehicleInsuranceId = vehicleInsurance.Id,
            Amount = 600.00m,
            DueDate = DateTime.Today.AddDays(30),
            Status = InvoiceStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Invoices.Add(invoice);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var savedInvoice = await context.Invoices
            .Include(i => i.VehicleInsurance)
                .ThenInclude(vi => vi.User)
            .FirstOrDefaultAsync(i => i.Id == invoice.Id);

        savedInvoice.Should().NotBeNull();
        savedInvoice!.VehicleInsurance.Should().NotBeNull();
        savedInvoice.VehicleInsurance.User.Should().NotBeNull();
        savedInvoice.Amount.Should().Be(600.00m);
    }
}
