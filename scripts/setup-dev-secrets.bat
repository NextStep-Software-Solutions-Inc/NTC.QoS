@echo off
REM setup-dev-secrets.bat
REM Script to help developers set up User Secrets for local development

echo ?? Setting up User Secrets for NTC.QoS Development
echo =================================================

REM Check if dotnet CLI is available
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ? Error: .NET CLI is not installed or not in PATH
    pause
    exit /b 1
)

REM Navigate to API project directory
set API_PROJECT=NTC.QoS.API

if not exist "%API_PROJECT%" (
    echo ? Error: Cannot find %API_PROJECT% directory
    echo Please run this script from the solution root directory
    pause
    exit /b 1
)

echo ?? Found API project at: %API_PROJECT%

REM Initialize user secrets if not already done
echo ?? Initializing User Secrets...
dotnet user-secrets init --project "%API_PROJECT%" >nul 2>&1

echo.
echo Setting up secrets for local development...
echo You can skip any secret by pressing Enter without typing a value.
echo.

REM Set up admin password
echo ?? Setting up: Default Admin Password
echo Key: DefaultAdmin:Password
set /p admin_password="Enter admin password (or press Enter for default): "
if "%admin_password%"=="" set admin_password=DevPassword123!
dotnet user-secrets set "DefaultAdmin:Password" "%admin_password%" --project "%API_PROJECT%"
if errorlevel 1 (
    echo ? Failed to set DefaultAdmin:Password
) else (
    echo ? Successfully set DefaultAdmin:Password
)

echo.
REM Set up database connection
echo ?? Setting up: PostgreSQL Connection String
echo Key: ConnectionStrings:PostgreSqlConnection
set /p db_connection="Enter PostgreSQL connection string: "
if not "%db_connection%"=="" (
    dotnet user-secrets set "ConnectionStrings:PostgreSqlConnection" "%db_connection%" --project "%API_PROJECT%"
    if errorlevel 1 (
        echo ? Failed to set ConnectionStrings:PostgreSqlConnection
    ) else (
        echo ? Successfully set ConnectionStrings:PostgreSqlConnection
    )
) else (
    echo ?? Skipped PostgreSQL connection string
)

echo.
REM Set up Hangfire connection
echo ?? Setting up: Hangfire Connection String
echo Key: ConnectionStrings:HangfireConnection
set /p hangfire_connection="Enter Hangfire connection string (or press Enter to use same as PostgreSQL): "
if "%hangfire_connection%"=="" set hangfire_connection=%db_connection%
if not "%hangfire_connection%"=="" (
    dotnet user-secrets set "ConnectionStrings:HangfireConnection" "%hangfire_connection%" --project "%API_PROJECT%"
    if errorlevel 1 (
        echo ? Failed to set ConnectionStrings:HangfireConnection
    ) else (
        echo ? Successfully set ConnectionStrings:HangfireConnection
    )
) else (
    echo ?? Skipped Hangfire connection string
)

echo.
REM Set up Clerk secret key
echo ?? Setting up: Clerk Secret Key
echo Key: Clerk:SecretKey
set /p clerk_secret="Enter Clerk Secret Key: "
if not "%clerk_secret%"=="" (
    dotnet user-secrets set "Clerk:SecretKey" "%clerk_secret%" --project "%API_PROJECT%"
    if errorlevel 1 (
        echo ? Failed to set Clerk:SecretKey
    ) else (
        echo ? Successfully set Clerk:SecretKey
    )
) else (
    echo ?? Skipped Clerk Secret Key
)

echo.
REM Set up Clerk authority
echo ?? Setting up: Clerk Authority URL
echo Key: Clerk:Authority
set /p clerk_authority="Enter Clerk Authority URL: "
if not "%clerk_authority%"=="" (
    dotnet user-secrets set "Clerk:Authority" "%clerk_authority%" --project "%API_PROJECT%"
    if errorlevel 1 (
        echo ? Failed to set Clerk:Authority
    ) else (
        echo ? Successfully set Clerk:Authority
    )
) else (
    echo ?? Skipped Clerk Authority URL
)

echo.
echo ?? User Secrets setup complete!
echo.
echo ?? To view all configured secrets:
echo    dotnet user-secrets list --project %API_PROJECT%
echo.
echo ?? To update a specific secret:
echo    dotnet user-secrets set "key" "value" --project %API_PROJECT%
echo.
echo ??? To remove a secret:
echo    dotnet user-secrets remove "key" --project %API_PROJECT%
echo.
echo ?? Remember: User Secrets are for development only!
echo    Use environment variables for production deployments.
echo.
pause