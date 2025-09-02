#!/bin/bash

# setup-dev-secrets.sh
# Script to help developers set up User Secrets for local development

echo "?? Setting up User Secrets for NTC.QoS Development"
echo "================================================="

# Check if dotnet CLI is available
if ! command -v dotnet &> /dev/null; then
    echo "? Error: .NET CLI is not installed or not in PATH"
    exit 1
fi

# Navigate to API project directory
API_PROJECT="NTC.QoS.API"

if [ ! -d "$API_PROJECT" ]; then
    echo "? Error: Cannot find $API_PROJECT directory"
    echo "Please run this script from the solution root directory"
    exit 1
fi

echo "?? Found API project at: $API_PROJECT"

# Function to set a secret
set_secret() {
    local key=$1
    local description=$2
    local default_value=$3
    local is_password=$4
    
    echo ""
    echo "?? Setting up: $description"
    echo "Key: $key"
    
    if [ "$is_password" = "true" ]; then
        echo "Enter value (input will be hidden):"
        read -s value
    else
        if [ -n "$default_value" ]; then
            echo "Enter value (or press Enter for default: $default_value):"
            read value
            if [ -z "$value" ]; then
                value=$default_value
            fi
        else
            echo "Enter value:"
            read value
        fi
    fi
    
    if [ -n "$value" ]; then
        dotnet user-secrets set "$key" "$value" --project "$API_PROJECT"
        if [ $? -eq 0 ]; then
            echo "? Successfully set $key"
        else
            echo "? Failed to set $key"
        fi
    else
        echo "?? Skipped $key (no value provided)"
    fi
}

# Initialize user secrets if not already done
echo "?? Initializing User Secrets..."
dotnet user-secrets init --project "$API_PROJECT" 2>/dev/null

echo ""
echo "Setting up secrets for local development..."
echo "You can skip any secret by pressing Enter without typing a value."

# Set up admin password
set_secret "DefaultAdmin:Password" "Default Admin Password" "DevPassword123!" true

# Set up database connection strings
set_secret "ConnectionStrings:PostgreSqlConnection" "PostgreSQL Connection String" "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=yourpassword;" false

set_secret "ConnectionStrings:HangfireConnection" "Hangfire Connection String" "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=yourpassword;" false

# Set up Clerk secrets
set_secret "Clerk:SecretKey" "Clerk Secret Key" "" true

set_secret "Clerk:Authority" "Clerk Authority URL" "https://your-clerk-instance.clerk.accounts.dev" false

echo ""
echo "?? User Secrets setup complete!"
echo ""
echo "?? To view all configured secrets:"
echo "   dotnet user-secrets list --project $API_PROJECT"
echo ""
echo "?? To update a specific secret:"
echo "   dotnet user-secrets set \"key\" \"value\" --project $API_PROJECT"
echo ""
echo "??? To remove a secret:"
echo "   dotnet user-secrets remove \"key\" --project $API_PROJECT"
echo ""
echo "?? Remember: User Secrets are for development only!"
echo "   Use environment variables for production deployments."