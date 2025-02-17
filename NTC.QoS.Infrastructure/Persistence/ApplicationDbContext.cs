using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;

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


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {

    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ... any entity configuration ...
        modelBuilder.Entity<Provider>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<Provider>()
            .HasMany(p => p.ServiceProviders);

        modelBuilder.Entity<Service>()
            .HasIndex(s => s.Name)
            .IsUnique();

        modelBuilder.Entity<Service>()
            .HasMany(s => s.ServiceProviders);

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
    }
}