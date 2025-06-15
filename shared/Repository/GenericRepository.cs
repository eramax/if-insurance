using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Shared.Repository;

/// <summary>
/// Generic repository implementation with Application Insights telemetry
/// Provides CRUD operations for any entity with comprehensive logging and monitoring
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The primary key type</typeparam>
public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : class
{
    private readonly DbContext _context;
    private readonly DbSet<TEntity> _dbSet;
    private readonly TelemetryClient? _telemetryClient;
    private readonly string _entityTypeName; public GenericRepository(DbContext context, TelemetryClient? telemetryClient)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _telemetryClient = telemetryClient;
        _dbSet = _context.Set<TEntity>();
        _entityTypeName = typeof(TEntity).Name;
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationName = $"Repository.GetAll{_entityTypeName}";

        try
        {
            TrackTrace(
                $"Starting {operationName}",
                SeverityLevel.Information,
                new Dictionary<string, string> { ["EntityType"] = _entityTypeName });

            var entities = await _dbSet.ToListAsync(cancellationToken);

            stopwatch.Stop();

            var dependencyTelemetry = new DependencyTelemetry(
                "Database",
                operationName,
                operationName,
                null)
            {
                Duration = stopwatch.Elapsed,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            };
            TrackDependency(dependencyTelemetry);

            TrackMetric(
                $"{_entityTypeName}.GetAll.Count",
                entities.Count,
                new Dictionary<string, string> { ["EntityType"] = _entityTypeName });

            return entities;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var dependencyTelemetry = new DependencyTelemetry(
                "Database",
                operationName,
                operationName,
                null)
            {
                Duration = stopwatch.Elapsed,
                Success = false,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            };
            TrackDependency(dependencyTelemetry);

            TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = operationName,
                ["EntityType"] = _entityTypeName
            });

            throw;
        }
    }

    public async Task<IEnumerable<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationName = $"Repository.Get{_entityTypeName}WithFilter";

        try
        {
            TrackTrace(
                $"Starting {operationName}",
                SeverityLevel.Information,
                new Dictionary<string, string>
                {
                    ["EntityType"] = _entityTypeName,
                    ["HasFilter"] = (filter != null).ToString(),
                    ["HasOrderBy"] = (orderBy != null).ToString(),
                    ["IncludeProperties"] = includeProperties
                });

            IQueryable<TEntity> query = _dbSet;

            if (filter != null)
                query = query.Where(filter);

            foreach (var includeProperty in includeProperties.Split(
                new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            if (orderBy != null)
                query = orderBy(query);

            var entities = await query.ToListAsync(cancellationToken);

            stopwatch.Stop();

            var dependencyTelemetry = new DependencyTelemetry(
                "Database",
                operationName,
                operationName,
                null)
            {
                Duration = stopwatch.Elapsed,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            };
            TrackDependency(dependencyTelemetry);

            TrackMetric(
                $"{_entityTypeName}.GetWithFilter.Count",
                entities.Count,
                new Dictionary<string, string> { ["EntityType"] = _entityTypeName });

            return entities;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var dependencyTelemetry = new DependencyTelemetry(
                "Database",
                operationName,
                operationName,
                null)
            {
                Duration = stopwatch.Elapsed,
                Success = false,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            };
            TrackDependency(dependencyTelemetry);

            TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = operationName,
                ["EntityType"] = _entityTypeName,
                ["Filter"] = filter?.ToString() ?? "null",
                ["IncludeProperties"] = includeProperties
            });

            throw;
        }
    }

    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationName = $"Repository.Get{_entityTypeName}ById";

        try
        {
            TrackTrace(
                $"Starting {operationName}",
                SeverityLevel.Information,
                new Dictionary<string, string>
                {
                    ["EntityType"] = _entityTypeName,
                    ["Id"] = id?.ToString() ?? "null"
                });

            var entity = await _dbSet.FindAsync(new object[] { id! }, cancellationToken);

            stopwatch.Stop();

            var dependencyTelemetry = new DependencyTelemetry(
                "Database",
                operationName,
                operationName,
                null)
            {
                Duration = stopwatch.Elapsed,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            };
            TrackDependency(dependencyTelemetry);

            TrackMetric(
                $"{_entityTypeName}.GetById.Found",
                entity != null ? 1 : 0,
                new Dictionary<string, string>
                {
                    ["EntityType"] = _entityTypeName,
                    ["Id"] = id?.ToString() ?? "null"
                });

            return entity;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var dependencyTelemetry = new DependencyTelemetry(
                "Database",
                operationName,
                operationName,
                null)
            {
                Duration = stopwatch.Elapsed,
                Success = false,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            };
            TrackDependency(dependencyTelemetry);

            TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = operationName,
                ["EntityType"] = _entityTypeName,
                ["Id"] = id?.ToString() ?? "null"
            });

            throw;
        }
    }

    public async Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationName = $"Repository.GetFirstOrDefault{_entityTypeName}";

        try
        {
            TrackTrace(
                $"Starting {operationName}",
                SeverityLevel.Information,
                new Dictionary<string, string>
                {
                    ["EntityType"] = _entityTypeName,
                    ["Filter"] = filter.ToString()
                });

            var entity = await _dbSet.FirstOrDefaultAsync(filter, cancellationToken);

            stopwatch.Stop();

            var dependencyTelemetry = new DependencyTelemetry(
                "Database",
                operationName,
                operationName,
                null)
            {
                Duration = stopwatch.Elapsed,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            };
            TrackDependency(dependencyTelemetry);

            return entity;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var dependencyTelemetry = new DependencyTelemetry(
                "Database",
                operationName,
                operationName,
                null)
            {
                Duration = stopwatch.Elapsed,
                Success = false,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            };
            TrackDependency(dependencyTelemetry);

            TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = operationName,
                ["EntityType"] = _entityTypeName,
                ["Filter"] = filter.ToString()
            });

            throw;
        }
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationName = $"Repository.Any{_entityTypeName}";

        try
        {
            TrackTrace(
                $"Starting {operationName}",
                SeverityLevel.Information,
                new Dictionary<string, string>
                {
                    ["EntityType"] = _entityTypeName,
                    ["Filter"] = filter.ToString()
                });

            var exists = await _dbSet.AnyAsync(filter, cancellationToken);

            stopwatch.Stop();

            var dependencyTelemetry = new DependencyTelemetry(
                "Database",
                operationName,
                operationName,
                null)
            {
                Duration = stopwatch.Elapsed,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            };
            TrackDependency(dependencyTelemetry);

            return exists;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var dependencyTelemetry = new DependencyTelemetry(
                "Database",
                operationName,
                operationName,
                null)
            {
                Duration = stopwatch.Elapsed,
                Success = false,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            };
            TrackDependency(dependencyTelemetry);

            TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = operationName,
                ["EntityType"] = _entityTypeName,
                ["Filter"] = filter.ToString()
            });

            throw;
        }
    }

    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationName = $"Repository.Count{_entityTypeName}";

        try
        {
            TrackTrace(
                $"Starting {operationName}",
                SeverityLevel.Information,
                new Dictionary<string, string>
                {
                    ["EntityType"] = _entityTypeName,
                    ["HasFilter"] = (filter != null).ToString()
                });

            var count = filter == null
                ? await _dbSet.CountAsync(cancellationToken)
                : await _dbSet.CountAsync(filter, cancellationToken);

            stopwatch.Stop();

            var dependencyTelemetry = new DependencyTelemetry(
                "Database",
                operationName,
                operationName,
                null)
            {
                Duration = stopwatch.Elapsed,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            };
            TrackDependency(dependencyTelemetry);

            TrackMetric(
                $"{_entityTypeName}.Count",
                count,
                new Dictionary<string, string> { ["EntityType"] = _entityTypeName });

            return count;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var dependencyTelemetry = new DependencyTelemetry(
                "Database",
                operationName,
                operationName,
                null)
            {
                Duration = stopwatch.Elapsed,
                Success = false,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            };
            TrackDependency(dependencyTelemetry);

            TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = operationName,
                ["EntityType"] = _entityTypeName,
                ["Filter"] = filter?.ToString() ?? "null"
            });

            throw;
        }
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationName = $"Repository.Add{_entityTypeName}";

        try
        {
            TrackTrace(
                $"Starting {operationName}",
                SeverityLevel.Information,
                new Dictionary<string, string> { ["EntityType"] = _entityTypeName });

            var addedEntity = await _dbSet.AddAsync(entity, cancellationToken);

            stopwatch.Stop();

            TrackEvent($"{_entityTypeName}.Added", new Dictionary<string, string>
            {
                ["EntityType"] = _entityTypeName,
                ["Duration"] = stopwatch.Elapsed.TotalMilliseconds.ToString()
            });

            return addedEntity.Entity;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = operationName,
                ["EntityType"] = _entityTypeName
            });

            throw;
        }
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationName = $"Repository.AddRange{_entityTypeName}";

        try
        {
            var entitiesList = entities.ToList();

            TrackTrace(
                $"Starting {operationName}",
                SeverityLevel.Information,
                new Dictionary<string, string>
                {
                    ["EntityType"] = _entityTypeName,
                    ["Count"] = entitiesList.Count.ToString()
                });

            await _dbSet.AddRangeAsync(entitiesList, cancellationToken);

            stopwatch.Stop();

            TrackEvent($"{_entityTypeName}.AddedRange", new Dictionary<string, string>
            {
                ["EntityType"] = _entityTypeName,
                ["Count"] = entitiesList.Count.ToString(),
                ["Duration"] = stopwatch.Elapsed.TotalMilliseconds.ToString()
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = operationName,
                ["EntityType"] = _entityTypeName
            });

            throw;
        }
    }

    public Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationName = $"Repository.Update{_entityTypeName}";

        try
        {
            TrackTrace(
                $"Starting {operationName}",
                SeverityLevel.Information,
                new Dictionary<string, string> { ["EntityType"] = _entityTypeName });

            // Use SQL update instead of remove/add - EF Core will generate UPDATE SQL
            _dbSet.Update(entity);

            stopwatch.Stop();

            TrackEvent($"{_entityTypeName}.Updated", new Dictionary<string, string>
            {
                ["EntityType"] = _entityTypeName,
                ["Duration"] = stopwatch.Elapsed.TotalMilliseconds.ToString()
            });

            return Task.FromResult(entity);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = operationName,
                ["EntityType"] = _entityTypeName
            });

            throw;
        }
    }

    public Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationName = $"Repository.UpdateRange{_entityTypeName}";

        try
        {
            var entitiesList = entities.ToList();

            TrackTrace(
                $"Starting {operationName}",
                SeverityLevel.Information,
                new Dictionary<string, string>
                {
                    ["EntityType"] = _entityTypeName,
                    ["Count"] = entitiesList.Count.ToString()
                });

            _dbSet.UpdateRange(entitiesList);

            stopwatch.Stop();

            TrackEvent($"{_entityTypeName}.UpdatedRange", new Dictionary<string, string>
            {
                ["EntityType"] = _entityTypeName,
                ["Count"] = entitiesList.Count.ToString(),
                ["Duration"] = stopwatch.Elapsed.TotalMilliseconds.ToString()
            });

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = operationName,
                ["EntityType"] = _entityTypeName
            });

            throw;
        }
    }

    public async Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationName = $"Repository.Delete{_entityTypeName}ById";

        try
        {
            TrackTrace(
                $"Starting {operationName}",
                SeverityLevel.Information,
                new Dictionary<string, string>
                {
                    ["EntityType"] = _entityTypeName,
                    ["Id"] = id?.ToString() ?? "null"
                });

            var entity = await _dbSet.FindAsync(new object[] { id! }, cancellationToken);
            if (entity == null)
            {
                stopwatch.Stop();
                return false;
            }

            _dbSet.Remove(entity);

            stopwatch.Stop();

            TrackEvent($"{_entityTypeName}.Deleted", new Dictionary<string, string>
            {
                ["EntityType"] = _entityTypeName,
                ["Id"] = id?.ToString() ?? "null",
                ["Duration"] = stopwatch.Elapsed.TotalMilliseconds.ToString()
            });

            return true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = operationName,
                ["EntityType"] = _entityTypeName,
                ["Id"] = id?.ToString() ?? "null"
            });

            throw;
        }
    }

    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationName = $"Repository.Delete{_entityTypeName}";

        try
        {
            TrackTrace(
                $"Starting {operationName}",
                SeverityLevel.Information,
                new Dictionary<string, string> { ["EntityType"] = _entityTypeName });

            _dbSet.Remove(entity);

            stopwatch.Stop();

            TrackEvent($"{_entityTypeName}.Deleted", new Dictionary<string, string>
            {
                ["EntityType"] = _entityTypeName,
                ["Duration"] = stopwatch.Elapsed.TotalMilliseconds.ToString()
            });

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = operationName,
                ["EntityType"] = _entityTypeName
            });

            throw;
        }
    }

    public Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationName = $"Repository.DeleteRange{_entityTypeName}";

        try
        {
            var entitiesList = entities.ToList();

            TrackTrace(
                $"Starting {operationName}",
                SeverityLevel.Information,
                new Dictionary<string, string>
                {
                    ["EntityType"] = _entityTypeName,
                    ["Count"] = entitiesList.Count.ToString()
                });

            _dbSet.RemoveRange(entitiesList);

            stopwatch.Stop();

            TrackEvent($"{_entityTypeName}.DeletedRange", new Dictionary<string, string>
            {
                ["EntityType"] = _entityTypeName,
                ["Count"] = entitiesList.Count.ToString(),
                ["Duration"] = stopwatch.Elapsed.TotalMilliseconds.ToString()
            });

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = operationName,
                ["EntityType"] = _entityTypeName
            });

            throw;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationName = "Repository.SaveChanges";

        try
        {
            TrackTrace(
                $"Starting {operationName}",
                SeverityLevel.Information,
                new Dictionary<string, string> { ["EntityType"] = _entityTypeName });

            var affectedRows = await _context.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();

            var dependencyTelemetry = new DependencyTelemetry(
                "Database",
                operationName,
                operationName,
                null)
            {
                Duration = stopwatch.Elapsed,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            };
            TrackDependency(dependencyTelemetry);

            TrackMetric(
                "Database.SaveChanges.AffectedRows",
                affectedRows,
                new Dictionary<string, string> { ["EntityType"] = _entityTypeName });

            return affectedRows;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var dependencyTelemetry = new DependencyTelemetry(
                "Database",
                operationName,
                operationName,
                null)
            {
                Duration = stopwatch.Elapsed,
                Success = false,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            };
            TrackDependency(dependencyTelemetry);

            TrackException(ex, new Dictionary<string, string>
            {
                ["Operation"] = operationName,
                ["EntityType"] = _entityTypeName
            });

            throw;
        }
    }

    // Helper methods for telemetry tracking with null checks
    private void TrackTrace(string message, SeverityLevel level, Dictionary<string, string>? properties = null)
    {
        _telemetryClient?.TrackTrace(message, level, properties);
    }

    private void TrackDependency(DependencyTelemetry dependency)
    {
        _telemetryClient?.TrackDependency(dependency);
    }

    private void TrackException(Exception exception, Dictionary<string, string>? properties = null)
    {
        _telemetryClient?.TrackException(exception, properties);
    }

    private void TrackMetric(string name, double value, Dictionary<string, string>? properties = null)
    {
        _telemetryClient?.TrackMetric(name, value, properties);
    }

    private void TrackEvent(string name, Dictionary<string, string>? properties = null)
    {
        _telemetryClient?.TrackEvent(name, properties);
    }
}
