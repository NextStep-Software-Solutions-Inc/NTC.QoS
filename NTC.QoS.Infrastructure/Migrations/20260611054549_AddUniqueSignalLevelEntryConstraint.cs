using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueSignalLevelEntryConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                WITH ranked_entries AS (
                    SELECT
                        ctid,
                        ROW_NUMBER() OVER (
                            PARTITION BY ""SignalLevelId"", ""ThresholdFrom"", ""ThresholdTo"", ""SortOrder""
                            ORDER BY ""CreatedAt"" ASC, ""Id"" ASC
                        ) AS rn
                    FROM ""SignalLevelEntries""
                    WHERE ""IsDeleted"" = FALSE
                )
                DELETE FROM ""SignalLevelEntries"" sle
                USING ranked_entries re
                WHERE sle.ctid = re.ctid
                  AND re.rn > 1;
            ");

            migrationBuilder.DropIndex(
                name: "IX_SignalLevelEntries_SignalLevelId",
                table: "SignalLevelEntries");

            migrationBuilder.CreateIndex(
                name: "IX_SignalLevelEntries_SignalLevelId_ThresholdFrom_ThresholdTo_SortOrder_Active",
                table: "SignalLevelEntries",
                columns: new[] { "SignalLevelId", "ThresholdFrom", "ThresholdTo", "SortOrder" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SignalLevelEntries_SignalLevelId_ThresholdFrom_ThresholdTo_SortOrder_Active",
                table: "SignalLevelEntries");

            migrationBuilder.CreateIndex(
                name: "IX_SignalLevelEntries_SignalLevelId",
                table: "SignalLevelEntries",
                column: "SignalLevelId");
        }
    }
}
