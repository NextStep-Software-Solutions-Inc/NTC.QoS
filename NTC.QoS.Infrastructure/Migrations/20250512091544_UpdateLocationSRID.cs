using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLocationSRID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update the SRID of the Location column in the NAPs table
            migrationBuilder.Sql(
                "UPDATE \"NAPs\" " +
                "SET \"Location\" = ST_Transform(ST_SetSRID(\"Location\", 4326), 3857) " +
                "WHERE \"Location\" IS NOT NULL;"
            );
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert the SRID back to the previous value (e.g., 4326)
            migrationBuilder.Sql(
                "UPDATE \"NAPs\" " +
                "SET \"Location\" = ST_Transform(ST_SetSRID(\"Location\", 3857), 4326) " +
                "WHERE \"Location\" IS NOT NULL;"
            );
        }
    }
}
