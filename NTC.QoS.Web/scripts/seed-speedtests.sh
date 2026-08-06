#!/bin/bash

# =============================================================================
# Speedtest Database Seeder Script
# =============================================================================
# This script seeds the database with random speedtest data for all wireless
# service providers. It respects the API rate limit of 100 requests / 10 seconds.
#
# Usage:
#   ./scripts/seed-speedtests.sh [OPTIONS]
#
# Options:
#   -n, --count      Total number of speedtests to create (default: 1000)
#   -u, --url        API base URL (default: http://localhost:5000)
#   -t, --token      Authorization token (required for authenticated APIs)
#   -b, --batch      Batch size per rate limit window (default: 100)
#   -w, --wait       Wait time in seconds between batches (default: 10)
#   -p, --parallel   Number of parallel requests (default: 10)
#   -h, --help       Show this help message
#
# Examples:
#   ./scripts/seed-speedtests.sh -n 10000 -u http://localhost:5000
#   ./scripts/seed-speedtests.sh -n 1000000 -t "your-jwt-token" -p 20
#
# =============================================================================

set -e

# Default configuration
TOTAL_COUNT=1000
API_BASE_URL="${NEXT_PUBLIC_QOS_BACKEND_URL:-http://localhost:5000}"
AUTH_TOKEN=""
BATCH_SIZE=100
WAIT_TIME=10
PARALLEL_REQUESTS=10
LOG_FILE="seed-speedtests-$(date +%Y%m%d_%H%M%S).log"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Philippine wireless service providers (to be fetched from API or use defaults)
declare -a SERVICE_PROVIDER_IDS=()
declare -a TECHNOLOGY_IDS=()
declare -a GENERATION_IDS=()

# Philippine geographic bounds
MIN_LAT=4.5
MAX_LAT=21.2
MIN_LNG=116.0
MAX_LNG=127.0

# Phone models for random selection
PHONE_MODELS=(
    "Samsung Galaxy S24 Ultra"
    "Samsung Galaxy S23"
    "Samsung Galaxy A54"
    "iPhone 15 Pro Max"
    "iPhone 15"
    "iPhone 14"
    "iPhone 13"
    "Xiaomi 14"
    "Xiaomi Redmi Note 13"
    "OPPO Reno 11"
    "OPPO A98"
    "Vivo V30"
    "Vivo Y36"
    "Realme 12 Pro+"
    "Realme C55"
    "Huawei P60"
    "Google Pixel 8"
    "OnePlus 12"
    "Nothing Phone 2"
    "Infinix Note 30"
)

# Signal quality levels
SIGNAL_QUALITIES=("Excellent" "Good" "Average" "Weak" "VeryWeak" "Unknown")

# Show help message
show_help() {
    head -35 "$0" | tail -30
    exit 0
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -n|--count)
            TOTAL_COUNT="$2"
            shift 2
            ;;
        -u|--url)
            API_BASE_URL="$2"
            shift 2
            ;;
        -t|--token)
            AUTH_TOKEN="$2"
            shift 2
            ;;
        -b|--batch)
            BATCH_SIZE="$2"
            shift 2
            ;;
        -w|--wait)
            WAIT_TIME="$2"
            shift 2
            ;;
        -p|--parallel)
            PARALLEL_REQUESTS="$2"
            shift 2
            ;;
        -h|--help)
            show_help
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            show_help
            ;;
    esac
done

# Log function
log() {
    local level=$1
    shift
    local message="$@"
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    echo -e "[$timestamp] [$level] $message" | tee -a "$LOG_FILE"
}

log_info() { log "${BLUE}INFO${NC}" "$@"; }
log_success() { log "${GREEN}SUCCESS${NC}" "$@"; }
log_warning() { log "${YELLOW}WARNING${NC}" "$@"; }
log_error() { log "${RED}ERROR${NC}" "$@"; }

# Progress bar function
show_progress() {
    local current=$1
    local total=$2
    local width=50
    local percentage=$((current * 100 / total))
    local filled=$((current * width / total))
    local empty=$((width - filled))
    
    printf "\r${CYAN}Progress: [${GREEN}"
    printf "%${filled}s" | tr ' ' '█'
    printf "${NC}"
    printf "%${empty}s" | tr ' ' '░'
    printf "${CYAN}] ${percentage}%% (${current}/${total})${NC}"
}

# Generate random float in range
random_float() {
    local min=$1
    local max=$2
    local precision=${3:-2}
    awk -v min="$min" -v max="$max" -v prec="$precision" 'BEGIN{srand(); printf "%.*f", prec, min + rand() * (max - min)}'
}

# Generate random integer in range
random_int() {
    local min=$1
    local max=$2
    echo $((RANDOM % (max - min + 1) + min))
}

# Generate random element from array
random_element() {
    local arr=("$@")
    local len=${#arr[@]}
    echo "${arr[$((RANDOM % len))]}"
}

# Generate random date within last 2 years
random_date() {
    local days_ago=$((RANDOM % 730))
    local hours=$((RANDOM % 24))
    local minutes=$((RANDOM % 60))
    local seconds=$((RANDOM % 60))
    
    if [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        date -v-${days_ago}d -v${hours}H -v${minutes}M -v${seconds}S '+%Y-%m-%dT%H:%M:%S.000Z'
    else
        # Linux
        date -d "-${days_ago} days ${hours} hours ${minutes} minutes ${seconds} seconds" '+%Y-%m-%dT%H:%M:%S.000Z'
    fi
}

# Generate MCC (Mobile Country Code) for Philippines
generate_mcc() {
    echo "515"
}

# Generate MNC (Mobile Network Code) for Philippine carriers
generate_mnc() {
    # Philippine MNCs: 01=Islacom, 02=Globe, 03=Smart, 05=Sun, 18=Cure, 24=ABS-CBN
    local mncs=("02" "03" "05" "18")
    random_element "${mncs[@]}"
}

# Generate TAC (Tracking Area Code)
generate_tac() {
    printf "%05d" $((RANDOM % 65535))
}

# Generate Cell Identifier
generate_cell_id() {
    printf "%08X" $((RANDOM % 268435455))
}

# Calculate signal quality based on RSSI
calculate_signal_quality() {
    local rssi=$1
    if (( rssi >= -65 )); then
        echo "Excellent"
    elif (( rssi >= -75 )); then
        echo "Good"
    elif (( rssi >= -85 )); then
        echo "Average"
    elif (( rssi >= -95 )); then
        echo "Weak"
    elif (( rssi >= -110 )); then
        echo "VeryWeak"
    else
        echo "Unknown"
    fi
}

# Fetch service providers from API
fetch_service_providers() {
    log_info "Fetching wireless service providers from API..."
    
    local headers=""
    if [[ -n "$AUTH_TOKEN" ]]; then
        headers="-H \"Authorization: Bearer $AUTH_TOKEN\""
    fi
    
    local response
    response=$(curl -s -X GET \
        -H "Accept: application/json" \
        -H "Content-Type: application/json" \
        ${AUTH_TOKEN:+-H "Authorization: Bearer $AUTH_TOKEN"} \
        "${API_BASE_URL}/service-providers?pageSize=1000&serviceTypes=wireless" 2>/dev/null)
    
    if [[ $? -ne 0 ]] || [[ -z "$response" ]]; then
        log_warning "Could not fetch service providers from API. Using default IDs."
        return 1
    fi
    
    # Parse the response and extract IDs (assuming JSON response with data array)
    local ids
    ids=$(echo "$response" | jq -r '.data[].id' 2>/dev/null)
    
    if [[ -z "$ids" ]]; then
        log_warning "No service providers found in response. Using default IDs."
        return 1
    fi
    
    while IFS= read -r id; do
        SERVICE_PROVIDER_IDS+=("$id")
    done <<< "$ids"
    
    log_success "Found ${#SERVICE_PROVIDER_IDS[@]} wireless service providers"
    return 0
}

# Fetch technologies from API
fetch_technologies() {
    log_info "Fetching technologies from API..."
    
    local response
    response=$(curl -s -X GET \
        -H "Accept: application/json" \
        -H "Content-Type: application/json" \
        ${AUTH_TOKEN:+-H "Authorization: Bearer $AUTH_TOKEN"} \
        "${API_BASE_URL}/technologies?pageSize=1000" 2>/dev/null)
    
    if [[ $? -ne 0 ]] || [[ -z "$response" ]]; then
        log_warning "Could not fetch technologies from API."
        return 1
    fi
    
    local ids
    ids=$(echo "$response" | jq -r '.data[].id' 2>/dev/null)
    
    if [[ -z "$ids" ]]; then
        log_warning "No technologies found in response."
        return 1
    fi
    
    while IFS= read -r id; do
        TECHNOLOGY_IDS+=("$id")
    done <<< "$ids"
    
    log_success "Found ${#TECHNOLOGY_IDS[@]} technologies"
    return 0
}

# Fetch generations from API
fetch_generations() {
    log_info "Fetching generations from API..."
    
    local response
    response=$(curl -s -X GET \
        -H "Accept: application/json" \
        -H "Content-Type: application/json" \
        ${AUTH_TOKEN:+-H "Authorization: Bearer $AUTH_TOKEN"} \
        "${API_BASE_URL}/generations?pageSize=1000" 2>/dev/null)
    
    if [[ $? -ne 0 ]] || [[ -z "$response" ]]; then
        log_warning "Could not fetch generations from API."
        return 1
    fi
    
    local ids
    ids=$(echo "$response" | jq -r '.data[].id' 2>/dev/null)
    
    if [[ -z "$ids" ]]; then
        log_warning "No generations found in response."
        return 1
    fi
    
    while IFS= read -r id; do
        GENERATION_IDS+=("$id")
    done <<< "$ids"
    
    log_success "Found ${#GENERATION_IDS[@]} generations"
    return 0
}

# Generate a single speedtest payload
generate_speedtest_payload() {
    local lat=$(random_float $MIN_LAT $MAX_LAT 6)
    local lng=$(random_float $MIN_LNG $MAX_LNG 6)
    local rssi=$(random_int -110 -50)
    local signal_quality=$(calculate_signal_quality $rssi)
    
    # Speed varies by signal quality
    local max_download max_upload max_ping
    case $signal_quality in
        "Excellent")
            max_download=500
            max_upload=100
            max_ping=20
            ;;
        "Good")
            max_download=200
            max_upload=50
            max_ping=50
            ;;
        "Average")
            max_download=50
            max_upload=20
            max_ping=100
            ;;
        "Weak")
            max_download=10
            max_upload=5
            max_ping=200
            ;;
        *)
            max_download=5
            max_upload=2
            max_ping=500
            ;;
    esac
    
    local download_speed=$(random_float 0.5 $max_download 2)
    local upload_speed=$(random_float 0.1 $max_upload 2)
    local ping=$(random_float 5 $max_ping 2)
    local jitter=$(random_float 0.5 50 2)
    
    local service_provider_id=""
    if [[ ${#SERVICE_PROVIDER_IDS[@]} -gt 0 ]]; then
        service_provider_id=$(random_element "${SERVICE_PROVIDER_IDS[@]}")
    fi
    
    local technology_id=""
    if [[ ${#TECHNOLOGY_IDS[@]} -gt 0 ]]; then
        technology_id=$(random_element "${TECHNOLOGY_IDS[@]}")
    fi
    
    local generation_id=""
    if [[ ${#GENERATION_IDS[@]} -gt 0 ]]; then
        generation_id=$(random_element "${GENERATION_IDS[@]}")
    fi
    
    local phone_model=$(random_element "${PHONE_MODELS[@]}")
    local date_time=$(random_date)
    local mcc=$(generate_mcc)
    local mnc=$(generate_mnc)
    local tac=$(generate_tac)
    local cell_id=$(generate_cell_id)
    
    # Build JSON payload
    cat << EOF
{
    "cellIdentifier": "$cell_id",
    "latitude": $lat,
    "longitude": $lng,
    "mcc": "$mcc",
    "mnc": "$mnc",
    "phoneModel": "$phone_model",
    "tac": "$tac",
    "dateTime": "$date_time",
    "ping": $ping,
    "jitter": $jitter,
    "rssi": $rssi,
    "uploadSpeed": $upload_speed,
    "downloadSpeed": $download_speed,
    "signalQuality": "$signal_quality",
    "isFixedBroadband": false${service_provider_id:+,
    "serviceProviderId": "$service_provider_id"}${technology_id:+,
    "technologyId": "$technology_id"}${generation_id:+,
    "generationId": "$generation_id"}
}
EOF
}

# Create a single speedtest via API
create_speedtest() {
    local payload=$1
    local index=$2
    
    local response
    local http_code
    
    # Make the API request
    response=$(curl -s -w "\n%{http_code}" -X POST \
        -H "Accept: application/json" \
        -H "Content-Type: application/json" \
        ${AUTH_TOKEN:+-H "Authorization: Bearer $AUTH_TOKEN"} \
        -d "$payload" \
        "${API_BASE_URL}/speedtests" 2>/dev/null)
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [[ "$http_code" == "200" ]] || [[ "$http_code" == "201" ]]; then
        return 0
    else
        echo "Failed [$http_code]: $body" >> "$LOG_FILE"
        return 1
    fi
}

# Process batch of requests with parallel execution
process_batch() {
    local start_index=$1
    local batch_size=$2
    local total=$3
    
    local end_index=$((start_index + batch_size))
    if [[ $end_index -gt $total ]]; then
        end_index=$total
    fi
    
    local pids=()
    local success_count=0
    local fail_count=0
    
    for ((i=start_index; i<end_index; i++)); do
        # Generate payload
        local payload=$(generate_speedtest_payload)
        
        # Run in background with limited parallelism
        while [[ ${#pids[@]} -ge $PARALLEL_REQUESTS ]]; do
            # Wait for any process to finish
            for pid_idx in "${!pids[@]}"; do
                if ! kill -0 "${pids[$pid_idx]}" 2>/dev/null; then
                    wait "${pids[$pid_idx]}" && ((success_count++)) || ((fail_count++))
                    unset 'pids[pid_idx]'
                fi
            done
            # Rebuild array to remove gaps
            pids=("${pids[@]}")
            sleep 0.05
        done
        
        # Start new request in background
        (create_speedtest "$payload" $i) &
        pids+=($!)
        
        show_progress $((i + 1)) $total
    done
    
    # Wait for remaining processes
    for pid in "${pids[@]}"; do
        wait "$pid" && ((success_count++)) || ((fail_count++))
    done
    
    echo ""
    return $fail_count
}

# Main execution
main() {
    echo ""
    echo -e "${CYAN}╔══════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${CYAN}║        ${GREEN}Speedtest Database Seeder${CYAN}                             ║${NC}"
    echo -e "${CYAN}║        ${YELLOW}Rate Limit: 100 req / 10 sec${CYAN}                         ║${NC}"
    echo -e "${CYAN}╚══════════════════════════════════════════════════════════════╝${NC}"
    echo ""
    
    log_info "Configuration:"
    log_info "  API URL: $API_BASE_URL"
    log_info "  Total records: $TOTAL_COUNT"
    log_info "  Batch size: $BATCH_SIZE"
    log_info "  Wait time: ${WAIT_TIME}s"
    log_info "  Parallel requests: $PARALLEL_REQUESTS"
    log_info "  Log file: $LOG_FILE"
    echo ""
    
    # Check dependencies
    if ! command -v curl &> /dev/null; then
        log_error "curl is required but not installed."
        exit 1
    fi
    
    if ! command -v jq &> /dev/null; then
        log_warning "jq is not installed. API responses won't be parsed for IDs."
    fi
    
    # Test API connection
    log_info "Testing API connection..."
    local test_response
    test_response=$(curl -s -o /dev/null -w "%{http_code}" \
        -H "Accept: application/json" \
        ${AUTH_TOKEN:+-H "Authorization: Bearer $AUTH_TOKEN"} \
        "${API_BASE_URL}/service-providers?pageSize=1" 2>/dev/null)
    
    if [[ "$test_response" != "200" ]]; then
        log_error "Cannot connect to API at $API_BASE_URL (HTTP $test_response)"
        log_error "Please check the API URL and authentication token."
        exit 1
    fi
    log_success "API connection successful"
    
    # Fetch reference data
    fetch_service_providers || true
    fetch_technologies || true
    fetch_generations || true
    
    if [[ ${#SERVICE_PROVIDER_IDS[@]} -eq 0 ]]; then
        log_warning "No service providers available. Speedtests will be created without provider IDs."
    fi
    
    if [[ ${#TECHNOLOGY_IDS[@]} -eq 0 ]]; then
        log_error "No technologies available. Technology ID is required for speedtests."
        log_error "Please create technologies first or check the API."
        exit 1
    fi
    
    echo ""
    log_info "Starting to seed $TOTAL_COUNT speedtest records..."
    echo ""
    
    local start_time=$(date +%s)
    local total_batches=$(( (TOTAL_COUNT + BATCH_SIZE - 1) / BATCH_SIZE ))
    local current_batch=0
    local total_success=0
    local total_fail=0
    
    for ((i=0; i<TOTAL_COUNT; i+=BATCH_SIZE)); do
        ((current_batch++))
        
        local batch_start=$i
        local batch_count=$BATCH_SIZE
        if [[ $((i + BATCH_SIZE)) -gt $TOTAL_COUNT ]]; then
            batch_count=$((TOTAL_COUNT - i))
        fi
        
        log_info "Processing batch $current_batch/$total_batches ($batch_count records)..."
        
        process_batch $batch_start $batch_count $TOTAL_COUNT
        local batch_fail=$?
        local batch_success=$((batch_count - batch_fail))
        
        ((total_success += batch_success))
        ((total_fail += batch_fail))
        
        # Rate limit: wait between batches (except for last batch)
        if [[ $((i + BATCH_SIZE)) -lt $TOTAL_COUNT ]]; then
            log_info "Rate limiting: waiting ${WAIT_TIME}s before next batch..."
            sleep $WAIT_TIME
        fi
    done
    
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    local hours=$((duration / 3600))
    local minutes=$(( (duration % 3600) / 60 ))
    local seconds=$((duration % 60))
    
    echo ""
    echo -e "${CYAN}╔══════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${CYAN}║                     ${GREEN}Seeding Complete!${CYAN}                        ║${NC}"
    echo -e "${CYAN}╚══════════════════════════════════════════════════════════════╝${NC}"
    echo ""
    log_success "Total records created: $total_success"
    if [[ $total_fail -gt 0 ]]; then
        log_warning "Failed records: $total_fail"
    fi
    log_info "Time elapsed: ${hours}h ${minutes}m ${seconds}s"
    log_info "Average rate: $(echo "scale=2; $total_success / $duration" | bc) records/second"
    log_info "Full log saved to: $LOG_FILE"
    echo ""
}

# Run main function
main
