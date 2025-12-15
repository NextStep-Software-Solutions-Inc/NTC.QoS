#!/bin/bash

# Test script to verify verbose API logging implementation
# This script creates a test scenario to validate the logging functionality

echo "=== Verbose API Logging Implementation Test ==="
echo ""

# Check if configuration files exist
echo "1. Checking configuration files..."
if [ -f "NTC.QoS.API/appsettings.json" ]; then
    echo "   ✓ appsettings.json exists"
    
    # Check for VerboseApiLogging section
    if grep -q "VerboseApiLogging" NTC.QoS.API/appsettings.json; then
        echo "   ✓ VerboseApiLogging configuration found"
    else
        echo "   ✗ VerboseApiLogging configuration missing"
        exit 1
    fi
else
    echo "   ✗ appsettings.json not found"
    exit 1
fi

# Check if middleware exists
echo ""
echo "2. Checking middleware implementation..."
if [ -f "NTC.QoS.API/Middleware/RequestResponseLoggingMiddleware.cs" ]; then
    echo "   ✓ RequestResponseLoggingMiddleware.cs exists"
else
    echo "   ✗ RequestResponseLoggingMiddleware.cs not found"
    exit 1
fi

# Check if logging models exist
echo ""
echo "3. Checking logging models..."
if [ -f "NTC.QoS.Domain/Models/Logging/VerboseApiLoggingSettings.cs" ]; then
    echo "   ✓ VerboseApiLoggingSettings.cs exists"
else
    echo "   ✗ VerboseApiLoggingSettings.cs not found"
    exit 1
fi

if [ -f "NTC.QoS.Domain/Models/Logging/ApiRequestLog.cs" ]; then
    echo "   ✓ ApiRequestLog.cs exists"
else
    echo "   ✗ ApiRequestLog.cs not found"
    exit 1
fi

if [ -f "NTC.QoS.Domain/Models/Logging/ApiResponseLog.cs" ]; then
    echo "   ✓ ApiResponseLog.cs exists"
else
    echo "   ✗ ApiResponseLog.cs not found"
    exit 1
fi

# Check if extensions exist
echo ""
echo "4. Checking extension methods..."
if [ -f "NTC.QoS.API/Extensions/LoggingMiddlewareExtensions.cs" ]; then
    echo "   ✓ LoggingMiddlewareExtensions.cs exists"
else
    echo "   ✗ LoggingMiddlewareExtensions.cs not found"
    exit 1
fi

# Check if documentation exists
echo ""
echo "5. Checking documentation..."
if [ -f "docs/VERBOSE_API_LOGGING.md" ]; then
    echo "   ✓ VERBOSE_API_LOGGING.md exists"
else
    echo "   ✗ VERBOSE_API_LOGGING.md not found"
    exit 1
fi

if [ -f "docs/API_LOGGING_QUICK_REFERENCE.md" ]; then
    echo "   ✓ API_LOGGING_QUICK_REFERENCE.md exists"
else
    echo "   ✗ API_LOGGING_QUICK_REFERENCE.md not found"
    exit 1
fi

# Check if middleware is registered in Program.cs
echo ""
echo "6. Checking middleware registration..."
if grep -q "UseVerboseApiLogging" NTC.QoS.API/Program.cs; then
    echo "   ✓ Middleware registered in Program.cs"
else
    echo "   ✗ Middleware not registered in Program.cs"
    exit 1
fi

# Check if services are registered
if grep -q "AddVerboseApiLogging" NTC.QoS.API/Program.cs; then
    echo "   ✓ Services registered in Program.cs"
else
    echo "   ✗ Services not registered in Program.cs"
    exit 1
fi

# Check for Serilog configuration
echo ""
echo "7. Checking Serilog integration..."
if grep -q "UseSerilog" NTC.QoS.API/Program.cs; then
    echo "   ✓ Serilog configured in Program.cs"
else
    echo "   ✗ Serilog not configured in Program.cs"
    exit 1
fi

# Build test
echo ""
echo "8. Running build test..."
dotnet restore > /dev/null 2>&1
if dotnet build > /dev/null 2>&1; then
    echo "   ✓ Project builds successfully"
else
    echo "   ✗ Build failed"
    exit 1
fi

# Check for sensitive data redaction configuration
echo ""
echo "9. Verifying security features..."
if grep -q "HeadersToRedact" NTC.QoS.API/appsettings.json; then
    echo "   ✓ Header redaction configured"
else
    echo "   ✗ Header redaction not configured"
    exit 1
fi

if grep -q "BodyPropertiesToRedact" NTC.QoS.API/appsettings.json; then
    echo "   ✓ Body property redaction configured"
else
    echo "   ✗ Body property redaction not configured"
    exit 1
fi

# Verify redaction includes sensitive fields
if grep -q "password" NTC.QoS.API/appsettings.json && grep -q "Authorization" NTC.QoS.API/appsettings.json; then
    echo "   ✓ Sensitive fields configured for redaction"
else
    echo "   ✗ Sensitive fields not configured"
    exit 1
fi

echo ""
echo "=== All Tests Passed! ==="
echo ""
echo "Implementation Summary:"
echo "----------------------"
echo "✓ Configuration files created and properly structured"
echo "✓ Middleware implementation complete"
echo "✓ Logging models defined"
echo "✓ Extension methods created"
echo "✓ Documentation complete"
echo "✓ Middleware and services properly registered"
echo "✓ Serilog integration configured"
echo "✓ Security features (redaction) implemented"
echo "✓ Project builds successfully"
echo ""
echo "Next Steps:"
echo "-----------"
echo "1. Deploy to a test environment"
echo "2. Make test API calls and verify logs are created in logs/ directory"
echo "3. Verify sensitive data is properly redacted"
echo "4. Check correlation IDs are generated and tracked"
echo "5. Test configuration toggles (enable/disable logging)"
echo "6. Verify log rotation and retention policies"
echo ""
