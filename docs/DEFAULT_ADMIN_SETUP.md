# Default System Admin User Setup

This system automatically creates a default system administrator user during the initial database seeding process with secure credential management.

## Configuration

### Basic Configuration (appsettings.json)
The default admin user basic information is configured through the `appsettings.json` file in the `DefaultAdmin` section:

```json
{
  "DefaultAdmin": {
    "Email": "nssqos2025@gmail.com",
    "FirstName": "System",
    "LastName": "Administrator"
  }
}
```

⚠️ **Note**: Passwords are **NOT** stored in appsettings.json for security reasons.

### Secure Password Configuration

The system uses a secure credential hierarchy for the admin password:

1. **User Secrets** (Development) - Recommended for local development
2. **Environment Variables** (Production) - Recommended for production deployments  
3. **Generated Password** (Fallback) - Automatically generated if no password is configured

#### Development Setup (User Secrets)

For local development, use .NET User Secrets:

```bash
# Set the admin password using User Secrets
dotnet user-secrets set "DefaultAdmin:Password" "YourSecurePassword123!" --project NTC.QoS.API
```

Or manually add to your `secrets.json` file:
```json
{
  "DefaultAdmin": {
    "Password": "YourSecurePassword123!"
  }
}
```

#### Production Setup (Environment Variables)

For production deployments, set the password as an environment variable:

```bash
# Linux/Docker
export DEFAULT_ADMIN_PASSWORD="YourSecureProductionPassword123!"

# Windows
set DEFAULT_ADMIN_PASSWORD=YourSecureProductionPassword123!

# Docker Compose
environment:
  - DEFAULT_ADMIN_PASSWORD=YourSecureProductionPassword123!

# Kubernetes Secret
apiVersion: v1
kind: Secret
metadata:
  name: admin-credentials
data:
  DEFAULT_ADMIN_PASSWORD: <base64-encoded-password>
```

#### Azure App Service / Cloud Deployment

Set the environment variable in your cloud platform:
- **Azure App Service**: Configuration → Application Settings → `DEFAULT_ADMIN_PASSWORD`
- **AWS**: Environment Variables in deployment configuration
- **Google Cloud**: Environment Variables in App Engine/Cloud Run

## How It Works

1. **Database Seeding**: During application startup, the system runs database migrations and seeding
2. **Password Resolution**: System checks User Secrets → Environment Variables → Generates secure password
3. **User Creation**: The `UserSeeder` checks if a default admin user already exists
4. **Clerk Integration**: If no admin exists, it creates the user both in Clerk (authentication) and the local database
5. **Role Assignment**: The user is automatically assigned the `SuperAdmin` role with the `National` organization
6. **Metadata**: The Clerk user gets metadata indicating their role, organization, and that they're the default admin

## Features

- **Secure Credentials**: Passwords never stored in source code or configuration files
- **Environment Flexible**: Different credential sources for different environments
- **Automatic Fallback**: Generates secure password if none configured
- **Idempotent**: Safe to run multiple times - won't create duplicates
- **Configurable**: Admin details can be customized via configuration
- **Integrated**: Works with both Clerk authentication and local user management
- **Audit Trail**: Logs credential source and creation details

## Default Credentials

**Email**: `nssqos2025@gmail.com`  
**Password**: *Configured via User Secrets or Environment Variables*  
**Role**: SuperAdmin  
**Organization**: National  

## Security Best Practices

### ✅ DO
- Use User Secrets for development
- Use Environment Variables for production
- Change default password immediately after first login
- Use strong passwords (16+ characters, mixed case, numbers, symbols)
- Rotate passwords regularly in production
- Monitor logs for admin account usage

### ❌ DON'T
- Store passwords in appsettings.json
- Commit passwords to source control
- Use default passwords in production
- Share admin credentials
- Log passwords in plain text

## Deployment Checklist

Before deploying to production:

- [ ] Remove any passwords from appsettings.json files
- [ ] Set `DEFAULT_ADMIN_PASSWORD` environment variable
- [ ] Verify other secrets (Clerk keys, connection strings) are properly configured
- [ ] Test admin login with configured credentials
- [ ] Document password for secure storage (password manager)

## Technical Implementation

- **UserSeeder**: Located at `NTC.QoS.Infrastructure/Seed/UserSeeder.cs`
- **Clerk Integration**: Extended `IClerkUserService` with `CreateUserAsync` method
- **Database**: Creates user record with fixed UUID for consistency
- **Seeding Order**: Runs after Organization and Role seeders to ensure dependencies exist
- **Password Generation**: Secure random password generation as fallback

## Troubleshooting

### Common Issues

**Issue**: Default admin user creation fails
**Solutions**:
1. Check that Clerk API credentials are properly configured
2. Ensure database connectivity
3. Verify that Organization and Role seeding completed successfully
4. Check application logs for detailed error messages

**Issue**: Password not recognized
**Solutions**:
1. Verify User Secrets are properly set: `dotnet user-secrets list --project NTC.QoS.API`
2. Check environment variable is set: `echo $DEFAULT_ADMIN_PASSWORD`
3. Look for generated password in application logs (Critical level)

**Issue**: Generated password not visible
**Solutions**:
1. Check application logs at startup (Critical level logging)
2. Ensure logging level allows Critical messages
3. The password is only logged once during user creation

### Log Messages

The seeding process will log detailed information:
- **Information**: Normal seeding progress
- **Warning**: When using generated password instead of configured
- **Critical**: Generated password details (only when auto-generated)
- **Error**: Seeding failures with details

### Password Recovery

If you lose the admin password:
1. Check your User Secrets or environment variables
2. Check application logs for generated password (if applicable)
3. Delete the admin user from database and restart application to regenerate
4. Or manually reset password through Clerk dashboard