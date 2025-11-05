#!/bin/bash

# Quick test script to verify viewport API parameter names are working

echo "Testing Viewport API with new parameter names..."
echo "================================================"

# Test NAPs endpoint
echo -e "\n1. Testing NAPs Viewport API:"
response=$(curl -s "http://localhost:5113/v2/NAPs/viewport/public?pageNumber=1&pageSize=5&minLat=14.55&maxLat=14.65&minLng=120.95&maxLng=121.05")

if [[ "$response" == *'"succeeded": true'* ]]; then
    echo "   ✅ NAPs endpoint PASSED"
    count=$(echo "$response" | grep -o '"data":\[' | wc -l)
    echo "   📊 Response has data array"
else
    echo "   ❌ NAPs endpoint FAILED"
    echo "   Response: $response" | head -3
fi

# Test Cells endpoint
echo -e "\n2. Testing Cells Viewport API:"
response=$(curl -s "http://localhost:5113/v2/cells/viewport/public?pageNumber=1&pageSize=5&minLat=14.55&maxLat=14.65&minLng=120.95&maxLng=121.05")

if [[ "$response" == *'"succeeded": true'* ]]; then
    echo "   ✅ Cells endpoint PASSED"
else
    echo "   ❌ Cells endpoint FAILED"
    echo "   Response: $response" | head -3
fi

# Test Speedtests endpoint
echo -e "\n3. Testing Speedtests Viewport API:"
response=$(curl -s "http://localhost:5113/v2/speedtests/viewport/public?pageNumber=1&pageSize=5&minLat=14.55&maxLat=14.65&minLng=120.95&maxLng=121.05")

if [[ "$response" == *'"succeeded": true'* ]]; then
    echo "   ✅ Speedtests endpoint PASSED"
else
    echo "   ❌ Speedtests endpoint FAILED"
    echo "   Response: $response" | head -3
fi

# Test Cellular Coverages endpoint
echo -e "\n4. Testing Cellular Coverages Viewport API:"
response=$(curl -s "http://localhost:5113/v2/cellular-coverages/viewport/public?pageNumber=1&pageSize=5&minLat=14.55&maxLat=14.65&minLng=120.95&maxLng=121.05")

if [[ "$response" == *'"succeeded": true'* ]]; then
    echo "   ✅ Cellular Coverages endpoint PASSED"
else
    echo "   ❌ Cellular Coverages endpoint FAILED"
    echo "   Response: $response" | head -3
fi

echo -e "\n================================================"
echo "Quick test completed!"
echo "If all tests passed, the parameter name changes are working correctly."
