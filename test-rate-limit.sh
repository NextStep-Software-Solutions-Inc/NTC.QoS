#!/bin/bash

# Rate Limiting Test Script
# Tests the API rate limiting functionality

set -e

# Configuration
API_BASE_URL="${API_BASE_URL:-http://localhost:5113}"
TEST_ENDPOINT="${TEST_ENDPOINT:-public/rate-limit-test}"
FULL_URL="${API_BASE_URL}/${TEST_ENDPOINT}"
REQUESTS_TO_TEST="${REQUESTS_TO_TEST:-50}"

echo "🚀 Rate Limiting Test Script"
echo "============================"
echo ""
echo "📍 API URL: $FULL_URL"
echo "📊 Testing: $REQUESTS_TO_TEST requests"
echo "🎯 Expected: 100 requests allowed per 10-second window, then 429 responses (testing with $REQUESTS_TO_TEST total)"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to make a single request and extract status and headers
make_request() {
    local response
    response=$(curl -s -w "\nHTTP_STATUS:%{http_code}\nX_RATE_LIMIT_LIMIT:%{header:X-Rate-Limit-Limit}\nX_RATE_LIMIT_REMAINING:%{header:X-Rate-Limit-Remaining}\nX_RATE_LIMIT_RESET:%{header:X-Rate-Limit-Reset}\nRETRY_AFTER:%{header:Retry-After}" \
                 "$FULL_URL" 2>/dev/null)

    # Extract status and headers
    local http_status=$(echo "$response" | grep "HTTP_STATUS:" | cut -d: -f2)
    local rate_limit=$(echo "$response" | grep "X_RATE_LIMIT_LIMIT:" | cut -d: -f2)
    local remaining=$(echo "$response" | grep "X_RATE_LIMIT_REMAINING:" | cut -d: -f2)
    local reset=$(echo "$response" | grep "X_RATE_LIMIT_RESET:" | cut -d: -f2)
    local retry_after=$(echo "$response" | grep "RETRY_AFTER:" | cut -d: -f2)

    echo "$http_status|$rate_limit|$remaining|$reset|$retry_after"
}

# Test single request first
echo "1️⃣ Testing single request..."
result=$(make_request)
status=$(echo "$result" | cut -d'|' -f1)
remaining=$(echo "$result" | cut -d'|' -f3)

if [ "$status" = "200" ]; then
    echo -e "${GREEN}✅ API is responding (Status: $status, Remaining: ${remaining:-N/A})${NC}"
else
    echo -e "${RED}❌ API not responding (Status: $status)${NC}"
    echo "Please ensure the API is running on $API_BASE_URL"
    exit 1
fi

echo ""
echo "2️⃣ Testing rate limiting with $REQUESTS_TO_TEST requests in parallel..."

rate_limited_count=0
successful_count=0
total_requests=0

# Function to process a single request result
process_result() {
    local result="$1"
    local i="$2"
    local status=$(echo "$result" | cut -d'|' -f1)
    local remaining=$(echo "$result" | cut -d'|' -f3)

    if [ "$status" = "200" ]; then
        successful_count=$((successful_count + 1))
        if [ $((successful_count % 10)) -eq 0 ] || [ "${remaining:-0}" -le 5 ] 2>/dev/null; then
            echo -e "${GREEN}✅ Request $i: Status $status, Remaining: ${remaining:-N/A}${NC}"
        fi
    elif [ "$status" = "429" ]; then
        rate_limited_count=$((rate_limited_count + 1))
        if [ $rate_limited_count -le 5 ]; then
            echo -e "${RED}❌ Request $i: RATE LIMITED (Status: $status)${NC}"
        fi
    else
        echo -e "${YELLOW}⚠️  Request $i: Unexpected status $status${NC}"
    fi
}

# Start timing
echo "🚀 Launching $REQUESTS_TO_TEST requests simultaneously..."
start_time=$(date +%s%3N 2>/dev/null || date +%s000)


# Create temporary file for results
temp_file=$(mktemp)

# Launch all requests in parallel
pids=()
for i in $(seq 1 "$REQUESTS_TO_TEST"); do
    (
        result=$(make_request)
        echo "$result|$i" >> "$temp_file"
    ) &
    pids+=($!)
    
    # Small delay every 20 requests to avoid overwhelming the shell
    if [ $((i % 20)) -eq 0 ]; then
        sleep 0.001
    fi
done

# Wait for all requests to complete
for pid in "${pids[@]}"; do
    wait "$pid" 2>/dev/null || true
done

end_time=$(date +%s%3N 2>/dev/null || date +%s000)
duration_ms=$((end_time - start_time))
duration=$(echo "scale=3; $duration_ms / 1000" | bc 2>/dev/null || echo "$duration_ms")

echo -e "${BLUE}⏱️  All requests completed in ${duration}s${NC}"

# Process results
while IFS= read -r line; do
    if [ -n "$line" ]; then
        result=$(echo "$line" | cut -d'|' -f1-5)
        i=$(echo "$line" | cut -d'|' -f6)
        process_result "$result" "$i"
        total_requests=$((total_requests + 1))
    fi
done < "$temp_file"

# Clean up
rm -f "$temp_file"

echo ""
echo "📊 Test Results:"
echo "=================="
echo "✅ Successful requests: $successful_count"
echo "❌ Rate limited requests: $rate_limited_count"
echo "📈 Total requests processed: $total_requests"
echo "⏱️  Execution time: ${duration}s"

if [ "$successful_count" -ge 95 ] && [ "$rate_limited_count" -ge 1 ]; then
    echo -e "${GREEN}🎉 Rate limiting is working correctly!${NC}"
    echo "   • Allowed ~100 requests in the 10-second window"
    echo "   • Blocked excess requests with 429 status"
elif [ "$total_requests" -eq 0 ]; then
    echo -e "${YELLOW}⚠️  No requests were processed - check API connectivity${NC}"
else
    echo -e "${RED}⚠️  Rate limiting may not be working as expected${NC}"
    echo "   • Expected ~100 successful requests and some 429s in 10-second window"
    echo "   • Got $successful_count successful, $rate_limited_count limited"
fi

echo ""
echo "💡 Tips:"
echo "========"
echo "• Rate limiting is per user (authenticated) or per IP (anonymous)"
echo "• Limits reset every 10 seconds automatically"
echo "• Check X-Rate-Limit-* headers for current status"
echo "• 429 responses include Retry-After header"