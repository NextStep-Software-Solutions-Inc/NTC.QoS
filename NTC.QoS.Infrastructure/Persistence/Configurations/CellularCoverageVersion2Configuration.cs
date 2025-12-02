using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Infrastructure.Persistence.Configurations;

internal sealed class CellularCoverageVersion2Configuration : IEntityTypeConfiguration<CellularCoverageVersion2>
{
    public void Configure(EntityTypeBuilder<CellularCoverageVersion2> builder)
    {
        builder.ToTable("CellularCoveragesV2");

        builder.HasKey(cc => cc.Id);

        // Foreign key relationships
        builder
            .HasOne(cc => cc.Network)
            .WithMany()
            .HasForeignKey(cc => cc.NetworkId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(cc => cc.Band)
            .WithMany()
            .HasForeignKey(cc => cc.BandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(cc => cc.Technology)
            .WithMany()
            .HasForeignKey(cc => cc.TechnologyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(cc => cc.CellSector)
            .WithMany()
            .HasForeignKey(cc => cc.CellSectorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(cc => cc.Municipality)
            .WithMany()
            .HasForeignKey(cc => cc.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        // Location configuration
        builder
            .Property(cc => cc.Location)
            .HasColumnType("geometry (Point,4326)")
            .IsRequired()
            .HasDefaultValue(new Point(0, 0) { SRID = 4326 });

        // Signal strength configuration
        builder
            .Property(cc => cc.SignalStrength)
            .HasPrecision(10, 2)
            .IsRequired();

        // Address configuration
        builder
            .Property(cc => cc.Address)
            .HasMaxLength(500);

        // Indexes for performance optimization
        builder
            .HasIndex(cc => cc.Location)
            .HasMethod("GIST")
            .HasDatabaseName("IX_CellularCoverageV2_Location");

        // Composite index for duplicate detection
        builder
            .HasIndex(cc => new { cc.NetworkId, cc.BandId, cc.MunicipalityId })
            .HasDatabaseName("IX_CellularCoverageV2_Network_Band_Municipality");

        builder
            .HasIndex(cc => cc.CellSectorId)
            .HasDatabaseName("IX_CellularCoverageV2_CellSectorId");

        builder
            .HasIndex(cc => cc.MunicipalityId)
            .HasDatabaseName("IX_CellularCoverageV2_MunicipalityId");

        builder
            .HasIndex(cc => cc.IsFileUploaded)
            .HasDatabaseName("IX_CellularCoverageV2_IsFileUploaded");
    }
}
