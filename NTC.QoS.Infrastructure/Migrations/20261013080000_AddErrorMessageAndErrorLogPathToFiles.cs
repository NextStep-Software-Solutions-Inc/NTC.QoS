using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NTC.QoS.Infrastructure.Persistence;

#nullable disable

namespace NTC.QoS.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261013080000_AddErrorMessageAndErrorLogPathToFiles")]
    public partial class AddErrorMessageAndErrorLogPathToFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Files' AND column_name = 'ErrorMessage'
                    ) THEN
                        ALTER TABLE ""Files"" ADD ""ErrorMessage"" character varying(2000);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Files' AND column_name = 'ErrorLogPath'
                    ) THEN
                        ALTER TABLE ""Files"" ADD ""ErrorLogPath"" character varying(1000);
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
                        WHERE table_name = 'Files' AND column_name = 'ErrorMessage'
                    ) THEN
                        ALTER TABLE ""Files"" DROP COLUMN ""ErrorMessage"";
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Files' AND column_name = 'ErrorLogPath'
                    ) THEN
                        ALTER TABLE ""Files"" DROP COLUMN ""ErrorLogPath"";
                    END IF;
                END $$;
            ");
        }
    }
}
