// MyWebApi.Infrastructure/Persistence/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Infrastructure.Persistence;
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration) : DbContext(options)
{
    private readonly IConfiguration _configuration = configuration;

    public DbSet<Provider> Providers { get; set; } // Your entities

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ... any entity configuration ...
        modelBuilder.Entity<Provider>()
            .HasIndex(p => p.Name)
            .IsUnique();
    }
}