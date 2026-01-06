using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentJobIdToFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add column only if it doesn't exist
            // This handles dev environment where the column may already exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Files' AND column_name = 'AssessmentJobId'
                    ) THEN
                        ALTER TABLE ""Files"" ADD ""AssessmentJobId"" character varying(450);
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Files' AND column_name = 'AssessmentJobId'
                    ) THEN
                        ALTER TABLE ""Files"" DROP COLUMN ""AssessmentJobId"";
                    END IF;
                END $$;
            ");
        }
    }
}
