# NTC.QoS Architecture Patterns Reference

> **Purpose:** This document serves as the authoritative reference for maintaining consistency across the NTC.QoS codebase. All new features and modifications should follow these established patterns.

**Last Updated:** January 12, 2026  
**Repository:** NextStep-Software-Solutions-Inc/NTC.QoS  
**Branch:** feature/dynamic-rubrics

---

## Table of Contents

1. [Project Structure](#1-project-structure)
2. [Entity Patterns](#2-entity-patterns)
3. [Repository Patterns](#3-repository-patterns)
4. [Migration Patterns](#4-migration-patterns)
5. [Dependency Injection](#5-dependency-injection)

---

## 1. Project Structure

### Overview

The NTC.QoS solution follows **Clean Architecture** principles with clear separation of concerns across five main projects:

```
NTC.QoS.sln
├── NTC.QoS.API          # Presentation Layer
├── NTC.QoS.Application  # Application/Business Logic Layer
├── NTC.QoS.Domain       # Domain/Core Layer
├── NTC.QoS.Infrastructure # Data Access & External Services
└── NTC.QoS.Tests        # Unit & Integration Tests
```

### Layer Responsibilities

#### 1.1 **NTC.QoS.API** (Presentation Layer)

**Purpose:** HTTP endpoints, middleware, filters, and API configuration

**Key Components:**
- Controllers (RESTful API endpoints)
- Middleware (exception handling, security headers)
- Filters (authorization, operation formatting)
- Authentication & Authorization configuration
- Swagger/OpenAPI documentation

**File Path:** [NTC.QoS.API/Program.cs](../NTC.QoS.API/Program.cs)

**Example Structure:**
```
NTC.QoS.API/
├── Controllers/
│   ├── ProviderController.cs
│   ├── CellularController.cs
│   └── ConnectivityController.cs
├── Middleware/
│   ├── ExceptionHandlerMiddleware.cs
│   └── SecurityHeadersMiddleware.cs
├── Filters/
│   ├── ClerkWebhookOperationFilter.cs
│   └── FileUploadOperationFilter.cs
├── Extensions/
├── Models/
└── Program.cs
```

#### 1.2 **NTC.QoS.Application** (Application Layer)

**Purpose:** Business logic, CQRS commands/queries, validation, and application services

**Key Components:**
- MediatR Commands & Queries (CQRS pattern)
- FluentValidation validators
- Application services (caching, reporting)
- MediatR pipeline behaviors

**File Path:** [NTC.QoS.Application/ApplicationDependencyInjection.cs](../NTC.QoS.Application/ApplicationDependencyInjection.cs)

**Example Structure:**
```
NTC.QoS.Application/
├── Features/
│   ├── Providers/
│   │   ├── Commands/
│   │   │   ├── CreateProvider/
│   │   │   │   ├── CreateProviderCommand.cs
│   │   │   │   ├── CreateProviderHandler.cs
│   │   │   │   └── CreateProviderValidator.cs
│   │   └── Queries/
│   │       ├── GetProvider/
│   │       │   ├── GetProviderQuery.cs
│   │       │   └── GetProviderHandler.cs
├── Services/
│   ├── ConnectivityCacheService.cs
│   └── ConnectivityReportingService.cs
├── Behaviors/
│   └── ValidationBehavior.cs
└── ApplicationDependencyInjection.cs
```

**Example Code:** [NTC.QoS.Application/ApplicationDependencyInjection.cs](../NTC.QoS.Application/ApplicationDependencyInjection.cs#L16-L56)

```csharp
public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
{
    var applicationAssembly = typeof(ApplicationDependencyInjection).Assembly;

    // MediatR for CQRS pattern
    services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(applicationAssembly);
        cfg.Lifetime = ServiceLifetime.Scoped;
    });
    
    // FluentValidation for request validation
    services.AddFluentValidationAutoValidation();
    services.Scan(scan => scan
        .FromAssemblies(Assembly.GetExecutingAssembly())
        .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
        .AsImplementedInterfaces()
        .WithScopedLifetime());

    // Validation pipeline behavior
    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    // Application services
    services.AddScoped<IConnectivityCacheService, ConnectivityCacheService>();
    services.AddScoped<IConnectivityReportingService, ConnectivityReportingService>();
    services.AddScoped<BroadbandAnalyticsService>();
    
    return services;
}
```

#### 1.3 **NTC.QoS.Domain** (Domain/Core Layer)

**Purpose:** Core business entities, interfaces, enums, and domain logic (no external dependencies)

**Key Components:**
- Domain entities (business objects)
- Value objects & aggregates
- Domain interfaces (repository contracts)
- Enums (domain-specific types)
- Domain exceptions

**File Path:** [NTC.QoS.Domain/Entities](../NTC.QoS.Domain/Entities), [NTC.QoS.Domain/Interfaces](../NTC.QoS.Domain/Interfaces)

**Example Structure:**
```
NTC.QoS.Domain/
├── Entities/
│   ├── BaseEntity.cs
│   ├── Provider.cs
│   ├── Barangay.cs
│   └── ...
├── Aggregates/
│   ├── ServiceProvider.cs
│   └── ...
├── Enums/
│   ├── ConnectivityStatusEnum.cs
│   ├── LocationTypeEnum.cs
│   └── ...
├── Interfaces/
│   ├── IRepository.cs
│   ├── IBarangayRepository.cs
│   └── ...
├── Exceptions/
└── Models/
```

#### 1.4 **NTC.QoS.Infrastructure** (Infrastructure Layer)

**Purpose:** Data access, external services, and infrastructure concerns

**Key Components:**
- Entity Framework Core DbContext
- Repository implementations
- Database migrations
- Background jobs (Hangfire)
- External service integrations (Azure Blob, GenAI)
- Seeding data

**File Path:** [NTC.QoS.Infrastructure/InfrastructureDependencyInjection.cs](../NTC.QoS.Infrastructure/InfrastructureDependencyInjection.cs)

**Example Structure:**
```
NTC.QoS.Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs
│   ├── Repositories/
│   │   ├── Repository.cs
│   │   ├── BarangayRepository.cs
│   │   └── ...
│   └── Configuration/
├── Migrations/
├── BackgroundTasks/
│   ├── ProcessCellJob.cs
│   └── ...
├── Services/
│   ├── BlobStorageService.cs
│   └── GenAIService.cs
├── Seed/
│   ├── LocationSeederV2.cs
│   └── ...
└── InfrastructureDependencyInjection.cs
```

#### 1.5 **NTC.QoS.Tests** (Test Layer)

**Purpose:** Unit tests, integration tests, and test utilities

**Example Structure:**
```
NTC.QoS.Tests/
├── Unit/
├── Integration/
└── Helpers/
```

---

## 2. Entity Patterns

### 2.1 Base Entity Pattern

All domain entities inherit from `BaseEntity`, which provides common audit fields and soft delete functionality.

**File Path:** [NTC.QoS.Domain/Entities/BaseEntity.cs](../NTC.QoS.Domain/Entities/BaseEntity.cs)

```csharp
using NTC.QoS.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace NTC.QoS.Domain.Entities;

public abstract class BaseEntity : IEntity
{
    [Key]
    public Guid Id { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    
    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    public BaseEntity()
    {
        Id = Guid.NewGuid();
    }
}
```

**Key Features:**
- ✅ Guid-based primary key (auto-generated)
- ✅ Audit tracking (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
- ✅ Soft delete support (IsDeleted, DeletedAt, DeletedBy)
- ✅ Implements `IEntity` marker interface

### 2.2 Entity Examples

#### Example 1: Simple Entity

**File Path:** [NTC.QoS.Domain/Entities/Provider.cs](../NTC.QoS.Domain/Entities/Provider.cs)

```csharp
using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Entities;

public class Provider : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LicenseNumber { get; set; }
    public string Color { get; set; } = string.Empty;
    
    // Logo storage
    public byte[]? LogoData { get; set; }
    public string? LogoContentType { get; set; }
    public string? LogoFileName { get; set; }
    public DateTime? LogoUploadedAt { get; set; }
    
    // Navigation properties
    public virtual List<ServiceProvider> ServiceProviders { get; set; } = [];
}
```

**Pattern Notes:**
- Inherits from `BaseEntity` for audit/soft delete
- Required strings initialized to `string.Empty` (avoids null warnings)
- Nullable strings use `?` for optional fields
- Navigation properties initialized to empty collections (`= []`)

#### Example 2: Geospatial Entity

**File Path:** [NTC.QoS.Domain/Entities/Barangay.cs](../NTC.QoS.Domain/Entities/Barangay.cs)

```csharp
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Domain.Entities;

public class Barangay : BaseEntity, IGeoBoundaryEntity
{
    public Guid MunicipalityId { get; set; }
    public virtual Municipality Municipality { get; set; } = null!;
    
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // NetTopologySuite geometry types for PostGIS
    public MultiPolygon? Boundaries { get; set; }
    public Point Center { get; set; } = null!;
    
    public string Code { get; set; } = string.Empty;
    public float Area { get; set; } = 0f;

    // Census data fields
    public int? TotalPopulation { get; set; }
    public int? HouseholdPopulation { get; set; }
    public int? HouseholdCount { get; set; }

    // Calculated properties
    public double PopulationDensity => 
        TotalPopulation.HasValue && Area > 0 
            ? TotalPopulation.Value / Area 
            : 0;

    // Navigation properties
    public virtual List<Speedtest> Speedtests { get; set; } = [];
    public virtual List<Zone> Zones { get; set; } = [];
    public virtual List<WirelessValidation> WirelessValidations { get; set; } = [];
}
```

**Pattern Notes:**
- Uses NetTopologySuite (`MultiPolygon`, `Point`) for PostGIS integration
- Implements `IGeoBoundaryEntity` interface for geospatial queries
- Includes calculated properties (PopulationDensity)
- Foreign key + navigation property pattern (`MunicipalityId` + `Municipality`)

### 2.3 Enum Patterns

Enums are stored in [NTC.QoS.Domain/Enums](../NTC.QoS.Domain/Enums) and use XML documentation.

#### Example 1: Connectivity Status Enum

**File Path:** [NTC.QoS.Domain/Enums/ConnectivityStatusEnum.cs](../NTC.QoS.Domain/Enums/ConnectivityStatusEnum.cs)

```csharp
namespace NTC.QoS.Domain.Enums;

/// <summary>
/// CMTS Broadband Mapping Rubric connectivity classification
/// </summary>
public enum ConnectivityStatusEnum
{
    /// <summary>
    /// Total score = 11-15 points
    /// Fully developed, meets most technical and social benchmarks
    /// </summary>
    Served = 3,

    /// <summary>
    /// Total score = 6-10 points  
    /// Has basic infrastructure or partial access, but lacks quality, affordability, or consistency
    /// </summary>
    Underserved = 2,

    /// <summary>
    /// Total score ≤ 5 points
    /// Virtually no broadband access or supporting infrastructure
    /// </summary>
    Unserved = 1
}
```

#### Example 2: Location Type Enum

**File Path:** [NTC.QoS.Domain/Enums/LocationTypeEnum.cs](../NTC.QoS.Domain/Enums/LocationTypeEnum.cs)

```csharp
namespace NTC.QoS.Domain.Enums;

/// <summary>
/// Administrative location levels for connectivity analysis
/// </summary>
public enum LocationTypeEnum
{
    Barangay = 1,
    Municipality = 2,
    Provincial = 3,
    Regional = 4
}
```

**Pattern Notes:**
- ✅ Use XML documentation (`/// <summary>`) for enum and values
- ✅ Explicit integer values for database storage
- ✅ Descriptive value names (PascalCase)
- ✅ Enum suffix (`*Enum`) for clarity

---

## 3. Repository Patterns

### 3.1 Generic Repository Interface

**File Path:** [NTC.QoS.Domain/Interfaces/IRepository.cs](../NTC.QoS.Domain/Interfaces/IRepository.cs)

```csharp
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Models.DTOs.Common;
using System.Linq.Expressions;

namespace NTC.QoS.Domain.Interfaces;

public interface IRepository<TEntity> where TEntity : BaseEntity
{
    // Query building
    IQueryable<TEntity> Query(Expression<Func<TEntity, bool>>? expression = null);
    IQueryable<TEntity> QueryAsNoTracking(Expression<Func<TEntity, bool>>? expression = null);
    IQueryable<TEntity> QueryWithIgnoreQueryFilters(Expression<Func<TEntity, bool>>? expression = null);
    
    // Async operations with cancellation tokens
    Task<TResult?> FirstOrDefaultAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default);
    Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default);
    Task<int> CountAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<TResult>> PaginateAsync<TResult>(IQueryable<TResult> queryable, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    
    // CRUD operations
    void Add(TEntity entity);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void AddRange(IEnumerable<TEntity> entities);
    void Update(TEntity entity);
    void Delete(TEntity entity);
    void RemoveRange(IEnumerable<TEntity> entities);
    
    // Bulk operations (uses EFCore.BulkExtensions)
    Task BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task BulkDeleteAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    
    // Soft delete operations
    Task<int> SoftDeleteAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default);
    Task<int> SoftDeleteAsync(IQueryable<TEntity> queryable, string? userId, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<int> RestoreAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default);
    
    // Hard delete operations
    Task<int> ExecuteHardDeleteAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default);
    
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    
    // Geospatial queries
    IQueryable<T> GetEntitiesWithinBoundaries<T>(
       IQueryable<T> query,
       ViewportBounds? viewport = null,
       List<Guid>? regionIds = null,
       List<Guid>? provinceIds = null,
       List<Guid>? municipalityIds = null,
       List<Guid>? barangayIds = null) where T : IGeoLocatedEntity;
}
```

**Key Patterns:**
- ✅ `IQueryable<TEntity>` for composable queries
- ✅ Async methods with `CancellationToken` support
- ✅ Generic `TResult` for projection support
- ✅ Soft delete vs. hard delete separation
- ✅ Bulk operations for performance
- ✅ Geospatial query support

### 3.2 Generic Repository Implementation

**File Path:** [NTC.QoS.Infrastructure/Persistence/Repositories/Repository.cs](../NTC.QoS.Infrastructure/Persistence/Repositories/Repository.cs)

```csharp
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    private IUserContextService? _userContextService;
    private bool _userContextResolutionAttempted = false;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Lazy-load user context service when needed
    private IUserContextService? GetUserContextService()
    {
        if (!_userContextResolutionAttempted)
        {
            _userContextResolutionAttempted = true;
            try
            {
                var serviceProvider = _context.GetService<IServiceProvider>();
                _userContextService = serviceProvider?.GetService<IUserContextService>();
            }
            catch
            {
                _userContextService = null;
            }
        }
        return _userContextService;
    }

    public virtual IQueryable<TEntity> Query(Expression<Func<TEntity, bool>>? expression = null)
    {
        var query = _context.Set<TEntity>().AsQueryable();
        if (expression != null) query = query.Where(expression);
        return query;
    }

    public async Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default)
        => await queryable.ToListAsync(cancellationToken);

    public async Task<int> CountAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryable);
        return await queryable.CountAsync(cancellationToken);
    }

    public void Add(TEntity entity) => _context.Add(entity);
    
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) 
        => await _context.AddAsync(entity, cancellationToken);
    
    public void Update(TEntity entity) => _context.Update(entity);
    
    public void Delete(TEntity entity) => _context.Remove(entity);
    
    public void AddRange(IEnumerable<TEntity> entities) => _context.AddRange(entities);
    
    public async Task BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) 
        => await _context.BulkInsertAsync(entities, cancellationToken: cancellationToken);
    
    public void RemoveRange(IEnumerable<TEntity> entities) => _context.RemoveRange(entities);
    
    public async Task BulkDeleteAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) 
        => await _context.BulkDeleteAsync(entities, cancellationToken: cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try 
        { 
            await _context.SaveChangesAsync(cancellationToken); 
        }
        catch (DbUpdateException ex) 
        { 
            throw DatabaseExceptionHandler.HandleForeignKeyConstraintViolation(ex); 
        }
    }

    public async Task<TResult?> FirstOrDefaultAsync<TResult>(IQueryable<TResult> queryable, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryable);
        return await queryable.FirstOrDefaultAsync(cancellationToken);
    }

    // ... (additional methods)
}
```

**Pattern Notes:**
- ✅ Protected `_context` for derived repositories
- ✅ Lazy-loaded `IUserContextService` for audit tracking
- ✅ Virtual methods allow repository overrides
- ✅ Exception handling wrapper for database errors
- ✅ Uses EFCore.BulkExtensions for bulk operations

### 3.3 Specific Repository Interface

**File Path:** [NTC.QoS.Domain/Interfaces/IBarangayRepository.cs](../NTC.QoS.Domain/Interfaces/IBarangayRepository.cs)

```csharp
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Domain.Interfaces;

public interface IBarangayRepository : IRepository<Barangay>
{
    /// <summary>
    /// Applies case-insensitive search filter on barangay name
    /// </summary>
    IQueryable<Barangay> SearchByName(IQueryable<Barangay> query, string name);

    /// <summary>
    /// Applies case-insensitive search filter on municipality name
    /// </summary>
    IQueryable<Barangay> SearchByMunicipalityName(IQueryable<Barangay> query, string municipalityName);

    /// <summary>
    /// Gets top barangays by cell count with optimized spatial queries.
    /// </summary>
    Task<List<GeoEntityCellCountDto>> GetTopBarangaysByCellCountAsync(
        Guid? municipalityId,
        int limit,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}
```

**Pattern Notes:**
- ✅ Extends `IRepository<TEntity>` base interface
- ✅ Domain-specific query methods return `IQueryable<T>` for composability
- ✅ Complex queries return DTOs (not entities)
- ✅ Async methods with `CancellationToken`

### 3.4 Specific Repository Implementation

**File Path:** [NTC.QoS.Infrastructure/Persistence/Repositories/BarangayRepository.cs](../NTC.QoS.Infrastructure/Persistence/Repositories/BarangayRepository.cs)

```csharp
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class BarangayRepository(ApplicationDbContext context) 
    : Repository<Barangay>(context), IBarangayRepository
{
    public IQueryable<Barangay> SearchByName(IQueryable<Barangay> query, string name)
    {
        return ApplySearch(query, name, b => b.Name);
    }

    public IQueryable<Barangay> SearchByMunicipalityName(IQueryable<Barangay> query, string municipalityName)
    {
        return ApplySearch(query, municipalityName, b => b.Municipality.Name);
    }

    public async Task<List<GeoEntityCellCountDto>> GetTopBarangaysByCellCountAsync(
        Guid? municipalityId,
        int limit,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        // Build reusable cell query
        var cellsQuery = _context.Cells
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Location != null);

        if (startDate.HasValue)
            cellsQuery = cellsQuery.Where(c => c.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            cellsQuery = cellsQuery.Where(c => c.CreatedAt <= endDate.Value);

        // Build barangay query with optional parent filtering
        var barangayQuery = _context.Set<Barangay>()
            .AsNoTracking()
            .Where(b => b.Boundaries != null);

        if (municipalityId.HasValue)
        {
            barangayQuery = barangayQuery.Where(b => b.MunicipalityId == municipalityId.Value);
        }

        // CRITICAL: Materialize to calculate CellCount only ONCE
        var barangayCellCounts = await barangayQuery
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Code,
                CellCount = cellsQuery.Count(c => b.Boundaries!.Intersects(c.Location))
            })
            .ToListAsync(cancellationToken);

        // Filter, sort, and project in-memory
        var results = barangayCellCounts
            .Where(b => b.CellCount > 0)
            .OrderByDescending(b => b.CellCount)
            .Take(limit > 0 ? limit : int.MaxValue)
            .Select(b => new GeoEntityCellCountDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code ?? string.Empty,
                EntityType = nameof(Barangay),
                CellCount = b.CellCount
            })
            .ToList();

        return results;
    }
}
```

**Pattern Notes:**
- ✅ Primary constructor pattern: `BarangayRepository(ApplicationDbContext context)`
- ✅ `internal sealed` for infrastructure encapsulation
- ✅ Inherits from `Repository<Barangay>` base class
- ✅ Access to `_context` from base class
- ✅ Query composition pattern (methods return `IQueryable`)
- ✅ Spatial queries with NetTopologySuite (`Intersects`)

### 3.5 Caching Pattern

**File Path:** [NTC.QoS.Application/Services/ConnectivityCacheService.cs](../NTC.QoS.Application/Services/ConnectivityCacheService.cs)

```csharp
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace NTC.QoS.Application.Services;

public interface IConnectivityCacheService
{
    Task<T?> GetCachedResultAsync<T>(string cacheKey) where T : class;
    Task SetCachedResultAsync<T>(string cacheKey, T data, TimeSpan? expiry = null) where T : class;
    Task InvalidateCacheAsync(string pattern);
    string GenerateInfrastructureCacheKey(Guid locationId, LocationTypeEnum locationType);
}

public class ConnectivityCacheOptions
{
    public int InfrastructureCacheHours { get; set; } = 24;
    public int SpeedStatsCacheHours { get; set; } = 4;
    public int TechnologyCacheHours { get; set; } = 12;
    public int BandCacheHours { get; set; } = 12;
    public int AssessmentCacheHours { get; set; } = 8;
    public int MaxCacheSizeMB { get; set; } = 200;
}

public class ConnectivityCacheService : IConnectivityCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConnectivityCacheOptions _options;
    private readonly ILogger<ConnectivityCacheService> _logger;
    private static readonly ConcurrentDictionary<string, ConcurrentBag<string>> _cacheKeyPatterns = new();

    public ConnectivityCacheService(
        IMemoryCache memoryCache,
        IOptions<ConnectivityCacheOptions> options,
        ILogger<ConnectivityCacheService> logger)
    {
        _memoryCache = memoryCache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<T?> GetCachedResultAsync<T>(string cacheKey) where T : class
    {
        return await Task.FromResult(_memoryCache.Get<T>(cacheKey));
    }

    public async Task SetCachedResultAsync<T>(string cacheKey, T data, TimeSpan? expiry = null) where T : class
    {
        var cacheExpiry = expiry ?? GetCacheExpiryForType<T>();

        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = cacheExpiry,
            Priority = GetCachePriorityForType<T>(),
            Size = EstimateObjectSize<T>()
        };

        _memoryCache.Set(cacheKey, data, options);
        TrackCacheKey(cacheKey);

        await Task.CompletedTask;
    }

    public async Task InvalidateCacheAsync(string pattern)
    {
        if (_cacheKeyPatterns.TryGetValue(pattern, out var keyBag))
        {
            var keys = keyBag.ToArray();
            foreach (var key in keys)
            {
                _memoryCache.Remove(key);
            }

            _cacheKeyPatterns.TryUpdate(pattern, [], keyBag);
            _logger.LogInformation("Invalidated {Count} cache entries for pattern {Pattern}", keys.Length, pattern);
        }
        await Task.CompletedTask;
    }

    public string GenerateInfrastructureCacheKey(Guid locationId, LocationTypeEnum locationType)
        => $"infra:{locationType}:{locationId}";
}
```

**Pattern Notes:**
- ✅ `IMemoryCache` for in-memory caching
- ✅ Options pattern (`IOptions<T>`) for configuration
- ✅ Sliding expiration for cache entries
- ✅ Pattern-based cache invalidation
- ✅ Thread-safe `ConcurrentDictionary` for tracking
- ✅ Size-aware caching for memory limits

---

## 4. Migration Patterns

### 4.1 DbContext Configuration

**File Path:** [NTC.QoS.Infrastructure/Persistence/ApplicationDbContext.cs](../NTC.QoS.Infrastructure/Persistence/ApplicationDbContext.cs)

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NetTopologySuite.Geometries;

namespace NTC.QoS.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration) 
    : DbContext(options)
{
    private readonly IConfiguration _configuration = configuration;

    // DbSets
    public DbSet<Provider> Providers { get; set; }
    public DbSet<Cell> Cells { get; set; }
    public DbSet<Barangay> Barangays { get; set; }
    public DbSet<CellularCoverage> CellularCoverages { get; set; }
    public DbSet<ConnectivityAssessment> ConnectivityAssessments { get; set; }
    // ... (more DbSets)

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(
            _configuration.GetConnectionString("PostgreSqlConnection"), 
            x => 
            {
                x.UseNetTopologySuite();  // Enable PostGIS
                x.UseVector();            // Enable pgvector
                x.CommandTimeout(300);    // 5 minutes for spatial queries
            });
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Enable PostgreSQL extensions
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.HasPostgresExtension("vector");

        // Global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "entity");
                var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var notDeletedExpression = Expression.Not(property);
                var lambda = Expression.Lambda(notDeletedExpression, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        // Entity configurations
        modelBuilder.Entity<Provider>()
            .HasIndex(p => p.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<Provider>()
            .HasMany(p => p.ServiceProviders);

        // ... (more entity configurations)
    }
}
```

**Pattern Notes:**
- ✅ Primary constructor with `DbContextOptions` and `IConfiguration`
- ✅ PostGIS support with `UseNetTopologySuite()`
- ✅ pgvector support with `UseVector()`
- ✅ Global query filter for soft delete (`IsDeleted = false`)
- ✅ Unique indexes with soft delete filter
- ✅ Extended command timeout for spatial queries (300 seconds)

### 4.2 Migration Naming Convention

**Location:** [NTC.QoS.Infrastructure/Migrations](../NTC.QoS.Infrastructure/Migrations)

**Pattern:** `YYYYMMDDHHMMSS_DescriptiveName.cs`

**Examples:**
- `20251117060712_Initial.cs` - Initial migration (database creation)
- `20251228043526_AddBarangayCoverage.cs` - Add new entity
- `20251202084044_AddBandIdAndMunicipalityIdToNetwork.cs` - Add foreign keys
- `20251225160215_AddWirelessCoverageToConnectivityAssessment.cs` - Add column

**Migration Example:**

**File Path:** [NTC.QoS.Infrastructure/Migrations/20251228043526_AddBarangayCoverage.cs](../NTC.QoS.Infrastructure/Migrations/20251228043526_AddBarangayCoverage.cs)

```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBarangayCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BarangayCoverages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BarangayId = table.Column<Guid>(type: "uuid", nullable: false),
                    WiredFtthPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    WirelessPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarangayCoverages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BarangayCoverages_Barangays_BarangayId",
                        column: x => x.BarangayId,
                        principalTable: "Barangays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BarangayCoverages_BarangayId",
                table: "BarangayCoverages",
                column: "BarangayId",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BarangayCoverages");
        }
    }
}
```

**Pattern Notes:**
- ✅ Descriptive class name (`AddBarangayCoverage`)
- ✅ All BaseEntity fields included (audit + soft delete)
- ✅ Foreign keys with explicit naming
- ✅ Indexes with soft delete filter (`"IsDeleted" = FALSE`)
- ✅ `Up()` and `Down()` methods for rollback support

### 4.3 Migration Commands

```bash
# Add new migration
dotnet ef migrations add MigrationName --project NTC.QoS.Infrastructure --startup-project NTC.QoS.API

# Apply migrations
dotnet ef database update --project NTC.QoS.Infrastructure --startup-project NTC.QoS.API

# Remove last migration (if not applied)
dotnet ef migrations remove --project NTC.QoS.Infrastructure --startup-project NTC.QoS.API

# Generate SQL script
dotnet ef migrations script --project NTC.QoS.Infrastructure --startup-project NTC.QoS.API
```

---

## 5. Dependency Injection

### 5.1 Infrastructure Service Registration

**File Path:** [NTC.QoS.Infrastructure/InfrastructureDependencyInjection.cs](../NTC.QoS.Infrastructure/InfrastructureDependencyInjection.cs)

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NTC.QoS.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register logging first
        services.AddLogging();

        // Register services in logical groups
        services.AddDatabaseServices(configuration);
        services.AddHangfireServices(configuration);
        services.AddExternalServices(configuration);
        services.AddBlobStorageServices(configuration);
        services.AddRepositories();
        services.AddBackgroundJobServices();
        services.AddReportingServices();

        return services;
    }
}
```

**Extension Method Example - Repositories:**

**File Path:** [NTC.QoS.Infrastructure/Extensions/RepositoryServiceExtensions.cs](../NTC.QoS.Infrastructure/Extensions/RepositoryServiceExtensions.cs)

```csharp
using Microsoft.Extensions.DependencyInjection;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Persistence.Repositories;

namespace NTC.QoS.Infrastructure.Extensions;

internal static class RepositoryServiceExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Generic repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Specific repositories
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<IBarangayRepository, BarangayRepository>();
        services.AddScoped<ICellRepository, CellRepository>();
        services.AddScoped<ICellularRepository, CellularRepository>();
        services.AddScoped<ISpeedtestRepository, SpeedtestRepository>();
        services.AddScoped<IConnectivityAssessmentRepository, ConnectivityAssessmentRepository>();
        // ... (more repositories)

        return services;
    }
}
```

**Pattern Notes:**
- ✅ Extension methods on `IServiceCollection`
- ✅ `AddScoped` for per-request lifetime (EF Core DbContext)
- ✅ Generic repository registration: `typeof(IRepository<>), typeof(Repository<>)`
- ✅ Grouped by logical concern (repositories, services, background jobs)

### 5.2 Application Service Registration

**File Path:** [NTC.QoS.Application/ApplicationDependencyInjection.cs](../NTC.QoS.Application/ApplicationDependencyInjection.cs)

```csharp
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NTC.QoS.Application.Behaviors;
using NTC.QoS.Application.Services;
using System.Reflection;

namespace NTC.QoS.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var applicationAssembly = typeof(ApplicationDependencyInjection).Assembly;

        // MediatR (CQRS)
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(applicationAssembly);
            cfg.Lifetime = ServiceLifetime.Scoped;
        });

        // FluentValidation
        services.AddFluentValidationAutoValidation();
        services.Scan(scan => scan
            .FromAssemblies(Assembly.GetExecutingAssembly())
            .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Pipeline behaviors
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Background job scanning (using Scrutor)
        services.Scan(scan =>
            scan.FromAssemblies(Assembly.GetExecutingAssembly())
                .AddClasses(classes => classes.AssignableTo(typeof(IBackgroundJob<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );

        // Application services
        services.AddScoped<IConnectivityCacheService, ConnectivityCacheService>();
        services.AddScoped<IConnectivityReportingService, ConnectivityReportingService>();
        services.AddScoped<BroadbandAnalyticsService>();
        services.AddScoped<IMCPServerService, MCPServerService>();

        // Configure options
        services.Configure<ConnectivityCacheOptions>(
            configuration.GetSection("ConnectivityCache"));

        return services;
    }
}
```

**Pattern Notes:**
- ✅ MediatR auto-registration from assembly
- ✅ Scrutor scanning for validators and background jobs
- ✅ Options pattern with `Configure<T>()`
- ✅ Behavior pipeline registration

### 5.3 API Service Registration

**File Path:** [NTC.QoS.API/Program.cs](../NTC.QoS.API/Program.cs)

```csharp
using NTC.QoS.Application;
using NTC.QoS.Infrastructure;

namespace NTC.QoS.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var env = builder.Environment;

        // Configuration
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        // Authentication (Clerk)
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(x => { /* JWT config */ });

        builder.Services.AddAuthorization();
        
        // CORS
        builder.Services.AddCors(options => { /* CORS policies */ });

        // Controllers with JSON options
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options => { /* Swagger config */ });

        // Layer registration (extension methods)
        builder.Services.AddApplication(builder.Configuration);
        builder.Services.AddInfrastructure(builder.Configuration);

        // Additional services
        builder.Services.AddMemoryCache();
        builder.Services.AddResponseCaching();
        builder.Services.AddResponseCompression();
        builder.Services.AddHangfireServer();

        // Health checks
        builder.Services.AddHealthChecks()
            .AddCheck<PendingMigrationsHealthCheck>("pending_migrations")
            .AddCheck<DbConnectionHealthCheck>("db");

        var app = builder.Build();

        // Initialize (migrations, seeding)
        await app.Services.InitializeApplicationAsync(builder.Configuration, env);

        // Middleware pipeline
        app.UseCors(env.IsDevelopment() ? "Dev" : "Prod");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
```

**Pattern Notes:**
- ✅ Top-level statements with `Program` class
- ✅ Layer-specific extension methods (`AddApplication`, `AddInfrastructure`)
- ✅ Environment-based configuration
- ✅ Health checks for database and migrations
- ✅ Startup initialization for migrations and seeding

### 5.4 Service Lifetime Guidelines

| Lifetime | Use Case | Example |
|----------|----------|---------|
| **Singleton** | Stateless services, caches, configuration | `IMemoryCache`, `IConfiguration` |
| **Scoped** | Per-request services, DbContext, repositories | `ApplicationDbContext`, `IRepository<T>` |
| **Transient** | Lightweight, stateless services | Not commonly used in this project |

**Best Practices:**
- ✅ Use `AddScoped` for repositories (matches DbContext lifetime)
- ✅ Use `AddScoped` for services that depend on repositories
- ✅ Use `AddSingleton` for caches and configuration
- ✅ Use `AddScoped` for MediatR handlers and validators
- ✅ Avoid capturing scoped services in singletons

---

## Summary Checklist

When implementing new features, ensure consistency by following these patterns:

### ✅ Entity Checklist
- [ ] Inherit from `BaseEntity`
- [ ] Initialize required strings to `string.Empty`
- [ ] Use nullable types (`?`) for optional fields
- [ ] Initialize collections to `[]`
- [ ] Use NetTopologySuite for geospatial types
- [ ] Add XML documentation for enums

### ✅ Repository Checklist
- [ ] Create interface in `Domain/Interfaces` extending `IRepository<T>`
- [ ] Implement in `Infrastructure/Persistence/Repositories`
- [ ] Use `internal sealed class` with primary constructor
- [ ] Return `IQueryable<T>` for composable queries
- [ ] Use async methods with `CancellationToken`
- [ ] Register in `RepositoryServiceExtensions.cs`

### ✅ Migration Checklist
- [ ] Use descriptive name: `YYYYMMDDHHMMSS_DescriptiveName`
- [ ] Include all BaseEntity fields
- [ ] Add indexes with soft delete filter
- [ ] Test `Up()` and `Down()` methods
- [ ] Review SQL generated with `migrations script`

### ✅ Service Registration Checklist
- [ ] Create extension method in appropriate layer
- [ ] Use `AddScoped` for per-request services
- [ ] Group related services together
- [ ] Use Options pattern for configuration
- [ ] Register in layer's `DependencyInjection.cs`

---

## Additional Resources

- **Clean Architecture:** [https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- **Repository Pattern:** [https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- **MediatR CQRS:** [https://github.com/jbogard/MediatR](https://github.com/jbogard/MediatR)
- **EF Core Migrations:** [https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- **NetTopologySuite:** [https://github.com/NetTopologySuite/NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite)

---

**End of Document**
