# Rate Limiting Test

This script tests the API rate limiting functionality that limits requests to 100 per 10 seconds per user/IP.

## Usage

### Basic Test

```bash
./test-rate-limit.sh
```

### Custom Configuration

```bash
# Test different API endpoint
API_BASE_URL="https://your-api.com" TEST_ENDPOINT="api/test" ./test-rate-limit.sh

# Test with fewer requests
REQUESTS_TO_TEST=50 ./test-rate-limit.sh
```

## What It Tests

1. **API Connectivity**: Verifies the API is responding
2. **Rate Limiting**: By default, makes 50 requests to trigger rate limiting (configurable via REQUESTS_TO_TEST)
3. **Response Codes**: Checks for 200 (allowed) and 429 (rate limited) responses
4. **Headers**: Validates X-Rate-Limit-* headers are present

## Expected Results

- **First 100 requests**: HTTP 200 with decreasing X-Rate-Limit-Remaining header
- **101st+ requests**: HTTP 429 with X-Rate-Limit-Remaining: 0
- **Headers**: X-Rate-Limit-Limit, X-Rate-Limit-Remaining, X-Rate-Limit-Reset

## API Endpoint

The test uses: `GET /public/rate-limit-test`

This endpoint returns:

```json
{
  "message": "Rate limiting test endpoint",
  "timestamp": "2025-10-29T12:00:00.0000000Z",
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "server": "YOUR_MACHINE_NAME"
}
```
