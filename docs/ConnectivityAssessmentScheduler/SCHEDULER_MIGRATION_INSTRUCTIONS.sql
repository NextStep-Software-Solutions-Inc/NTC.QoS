-- Migration Instructions for Connectivity Assessment Scheduler
-- This file documents the database migration process

-- Step 1: Create the migration
-- Run this command in the terminal from the project root:
/*
dotnet ef migrations add AddConnectivityAssessmentScheduler \
  --project NTC.QoS.Infrastructure \
  --startup-project NTC.QoS.API
*/

-- Step 2: Review the generated migration file
-- The migration should create a table named "ConnectivityAssessmentSchedulers" with the following columns:

/*
CREATE TABLE "ConnectivityAssessmentSchedulers" (
    "Id" uuid NOT NULL,
    "HangfireJobId" character varying(200) NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(1000),
    "Frequency" integer NOT NULL,
    "CronExpression" character varying(100) NOT NULL,
    "TimeZone" character varying(100) NOT NULL,
    "Status" integer NOT NULL,
    "LocationType" integer NOT NULL,
    "LocationIdsJson" text NOT NULL,
    "LastExecutionTime" timestamp without time zone,
    "NextExecutionTime" timestamp without time zone,
    "ExecutionCount" integer NOT NULL,
    "FailureCount" integer NOT NULL,
    "LastError" character varying(2000),
    "SendNotifications" boolean NOT NULL,
    "NotificationEmailsJson" character varying(1000),
    "StartDate" timestamp without time zone,
    "EndDate" timestamp without time zone,
    "ConfigurationJson" character varying(2000),
    "CreatedAt" timestamp without time zone NOT NULL,
    "CreatedBy" character varying NOT NULL,
    "UpdatedAt" timestamp without time zone NOT NULL,
    "UpdatedBy" character varying NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp without time zone,
    "DeletedBy" character varying,
    CONSTRAINT "PK_ConnectivityAssessmentSchedulers" PRIMARY KEY ("Id")
);
*/

-- Step 3: Apply the migration
-- Run this command to update the database:
/*
dotnet ef database update \
  --project NTC.QoS.Infrastructure \
  --startup-project NTC.QoS.API
*/

-- Step 4: Verify the migration
-- Run this SQL query to confirm the table was created:
/*
SELECT 
    table_name,
    column_name,
    data_type,
    is_nullable
FROM 
    information_schema.columns
WHERE 
    table_name = 'ConnectivityAssessmentSchedulers'
ORDER BY 
    ordinal_position;
*/

-- Step 5: Create useful indexes (optional but recommended)
-- These indexes will improve query performance:
/*
CREATE INDEX "IX_ConnectivityAssessmentSchedulers_Status" 
    ON "ConnectivityAssessmentSchedulers" ("Status") 
    WHERE "IsDeleted" = FALSE;

CREATE INDEX "IX_ConnectivityAssessmentSchedulers_HangfireJobId" 
    ON "ConnectivityAssessmentSchedulers" ("HangfireJobId") 
    WHERE "IsDeleted" = FALSE;

CREATE INDEX "IX_ConnectivityAssessmentSchedulers_NextExecutionTime" 
    ON "ConnectivityAssessmentSchedulers" ("NextExecutionTime") 
    WHERE "IsDeleted" = FALSE AND "Status" = 0; -- Active status
*/

-- Rollback Instructions (if needed)
-- To rollback the migration:
/*
dotnet ef database update <PreviousMigrationName> \
  --project NTC.QoS.Infrastructure \
  --startup-project NTC.QoS.API
  
-- Then remove the migration file:
dotnet ef migrations remove \
  --project NTC.QoS.Infrastructure \
  --startup-project NTC.QoS.API
*/
