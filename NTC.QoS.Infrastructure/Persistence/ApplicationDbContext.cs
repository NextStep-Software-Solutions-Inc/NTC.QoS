using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Helper;

namespace NTC.QoS.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration) : DbContext(options)
{
    private readonly IConfiguration _configuration = configuration;

    public DbSet<Provider> Providers { get; set; } // Your entities
    public DbSet<Cell> Cells { get; set; }
    public DbSet<GenerationBand> GenerationBands { get; set; }
    public DbSet<Link> Links { get; set; }
    public DbSet<NAP> NAPs { get; set; }
    public DbSet<Network> Networks { get; set; }
    public DbSet<ServiceProvider> ServiceProviders { get; set; }
    public DbSet<StudyMunicipality> StudyMunicipalities { get; set; }
    public DbSet<StudyNetwork> StudyNetworks { get; set; }
    public DbSet<Generation> Generations { get; set; }
    public DbSet<AntennaPattern> AntennaPatterns { get; set; }
    public DbSet<AreaStudy> AreaStudies { get; set; }
    public DbSet<Band> Bands { get; set; }
    public DbSet<Barangay> Barangays { get; set; }
    public DbSet<Cellular> Cellulars { get; set; }
    public DbSet<NTC.QoS.Domain.Entities.File> Files { get; set; }
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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_configuration.GetConnectionString("PostgreSqlConnection"), x => x.UseNetTopologySuite());
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.HasPostgresExtension("postgis");

        // ... any entity configuration ...
        modelBuilder.Entity<Provider>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<Provider>()
            .HasMany(p => p.ServiceProviders);

        modelBuilder.Entity<Service>()
            .HasIndex(s => new { s.Name, s.Type })
            .IsUnique();

        modelBuilder.Entity<Service>()
            .HasMany(s => s.ServiceProviders);

        modelBuilder.Entity<ServiceProvider>()
            .HasIndex(sp => new { sp.ServiceId, sp.ProviderId, sp.MNC })
            .IsUnique();

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
            .IsUnique();

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
            .IsUnique();

        modelBuilder.Entity<Band>()
            .HasMany(b => b.Links);

        modelBuilder.Entity<Band>()
            .HasMany(g => g.GenerationBands);

        modelBuilder.Entity<GenerationBand>()
            .HasOne(gb => gb.Band)
            .WithMany(b => b.GenerationBands)
            .HasForeignKey(gb => gb.BandId);

        modelBuilder.Entity<GenerationBand>()
            .HasOne(gb => gb.Generation)
            .WithMany(g => g.GenerationBands)
            .HasForeignKey(gb => gb.GenerationId);

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
            .HasOne(n => n.UpLink)
            .WithMany(l => l.UpLinks)
            .HasForeignKey(n => n.UpLinkId);

        modelBuilder.Entity<Network>()
            .HasOne(n => n.DownLink)
            .WithMany(l => l.DownLinks)
            .HasForeignKey(n => n.DownLinkId);

        modelBuilder.Entity<AreaStudy>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<AreaStudy>()
            .HasMany(a => a.Studies)
            .WithOne(s => s.AreaStudy)
            .HasForeignKey(s => s.AreaStudyId);

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

        modelBuilder.Entity<Cell>()
            .HasIndex(c => c.Name)
            .IsUnique();

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
            .HasColumnType("geography (Point,4326)")
            .HasDefaultValue(GeometryHelper.CreatePoint(new Coordinate(0, 0)));

        modelBuilder.Entity<NAP>()
            .HasIndex(n => n.Location)
            .HasDatabaseName("IX_NAP_Location")
            .HasMethod("GIST"); // Use GIST for spatial indexing

        modelBuilder.Entity<Zone>()
            .HasMany(z => z.NAPs);

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
            .HasColumnType("geography (Point,4326)")
            .HasDefaultValue(GeometryHelper.CreatePoint(0, 0));

        modelBuilder.Entity<Cellular>()
            .HasOne(c => c.Cell)
            .WithMany(cl => cl.Cellulars)
            .HasForeignKey(c => c.CellId);

        modelBuilder.Entity<Cellular>()
            .HasOne(c => c.ServiceProvider)
            .WithMany(sp => sp.Cellulars)
            .HasForeignKey(c => c.ServiceProviderId);

        modelBuilder.Entity<Cellular>()
            .HasOne(c => c.GenerationBand)
            .WithMany(gb => gb.Cellulars)
            .HasForeignKey(c => c.GenerationBandId);

        modelBuilder.Entity<Province>()
            .HasOne(p => p.Region)
            .WithMany(r => r.Provinces)
            .HasForeignKey(p => p.RegionId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Region>()
            .HasMany(r => r.Provinces);

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
            .HasForeignKey(u => u.RoleId);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Organization)
            .WithMany(o => o.Users)
            .HasForeignKey(u => u.OrganizationId);

        modelBuilder.Entity<Organization>()
            .HasMany(o => o.Users);

        modelBuilder.Entity<Role>()
            .HasMany(r => r.Users);

        modelBuilder.Entity<Role>()
            .HasMany(r => r.RolePermissions);

        modelBuilder.Entity<Permission>()
            .HasMany(p => p.RolePermissions);

        modelBuilder.Entity<RolePermission>()
            .HasIndex(rp => new { rp.PermissionId, rp.RoleId })
            .IsUnique();

        modelBuilder.Entity<MIMO>()
            .HasMany(m => m.Sectors);

        modelBuilder.Entity<CellSector>()
            .HasOne(cs => cs.Cell)
            .WithMany(cs => cs.CellSectors)
            .HasForeignKey(cs => cs.CellId);

        modelBuilder.Entity<CellSector>()
            .HasMany(cs => cs.Sectors);

        modelBuilder.Entity<Cell>()
            .Property(n => n.Location)
            .HasColumnType("geography (Point,4326)")
            .HasDefaultValue(GeometryHelper.CreatePoint(new Coordinate(0, 0)));

        base.OnModelCreating(modelBuilder);
    }
}