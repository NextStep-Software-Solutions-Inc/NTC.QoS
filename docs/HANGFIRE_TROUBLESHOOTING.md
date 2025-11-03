# ?? Hangfire Dashboard Troubleshooting Guide

## Current Configuration Status

? **HangfireDashboardAuthorizationFilter**: Set to allow all access (`return true;`)  
? **Health Endpoints**: Set to `.AllowAnonymous()`  
? **Program.cs**: No duplicate mappings  
? **Job Scheduling**: Configured correctly  

## ?? Why You Still Can't Access Hangfire

The issue is likely **middleware order**. Hangfire dashboard is at `/hangfire`, but ASP.NET's authorization middleware might be intercepting it.

## ? Quick Tests

### Test 1: Verify App is Running
```powershell
curl https://localhost:7199/health/live
```
**Expected**: `Healthy` response  
**If 401**: Health endpoints issue  

### Test 2: Check Swagger
```
https://localhost:7199/swagger
```
**Expected**: Swagger UI loads  
**If blocked**: General authentication issue  

### Test 3: Try HTTP Instead of HTTPS
```
http://localhost:5113/hangfire
```
**Expected**: Hangfire dashboard  
**Why**: SSL certificate issues  

### Test 4: Check Browser Console
1. Open `https://localhost:7199/hangfire`
2. Press `F12` (Developer Tools)
3. Check "Console" and "Network" tabs
4. Look for errors

## ?? Solutions to Try

### Solution 1: Check Your Environment
Are you running in **Development** mode?

Check console output when app starts:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7199
      Environment: Development  ? Should say "Development"
```

If it says **Production**, Hangfire auth filter won't allow access!

**Fix**: Set environment variable:
```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project NTC.QoS.API
```

### Solution 2: Move Hangfire Middleware BEFORE Authentication

This ensures Hangfire processes requests before authentication middleware can block them.

In `Program.cs`, move `app.UseHangfireDashboard()` **BEFORE** `app.UseAuthentication()`:

```csharp
app.UseResponseCompression();
app.UseResponseCaching();

// Move Hangfire BEFORE authentication middleware
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter()]
});

app.UseAuthentication();  // ? Hangfire is now before this
app.UseAuthorization();
```

### Solution 3: Try Different Port/Protocol

**HTTPS (default)**:
```
https://localhost:7199/hangfire
```

**HTTP**:
```
http://localhost:5113/hangfire
```

**With explicit localhost**:
```
https://127.0.0.1:7199/hangfire
```

### Solution 4: Clear Browser Cache

Sometimes browsers cache 401 responses:

1. Open Dev Tools (`F12`)
2. Right-click Refresh button
3. Select "Empty Cache and Hard Reload"

Or use Incognito/Private window:
```
Ctrl + Shift + N (Chrome)
Ctrl + Shift + P (Firefox)
```

### Solution 5: Check if Hangfire Server is Running

Look for this in console output:
```
Hangfire Server started using job storage: 'PostgreSqlStorage'
Using the following options for PostgreSQL job storage: ...
```

If missing, Hangfire might not have started properly.

## ?? Debug Information to Collect

If still not working, check:

### 1. Console Output
Look for:
- `Hangfire Server started`
- Any Hangfire errors
- Port numbers
- Environment name

### 2. Browser Network Tab
1. Open `https://localhost:7199/hangfire`
2. Open Dev Tools ? Network tab
3. Look at the request to `/hangfire`
4. Check:
   - Status code (401? 404? 500?)
   - Response headers
   - Response body

### 3. Application Logs
Any errors related to:
- Hangfire
- Authorization
- Database connection

## ?? What to Share for Help

If none of these work, share:

1. **Console output** when app starts (first 50 lines)
2. **Environment** (Development/Production)
3. **Browser console errors** (F12 ? Console tab)
4. **Network tab** response when accessing /hangfire
5. **Port being used** (from console: "Now listening on...")

## ?? Most Likely Fix

Based on the configuration, the most common issue is:

### **You're running in Production mode instead of Development**

Check your console output. If it says `Environment: Production`, the auth filter won't work!

**Quick fix**:
```powershell
# Set environment to Development
$env:ASPNETCORE_ENVIRONMENT="Development"

# Restart app
dotnet run --project NTC.QoS.API
```

Then try: `https://localhost:7199/hangfire`

---

**Let me know which test fails and what error you see!**
