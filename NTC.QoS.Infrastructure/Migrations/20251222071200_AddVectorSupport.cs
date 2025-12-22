using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVectorSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable the pgvector extension
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

            // Add the Embedding column to GeneratedReports
            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "GeneratedReports",
                type: "vector(768)",
                nullable: true);

            // Create HNSW index for semantic search
            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReport_Embedding",
                table: "GeneratedReports",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GeneratedReport_Embedding",
                table: "GeneratedReports");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "GeneratedReports");

            // Note: We don't drop the extension as other tables might use it
        }
    }
}
