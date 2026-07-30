using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Helper;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration) : DbContext(options)
{
    private readonly IConfiguration _configuration = configuration;

    public DbSet<Provider> Providers { get; set; } // Your entities
    public DbSet<Cell> Cells { get; set; }
    public DbSet<GenerationBand> GenerationBands { get; set; }
    public DbSet<TechnologyBand> TechnologyBands { get; set; }
    public DbSet<Link> Links { get; set; }
    public DbSet<NAP> NAPs { get; set; }
    public DbSet<Network> Networks { get; set; }
    public DbSet<ServiceProvider> ServiceProviders { get; set; }
    public DbSet<StudyMunicipality> StudyMunicipalities { get; set; }
    public DbSet<StudyNetwork> StudyNetworks { get; set; }
    public DbSet<Generation> Generations { get; set; }
    public DbSet<AntennaPattern> AntennaPatterns { get; set; }
    public DbSet<AreaStudy> AreaStudies { get; set; }
    public DbSet<SignalLevel> SignalLevels { get; set; }
    public DbSet<SignalLevelEntry> SignalLevelEntries { get; set; }
    public DbSet<Band> Bands { get; set; }
    public DbSet<Barangay> Barangays { get; set; }
    public DbSet<BarangayCoverage> BarangayCoverages { get; set; }
    public DbSet<Cellular> Cellulars { get; set; }
    public DbSet<Domain.Entities.File> Files { get; set; }
    public DbSet<MIMO> MIMOs { get; set; }
    public DbSet<Municipality> Municipalities { get; set; }
    public DbSet<NetworkTemplate> NetworkTemplates { get; set; }
    public DbSet<Province> Provinces { get; set; }
    public DbSet<Region> Regions { get; set; }
    public DbSet<Sector> Sectors { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<Speedtest> Speedtests { get; set; }
    public DbSet<Study> Studies { get; set; }
    public DbSet<Technology> Technologies { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserType> UserTypes { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<CellSector> CellSectors { get; set; }
    public DbSet<CellularCoverage> CellularCoverages { get; set; }
    public DbSet<SystemType> SystemTypes { get; set; }
    public DbSet<UserRegion> UserRegions { get; set; }
    public DbSet<WirelessValidation> WirelessValidations { get; set; }
    public DbSet<ConnectivityAssessment> ConnectivityAssessments { get; set; }
    public DbSet<WiFiHotspot> WiFiHotspots { get; set; }
    public DbSet<ConnectivityAssessmentScheduler> ConnectivityAssessmentSchedulers { get; set; }
    public DbSet<GeneratedReport> GeneratedReports { get; set; }
    public DbSet<ReportTemplate> ReportTemplates { get; set; }
    public DbSet<FrequencyAllocation> FrequencyAllocations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(
            _configuration.GetConnectionString("PostgreSqlConnection"), 
            x => 
            {
                x.UseNetTopologySuite();
                x.UseVector(); // Enable pgvector support
                // Increase command timeout for long-running spatial queries
                x.CommandTimeout(300); // 5 minutes
            });
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.HasPostgresExtension("vector"); // Enable pgvector extension

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

        // ... any entity configuration ...
        modelBuilder.Entity<Provider>()
            .HasIndex(p => p.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<Provider>()
            .HasMany(p => p.ServiceProviders);

        modelBuilder.Entity<Service>()
            .HasIndex(s => new { s.Name, s.Type })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<Service>()
            .Property(s => s.Type)
            .HasConversion<int>();

        modelBuilder.Entity<Service>()
            .HasMany(s => s.ServiceProviders);

        modelBuilder.Entity<ServiceProvider>()
            .HasIndex(sp => new { sp.ServiceId, sp.ProviderId, sp.MNC })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<ServiceProvider>()
            .HasOne(sp => sp.Service)
            .WithMany(s => s.ServiceProviders)
            .HasForeignKey(sp => sp.ServiceId);

        modelBuilder.Entity<ServiceProvider>()
            .HasOne(sp => sp.Provider)
            .WithMany(p => p.ServiceProviders)
            .HasForeignKey(sp => sp.ProviderId);

        modelBuilder.Entity<Technology>()
            .HasIndex(t => t.Standard)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<Technology>()
            .HasOne(t => t.Generation)
            .WithMany(g => g.Technologies)
            .HasForeignKey(t => t.GenerationId);

        modelBuilder.Entity<Generation>()
            .HasMany(g => g.Technologies);

        modelBuilder.Entity<Generation>()
            .HasMany(g => g.GenerationBands);

        modelBuilder.Entity<Band>()
            .HasIndex(b => b.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<Band>()
            .HasMany(b => b.TechnologyBands);

        modelBuilder.Entity<GenerationBand>()
            .HasOne(gb => gb.Band)
            .WithMany(b => b.GenerationBands)
            .HasForeignKey(gb => gb.BandId);

        modelBuilder.Entity<GenerationBand>()
            .HasOne(gb => gb.Generation)
            .WithMany(g => g.GenerationBands)
            .HasForeignKey(gb => gb.GenerationId);

        modelBuilder.Entity<TechnologyBand>()
            .HasOne(tb => tb.Band)
            .WithMany(b => b.TechnologyBands)
            .HasForeignKey(tb => tb.BandId);

        modelBuilder.Entity<TechnologyBand>()
            .HasOne(tb => tb.Technology)
            .WithMany(t => t.TechnologyBands)
            .HasForeignKey(tb => tb.TechnologyId);

        modelBuilder.Entity<TechnologyBand>()
            .HasIndex(tb => new { tb.TechnologyId, tb.BandId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<Link>()
            .HasOne(l => l.ServiceProvider)
            .WithMany(sp => sp.Links)
            .HasForeignKey(l => l.ServiceProviderId);

        modelBuilder.Entity<Link>()
            .HasOne(l => l.Band)
            .WithMany(b => b.Links)
            .HasForeignKey(l => l.BandId);

        modelBuilder.Entity<Network>()
            .HasOne(n => n.NetworkTemplate)
            .WithMany(nt => nt.Networks)
            .HasForeignKey(n => n.NetworkTemplateId);

        modelBuilder.Entity<Network>()
            .HasIndex(n => new { n.ServiceProviderId, n.Name })
            .HasDatabaseName("IX_Networks_ServiceProviderId_Name_Active")
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<AreaStudy>()
            .HasIndex(p => p.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<AreaStudy>()
            .HasMany(a => a.Studies)
            .WithOne(s => s.AreaStudy)
            .HasForeignKey(s => s.AreaStudyId);

        modelBuilder.Entity<AreaStudy>()
            .HasMany(a => a.SignalLevels)
            .WithOne(sl => sl.AreaStudy)
            .HasForeignKey(sl => sl.AreaStudyId);

        modelBuilder.Entity<SignalLevel>()
            .HasMany(sl => sl.SignalLevelEntries)
            .WithOne(sle => sle.SignalLevel)
            .HasForeignKey(sle => sle.SignalLevelId);

        modelBuilder.Entity<SignalLevelEntry>()
            .HasIndex(sle => new { sle.SignalLevelId, sle.ThresholdFrom, sle.ThresholdTo, sle.SortOrder })
            .HasDatabaseName("IX_SignalLevelEntries_SignalLevelId_Range_SortOrder_Active")
            .IsUnique()
            .AreNullsDistinct(false)
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<Study>()
            .HasMany(s => s.StudyMunicipalities)
            .WithOne(sm => sm.Study)
            .HasForeignKey(sm => sm.StudyId);

        modelBuilder.Entity<StudyNetwork>()
            .HasOne(sn => sn.Study)
            .WithMany(s => s.StudyNetworks)
            .HasForeignKey(sn => sn.StudyId);

        modelBuilder.Entity<StudyNetwork>()
            .HasOne(sn => sn.Network)
            .WithMany(n => n.StudyNetworks)
            .HasForeignKey(sn => sn.NetworkId);

        modelBuilder.Entity<StudyMunicipality>()
            .HasOne(sm => sm.Study)
            .WithMany(s => s.StudyMunicipalities)
            .HasForeignKey(sm => sm.StudyId);

        // Cell configuration - reverted to original IX_Cells_Name unique constraint
        modelBuilder.Entity<Cell>()
            .HasIndex(c => c.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<Cell>()
            .Property(c => c.Status)
            .HasConversion<int>();

        modelBuilder.Entity<Cell>()
            .Property(c => c.TowerType)
            .HasConversion(
                v => v.ToString(),
                v => ConvertToTowerType(v));

        modelBuilder.Entity<Cell>()
            .HasOne(c => c.ServiceProvider)
            .WithMany(sp => sp.Cells)
            .HasForeignKey(c => c.ServiceProviderId);

        modelBuilder.Entity<Sector>()
            .HasOne(s => s.MIMO)
            .WithMany(m => m.Sectors)
            .HasForeignKey(s => s.MIMOId);

        modelBuilder.Entity<Sector>()
            .HasOne(s => s.Network)
            .WithMany(n => n.Sectors)
            .HasForeignKey(s => s.NetworkId);

        modelBuilder.Entity<Sector>()
            .HasOne(s => s.CellSector)
            .WithMany(cs => cs.Sectors)
            .HasForeignKey(s => s.CellSectorId);

        modelBuilder.Entity<Sector>()
            .HasIndex(s => s.CellSectorId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<Sector>()
            .HasOne(s => s.TxAntennaPattern)
            .WithMany(a => a.TxSectors)
            .HasForeignKey(s => s.TxAntennaPatternId);

        modelBuilder.Entity<Sector>()
            .HasOne(s => s.RxAntennaPattern)
            .WithMany(a => a.RxSectors)
            .HasForeignKey(s => s.RxAntennaPatternId);

        modelBuilder.Entity<NAP>()
            .HasOne(n => n.ServiceProvider)
            .WithMany(sp => sp.NAPs)
            .HasForeignKey(n => n.ServiceProviderId);

        modelBuilder.Entity<NAP>()
            .Property(n => n.Location)
            .HasColumnType("geometry (Point,4326)")
            .HasDefaultValue(GeometryHelper.CreatePoint(new Coordinate(0, 0)));

        modelBuilder.Entity<NAP>()
            .HasIndex(n => n.Location)
            .HasDatabaseName("IX_NAP_Location")
            .HasMethod("GIST");

        modelBuilder.Entity<Speedtest>()
            .HasOne(s => s.ServiceProvider)
            .WithMany(p => p.Speedtests)
            .HasForeignKey(s => s.ServiceProviderId);

        modelBuilder.Entity<Speedtest>()
            .HasOne(s => s.Generation)
            .WithMany(g => g.Speedtests)
            .HasForeignKey(s => s.GenerationId);

        modelBuilder.Entity<Speedtest>()
            .Property(n => n.Location)
            .HasColumnType("geometry (Point,4326)")
            .HasDefaultValue(GeometryHelper.CreatePoint(0, 0));

        modelBuilder.Entity<Speedtest>()
            .HasIndex(n => n.Location)
            .HasDatabaseName("IX_Speedtest_Location")
            .HasMethod("GIST");

        modelBuilder.Entity<Cellular>()
            .HasOne(c => c.CellSector)
            .WithMany(cl => cl.Cellulars)
            .HasForeignKey(c => c.CellSectorId);

        modelBuilder.Entity<Cellular>()
            .HasOne(c => c.ServiceProvider)
            .WithMany(sp => sp.Cellulars)
            .HasForeignKey(c => c.ServiceProviderId);

        modelBuilder.Entity<Cellular>()
            .HasOne(c => c.Technology)
            .WithMany(t => t.Cellulars)
            .HasForeignKey(c => c.TechnologyId);

        modelBuilder.Entity<Cellular>()
            .HasOne(c => c.Band)
            .WithMany(b => b.Cellulars)
            .HasForeignKey(c => c.BandId);

        modelBuilder.Entity<Cellular>()
            .HasIndex(c => new { c.CellSectorId, c.ServiceProviderId, c.TechnologyId, c.BandId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("IX_Cellulars_UQ_CellSector_ServiceProvider_Technology_Band");

        modelBuilder.Entity<Province>()
            .HasOne(p => p.Region)
            .WithMany(r => r.Provinces)
            .HasForeignKey(p => p.RegionId);

        modelBuilder.Entity<Province>()
            .HasMany(p => p.Municipalities);

        modelBuilder.Entity<Region>()
            .HasMany(r => r.Provinces);

        modelBuilder.Entity<Domain.Entities.File>()
            .HasOne(f => f.Region)
            .WithMany()
            .HasForeignKey(f => f.RegionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Domain.Entities.File>()
            .HasIndex(f => f.RegionId)
            .HasDatabaseName("IX_Files_RegionId");

        modelBuilder.Entity<Domain.Entities.File>()
            .Property(f => f.AssessmentJobId)
            .HasMaxLength(450)
            .IsRequired(false);

        modelBuilder.Entity<Municipality>()
            .HasMany(m => m.Barangays)
            .WithOne(b => b.Municipality)
            .HasForeignKey(b => b.MunicipalityId);

        modelBuilder.Entity<Barangay>()
            .HasMany(b => b.Zones)
            .WithOne(z => z.Barangay)
            .HasForeignKey(z => z.BarangayId);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Organization)
            .WithMany(o => o.Users)
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Organization>()
            .HasMany(o => o.Users);

        modelBuilder.Entity<Organization>()
            .HasIndex(o => o.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<Role>()
            .HasMany(r => r.Users);

        modelBuilder.Entity<Permission>()
            .HasIndex(p => p.Key)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasMany(r => r.RolePermissions)
            .WithOne(rp => rp.Role)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Key)
            .IsUnique();

        modelBuilder.Entity<Permission>()
            .HasMany(p => p.RolePermissions)
            .WithOne(rp => rp.Permission)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePermission>()
            .HasIndex(rp => new { rp.PermissionId, rp.RoleId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("IX_RolePermissions_PermissionId_RoleId_Active");

        modelBuilder.Entity<MIMO>()
            .HasMany(m => m.Sectors);

        modelBuilder.Entity<CellSector>()
            .HasOne(cs => cs.Cell)
            .WithMany(cs => cs.CellSectors)
            .HasForeignKey(cs => cs.CellId);

        modelBuilder.Entity<CellSector>()
            .HasMany(cs => cs.Sectors);

        modelBuilder.Entity<Cell>()
            .Property(c => c.Location)
            .HasColumnType("geometry (Point,4326)")
            .HasDefaultValue(GeometryHelper.CreatePoint(new Coordinate(0, 0)));

        modelBuilder.Entity<Cell>()
            .HasIndex(c => c.Location)
            .HasDatabaseName("IX_Cell_Location")
            .HasMethod("GIST");

        modelBuilder.Entity<CellularCoverage>()
            .HasOne(cc => cc.Network)
            .WithMany(n => n.CellularCoverages)
            .HasForeignKey(cc => cc.NetworkId);

        modelBuilder.Entity<CellularCoverage>()
            .Property(cc => cc.Location)
            .HasColumnType("geometry (Point,4326)")
            .HasDefaultValue(GeometryHelper.CreatePoint(new Coordinate(0, 0)));

        modelBuilder.Entity<CellularCoverage>()
            .HasIndex(cc => cc.Location)
            .HasDatabaseName("IX_CellularCoverage_Location")
            .HasMethod("GIST");

        modelBuilder.Entity<SystemType>()
            .HasMany(st => st.Networks)
            .WithOne(n => n.SystemType)
            .HasForeignKey(n => n.SystemTypeId);

        modelBuilder.Entity<SystemType>()
            .HasOne(st => st.Technology)
            .WithMany(t => t.SystemTypes)
            .HasForeignKey(st => st.TechnologyId);

        modelBuilder.Entity<Network>()
            .HasOne(n => n.SystemType)
            .WithMany(st => st.Networks)
            .HasForeignKey(n => n.SystemTypeId);

        modelBuilder.Entity<Network>()
            .HasOne(n => n.ServiceProvider)
            .WithMany(sp => sp.Networks)
            .HasForeignKey(n => n.ServiceProviderId);

        modelBuilder.Entity<Network>()
            .HasOne(n => n.Band)
            .WithMany(b => b.Networks)
            .HasForeignKey(n => n.BandId);

        modelBuilder.Entity<Network>()
            .HasOne(n => n.Municipality)
            .WithMany(m => m.Networks)
            .HasForeignKey(n => n.MunicipalityId);

        modelBuilder.Entity<Technology>()
            .HasMany(t => t.SystemTypes)
            .WithOne(st => st.Technology)
            .HasForeignKey(st => st.TechnologyId);

        modelBuilder.Entity<Region>()
            .Property(r => r.Boundaries)
            .HasColumnType("geometry (MultiPolygon,4326)")
            .HasDefaultValue(GeometryHelper.CreateMultiPolygon([]));

        modelBuilder.Entity<Region>()
            .HasIndex(r => r.Boundaries)
            .HasDatabaseName("IX_Region_Boundaries")
            .HasMethod("GIST");

        modelBuilder.Entity<Region>()
            .Property(r => r.Center)
            .HasColumnType("geometry (Point,4326)")
            .HasDefaultValue(GeometryHelper.CreatePoint(0, 0));

        modelBuilder.Entity<Region>()
            .HasIndex(r => r.Center)
            .HasDatabaseName("IX_Region_Center")
            .HasMethod("GIST");

        modelBuilder.Entity<Province>()
            .Property(p => p.Boundaries)
            .HasColumnType("geometry (MultiPolygon,4326)")
            .HasDefaultValue(GeometryHelper.CreateMultiPolygon([]));

        modelBuilder.Entity<Province>()
            .HasIndex(p => p.Boundaries)
            .HasDatabaseName("IX_Province_Boundaries")
            .HasMethod("GIST");

        modelBuilder.Entity<Province>()
            .Property(p => p.Center)
            .HasColumnType("geometry (Point,4326)")
            .HasDefaultValue(GeometryHelper.CreatePoint(0, 0));

        modelBuilder.Entity<Province>()
            .HasIndex(p => p.Center)
            .HasDatabaseName("IX_Province_Center")
            .HasMethod("GIST");

        modelBuilder.Entity<Municipality>()
            .Property(m => m.Boundaries)
            .HasColumnType("geometry (MultiPolygon,4326)")
            .HasDefaultValue(GeometryHelper.CreateMultiPolygon([]));

        modelBuilder.Entity<Municipality>()
            .HasIndex(m => m.Boundaries)
            .HasDatabaseName("IX_Municipality_Boundaries")
            .HasMethod("GIST");

        modelBuilder.Entity<Municipality>()
            .Property(m => m.Center)
            .HasColumnType("geometry (Point,4326)")
            .HasDefaultValue(GeometryHelper.CreatePoint(0, 0));

        modelBuilder.Entity<Municipality>()
            .HasIndex(m => m.Center)
            .HasDatabaseName("IX_Municipality_Center")
            .HasMethod("GIST");

        modelBuilder.Entity<Barangay>()
            .Property(b => b.Boundaries)
            .HasColumnType("geometry (MultiPolygon,4326)")
            .HasDefaultValue(GeometryHelper.CreateMultiPolygon([]));

        modelBuilder.Entity<Barangay>()
            .HasIndex(b => b.Boundaries)
            .HasDatabaseName("IX_Barangay_Boundaries")
            .HasMethod("GIST");

        modelBuilder.Entity<Barangay>()
            .Property(b => b.Center)
            .HasColumnType("geometry (Point,4326)")
            .HasDefaultValue(GeometryHelper.CreatePoint(0, 0));

        modelBuilder.Entity<Barangay>()
            .HasIndex(b => b.Center)
            .HasDatabaseName("IX_Barangay_Center")
            .HasMethod("GIST");

        modelBuilder.Entity<CellSector>()
            .HasMany(cs => cs.Cellulars);

        modelBuilder.Entity<UserRegion>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRegions)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRegion>()
            .HasOne(ur => ur.Region)
            .WithMany(r => r.UserRegions)
            .HasForeignKey(ur => ur.RegionId);

        modelBuilder.Entity<SystemType>()
            .HasOne(st => st.Generation)
            .WithMany(g => g.SystemTypes)
            .HasForeignKey(st => st.GenerationId);

        modelBuilder.Entity<WirelessValidation>()
            .HasOne(wv => wv.ServiceProvider)
            .WithMany(sp => sp.WirelessValidations)
            .HasForeignKey(wv => wv.ServiceProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WirelessValidation>()
            .HasOne(wv => wv.Barangay)
            .WithMany(b => b.WirelessValidations)
            .HasForeignKey(wv => wv.BarangayId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WirelessValidation>()
            .HasOne(wv => wv.Band)
            .WithMany(b => b.WirelessValidations)
            .HasForeignKey(wv => wv.BandId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WirelessValidation>()
            .HasOne(wv => wv.Technology)
            .WithMany(t => t.WirelessValidations)
            .HasForeignKey(wv => wv.TechnologyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WirelessValidation>()
            .HasIndex(wv => wv.ServiceProviderId)
            .HasDatabaseName("IX_WirelessValidation_ServiceProviderId");

        modelBuilder.Entity<WirelessValidation>()
            .HasIndex(wv => wv.BarangayId)
            .HasDatabaseName("IX_WirelessValidation_BarangayId");

        modelBuilder.Entity<WirelessValidation>()
            .HasIndex(wv => new { wv.TechnologyId, wv.BandId })
            .HasDatabaseName("IX_WirelessValidation_TechnologyId_BandId");

        modelBuilder.Entity<WirelessValidation>()
            .HasIndex(wv => wv.IsMarginal)
            .HasDatabaseName("IX_WirelessValidation_IsMarginal");

        modelBuilder.Entity<ConnectivityAssessment>()
            .Property(ca => ca.Location)
            .HasColumnType("geometry (Point,4326)")
            .HasDefaultValue(GeometryHelper.CreatePoint(0, 0));

        modelBuilder.Entity<ConnectivityAssessment>()
            .HasIndex(ca => ca.Location)
            .HasDatabaseName("IX_ConnectivityAssessment_Location")
            .HasMethod("GIST");

        modelBuilder.Entity<ConnectivityAssessment>()
            .HasIndex(ca => ca.LocationId)
            .IsUnique()
            .HasDatabaseName("IX_ConnectivityAssessment_LocationId");

        modelBuilder.Entity<ConnectivityAssessment>()
            .HasIndex(ca => ca.LocationType)
            .HasDatabaseName("IX_ConnectivityAssessment_LocationType");

        modelBuilder.Entity<ConnectivityAssessment>()
            .HasIndex(ca => ca.ConnectivityStatus)
            .HasDatabaseName("IX_ConnectivityAssessment_Status");

        modelBuilder.Entity<WiFiHotspot>()
            .HasOne(w => w.ServiceProvider)
            .WithMany(sp => sp.WiFiHotspots)
            .HasForeignKey(w => w.ServiceProviderId);

        modelBuilder.Entity<WiFiHotspot>()
            .Property(w => w.Location)
            .HasColumnType("geometry (Point,4326)")
            .HasDefaultValue(GeometryHelper.CreatePoint(0, 0));

        modelBuilder.Entity<WiFiHotspot>()
            .HasIndex(w => w.Location)
            .HasDatabaseName("IX_WiFiHotspot_Location")
            .HasMethod("GIST");

        modelBuilder.Entity<WiFiHotspot>()
            .HasIndex(w => new { w.IsActive, w.IsPublic })
            .HasDatabaseName("IX_WiFiHotspot_Active_Public");

        // GeneratedReport indexes
        modelBuilder.Entity<GeneratedReport>()
            .HasIndex(gr => new { gr.UserId, gr.GeneratedAt })
            .HasDatabaseName("IX_GeneratedReport_UserId_GeneratedAt")
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<GeneratedReport>()
            .HasIndex(gr => gr.Title)
            .HasDatabaseName("IX_GeneratedReport_Title")
            .HasFilter("\"IsDeleted\" = FALSE");

        // Vector embedding HNSW index for semantic search
        modelBuilder.Entity<GeneratedReport>()
            .HasIndex(gr => gr.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasDatabaseName("IX_GeneratedReport_Embedding");

        // FrequencyAllocation configuration
        modelBuilder.Entity<FrequencyAllocation>()
            .HasOne(fa => fa.Provider)
            .WithMany(p => p.FrequencyAllocations)
            .HasForeignKey(fa => fa.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FrequencyAllocation>()
            .HasOne(fa => fa.UpLink)
            .WithMany()
            .HasForeignKey(fa => fa.UpLinkId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FrequencyAllocation>()
            .HasOne(fa => fa.DownLink)
            .WithMany()
            .HasForeignKey(fa => fa.DownLinkId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FrequencyAllocation>()
            .HasIndex(fa => new { fa.ProviderId, fa.UpLinkId, fa.DownLinkId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        // BarangayCoverage unique constraint - one coverage record per barangay
        modelBuilder.Entity<BarangayCoverage>()
            .HasIndex(bc => bc.BarangayId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        modelBuilder.Entity<BarangayCoverage>()
            .HasOne(bc => bc.Barangay)
            .WithMany()
            .HasForeignKey(bc => bc.BarangayId);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Converts database TowerType string values to TowerTypeEnum.
    /// Handles legacy hyphenated values like "Self-Support" and "Wall-Mounted".
    /// </summary>
    private static TowerTypeEnum ConvertToTowerType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return TowerTypeEnum.Unknown;

        // Handle legacy hyphenated values
        return value switch
        {
            "Self-Support" or "SelfSupporting" => TowerTypeEnum.SelfSupporting,
            "Wall-Mounted" or "WallMounted" => TowerTypeEnum.WallMounted,
            _ => Enum.TryParse<TowerTypeEnum>(value, true, out var result) ? result : TowerTypeEnum.Unknown
        };
    }
}
