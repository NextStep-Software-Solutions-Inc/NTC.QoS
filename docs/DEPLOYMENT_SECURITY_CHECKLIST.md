# Deployment Security Checklist

This checklist ensures that sensitive credentials are properly handled during deployment and not exposed in configuration files.

## Pre-Deployment Security Checklist

### ? Configuration Files Review

- [ ] **appsettings.json**: No passwords, API keys, or connection strings
- [ ] **appsettings.Production.json**: No sensitive data
- [ ] **appsettings.Development.json**: No sensitive data (if exists)
- [ ] **User Secrets**: Used only for local development
- [ ] **Docker files**: No hardcoded secrets
- [ ] **CI/CD configs**: No exposed credentials

### ? Environment Variables Setup

- [ ] `DEFAULT_ADMIN_PASSWORD`: Set in deployment environment
- [ ] `Clerk__SecretKey`: Set in deployment environment
- [ ] Connection strings: Set in deployment environment
- [ ] Other API keys: Set in deployment environment

### ? Source Code Review

- [ ] No hardcoded passwords in source code
- [ ] No API keys in source code
- [ ] No connection strings in source code
- [ ] No commented-out credentials
- [ ] Git history clean of sensitive data

### ? Secrets Management

- [ ] Production secrets stored in secure vault (Azure Key Vault, AWS Secrets Manager, etc.)
- [ ] Environment-specific configuration properly segregated
- [ ] Secrets rotation plan in place
- [ ] Access to secrets properly controlled

## Deployment Environment Setup

### Azure App Service
```bash
# Set application settings (environment variables)
az webapp config appsettings set --resource-group myResourceGroup --name myWebApp --settings \
  DEFAULT_ADMIN_PASSWORD="SecurePassword123!" \
  Clerk__SecretKey="sk_live_..." \
  ConnectionStrings__PostgreSqlConnection="Host=...;Password=..."
```

### Docker Deployment
```yaml
# docker-compose.yml
version: '3.8'
services:
  api:
    image: ntc-qos-api
    environment:
      - DEFAULT_ADMIN_PASSWORD=${DEFAULT_ADMIN_PASSWORD}
      - Clerk__SecretKey=${CLERK_SECRET_KEY}
      - ConnectionStrings__PostgreSqlConnection=${DATABASE_CONNECTION}
    env_file:
      - .env.production  # Never commit this file
```

### Kubernetes
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: ntc-qos-secrets
type: Opaque
data:
  admin-password: <base64-encoded-password>
  clerk-secret: <base64-encoded-clerk-key>
  db-connection: <base64-encoded-connection-string>
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ntc-qos-api
spec:
  template:
    spec:
      containers:
      - name: api
        image: ntc-qos-api
        env:
        - name: DEFAULT_ADMIN_PASSWORD
          valueFrom:
            secretKeyRef:
              name: ntc-qos-secrets
              key: admin-password
```

## File Scanning Tools

Use these tools to scan for exposed secrets before deployment:

### Git-secrets
```bash
# Install git-secrets
git secrets --install
git secrets --register-aws

# Scan repository
git secrets --scan
git secrets --scan-history
```

### TruffleHog
```bash
# Install and run TruffleHog
pip install truffleHog
trufflehog --regex --entropy=False .
```

### GitGuardian CLI
```bash
# Install GitGuardian CLI
pip install detect-secrets
detect-secrets scan --all-files
```

## Common Credential Patterns to Avoid

### ? Bad Examples

```json
// appsettings.json - DON'T DO THIS
{
  "DefaultAdmin": {
    "Password": "MyPassword123!"  // ? Exposed in source code
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod;Password=secret;"  // ? Exposed
  },
  "Clerk": {
    "SecretKey": "sk_live_abc123..."  // ? Exposed
  }
}
```

```csharp
// Code - DON'T DO THIS
public class MyService 
{
    private readonly string _apiKey = "sk_live_abc123..."; // ? Hardcoded
    private readonly string _password = "MyPassword123!"; // ? Hardcoded
}
```

### ? Good Examples

```json
// appsettings.json - CORRECT
{
  "DefaultAdmin": {
    "Email": "admin@ntc.gov.ph",
    "FirstName": "System",
    "LastName": "Administrator"
    // Password configured via environment variables
  }
}
```

```csharp
// Code - CORRECT
public class MyService 
{
    private readonly string _apiKey;
    
    public MyService(IConfiguration configuration)
    {
        _apiKey = configuration["Clerk:SecretKey"] ?? 
                 throw new InvalidOperationException("API key not configured");
    }
}
```

## Post-Deployment Verification

### ? Verify Deployment

- [ ] Application starts successfully
- [ ] Admin user can log in with configured password
- [ ] Database connections work
- [ ] Clerk authentication works
- [ ] All features function properly
- [ ] No sensitive data visible in logs
- [ ] No sensitive data in error messages

### ? Security Verification

- [ ] Configuration endpoints don't expose secrets
- [ ] Error pages don't reveal sensitive information
- [ ] Logs don't contain passwords or keys
- [ ] Debug mode disabled in production
- [ ] HTTPS enforced for all endpoints

## Incident Response

If credentials are accidentally exposed:

### Immediate Actions
1. **Rotate all exposed credentials immediately**
2. **Remove credentials from source control history** (if committed)
3. **Update deployment with new credentials**
4. **Audit access logs** for unauthorized usage
5. **Document the incident** for future prevention

### Tools for Cleanup
```bash
# Remove from Git history (use carefully)
git filter-branch --force --index-filter \
  'git rm --cached --ignore-unmatch path/to/file' \
  --prune-empty --tag-name-filter cat -- --all

# Or use BFG Repo-Cleaner
java -jar bfg.jar --delete-files appsettings.production.json
```

## Monitoring and Alerts

Set up monitoring for:
- Failed authentication attempts
- Unusual admin account activity  
- Configuration changes
- Environment variable access
- Secret rotation reminders

This checklist should be part of your deployment pipeline to ensure no credentials are accidentally exposed.