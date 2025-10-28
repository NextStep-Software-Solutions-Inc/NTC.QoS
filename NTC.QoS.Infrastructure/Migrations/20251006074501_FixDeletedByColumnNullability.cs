//using Microsoft.EntityFrameworkCore.Migrations;

//#nullable disable

//namespace NTC.QoS.Infrastructure.Migrations
//{
//    /// <inheritdoc />
//    public partial class FixDeletedByColumnNullability : Migration
//    {
//        /// <inheritdoc />
//        protected override void Up(MigrationBuilder migrationBuilder)
//        {
//            // Fix DeletedBy column to be nullable for all tables
//            var tableNames = new[]
//            {
//                "ConnectivityAssessments", "WiFiHotspots", "AntennaPatterns", "AreaStudies", "Bands", 
//                "Barangays", "CellSectors", "Cellulars", "CellularCoverages", "Cells", "Files", 
//                "GenerationBands", "Generations", "Links", "MIMOs", "Municipalities", "NAPs", 
//                "NetworkTemplates", "Networks", "Organizations", "Permissions", "Providers", 
//                "Provinces", "Regions", "RolePermissions", "Roles", "Sectors", "ServiceProviders", 
//                "Services", "Speedtests", "Studies", "StudyMunicipalities", "StudyNetworks", 
//                "SystemTypes", "Technologies", "TechnologyBands", "Users", "UserRegions", 
//                "UserTypes", "WirelessValidations", "Zone"
//            };

//            foreach (var tableName in tableNames)
//            {
//                migrationBuilder.Sql($"ALTER TABLE \"{tableName}\" ALTER COLUMN \"DeletedBy\" DROP NOT NULL;");
//            }
//        }

//        /// <inheritdoc />
//        protected override void Down(MigrationBuilder migrationBuilder)
//        {
//            // Revert DeletedBy column to be not nullable for all tables
//            var tableNames = new[]
//            {
//                "ConnectivityAssessments", "WiFiHotspots", "AntennaPatterns", "AreaStudies", "Bands", 
//                "Barangays", "CellSectors", "Cellulars", "CellularCoverages", "Cells", "Files", 
//                "GenerationBands", "Generations", "Links", "MIMOs", "Municipalities", "NAPs", 
//                "NetworkTemplates", "Networks", "Organizations", "Permissions", "Providers", 
//                "Provinces", "Regions", "RolePermissions", "Roles", "Sectors", "ServiceProviders", 
//                "Services", "Speedtests", "Studies", "StudyMunicipalities", "StudyNetworks", 
//                "SystemTypes", "Technologies", "TechnologyBands", "Users", "UserRegions", 
//                "UserTypes", "WirelessValidations", "Zone"
//            };

//            foreach (var tableName in tableNames)
//            {
//                migrationBuilder.Sql($"ALTER TABLE \"{tableName}\" ALTER COLUMN \"DeletedBy\" SET NOT NULL;");
//            }
//        }
//    }
//}
