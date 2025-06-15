using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Services;
using VehicleInsurance.Data;

namespace VehicleInsurance.Services;

public class VehicleService
{
    private readonly VehicleDbContext _db;
    private readonly IServiceBusMessagingService _messagingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VehicleService> _logger;
    private readonly ApplicationInsightsService _appInsights;

    public VehicleService(VehicleDbContext db, IServiceBusMessagingService messagingService,
        IConfiguration configuration, ILogger<VehicleService> logger, ApplicationInsightsService appInsights)
    {
        _db = db;
        _messagingService = messagingService;
        _configuration = configuration;
        _logger = logger;
        _appInsights = appInsights;
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id)
    {
        return await _appInsights.TrackOperationAsync(
            "VehicleService.GetById",
            async () =>
            {
                return await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id);
            },
            new Dictionary<string, string> { ["VehicleId"] = id.ToString() });
    }

    public async Task<IEnumerable<Vehicle>> GetAllAsync()
    {
        return await _appInsights.TrackOperationAsync(
            "VehicleService.GetAll",
            async () =>
            {
                return await _db.Vehicles.ToListAsync();
            });
    }

    public async Task<Vehicle> CreateAsync(Vehicle vehicle)
    {
        return await _appInsights.TrackOperationAsync(
            "VehicleService.Create",
            async () =>
            {
                vehicle.Id = Guid.NewGuid();
                vehicle.CreatedAt = DateTime.UtcNow;
                vehicle.UpdatedAt = DateTime.UtcNow;

                _db.Vehicles.Add(vehicle);
                await _db.SaveChangesAsync();

                _appInsights.TrackBusinessEvent("Vehicle.Created", new Dictionary<string, string>
                {
                    ["VehicleId"] = vehicle.Id.ToString(),
                    ["Make"] = vehicle.Make ?? string.Empty,
                    ["Model"] = vehicle.Model ?? string.Empty
                });

                return vehicle;
            },
            new Dictionary<string, string> { ["VehicleId"] = vehicle.Id.ToString() });
    }

    public async Task<Vehicle?> UpdateAsync(Guid id, Vehicle updatedVehicle)
    {
        return await _appInsights.TrackOperationAsync(
            "VehicleService.Update",
            async () =>
            {
                var existingVehicle = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id);
                if (existingVehicle == null)
                {
                    return null;
                }                // Update properties
                existingVehicle.Make = updatedVehicle.Make;
                existingVehicle.Model = updatedVehicle.Model;
                existingVehicle.Year = updatedVehicle.Year;
                existingVehicle.LicensePlate = updatedVehicle.LicensePlate;
                existingVehicle.Vin = updatedVehicle.Vin;
                existingVehicle.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                _appInsights.TrackBusinessEvent("Vehicle.Updated", new Dictionary<string, string>
                {
                    ["VehicleId"] = id.ToString(),
                    ["Make"] = existingVehicle.Make ?? string.Empty,
                    ["Model"] = existingVehicle.Model ?? string.Empty
                });

                return existingVehicle;
            },
            new Dictionary<string, string> { ["VehicleId"] = id.ToString() });
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _appInsights.TrackOperationAsync(
            "VehicleService.Delete",
            async () =>
            {
                var vehicle = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id);
                if (vehicle == null)
                {
                    return false;
                }

                _db.Vehicles.Remove(vehicle);
                await _db.SaveChangesAsync();

                _appInsights.TrackBusinessEvent("Vehicle.Deleted", new Dictionary<string, string>
                {
                    ["VehicleId"] = id.ToString(),
                    ["Make"] = vehicle.Make ?? string.Empty,
                    ["Model"] = vehicle.Model ?? string.Empty
                });

                return true;
            },
            new Dictionary<string, string> { ["VehicleId"] = id.ToString() });
    }
}
