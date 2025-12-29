#!/bin/bash

# Robust log monitoring for ntc-qos-api
# Handles pod restarts, crashes, and node recycling

LOG_DIR="logs"
DATE=$(date +%F)
NAMESPACE="default"
APP_LABEL="app=ntc-qos-api"
LOG_FILE="${LOG_DIR}/ntc-qos-api-${DATE}.log"
EVENT_FILE="${LOG_DIR}/cluster-events-${DATE}.log"
CRASH_LOG_DIR="${LOG_DIR}/crashes"

mkdir -p "$LOG_DIR" "$CRASH_LOG_DIR"

echo "$(date '+%Y-%m-%d %H:%M:%S') - Starting log monitoring for ntc-qos-api" | tee -a "$LOG_FILE"

# Function to capture crash logs from previous container
capture_crash_logs() {
    local pod=$1
    echo "$(date '+%Y-%m-%d %H:%M:%S') - Detected crash/restart in pod: $pod" | tee -a "$LOG_FILE"
    
    # Try to get previous container logs
    kubectl logs "$pod" -n "$NAMESPACE" --previous --timestamps 2>/dev/null > "${CRASH_LOG_DIR}/${pod}-$(date +%s).log"
    
    # Get pod events
    kubectl describe pod "$pod" -n "$NAMESPACE" >> "${CRASH_LOG_DIR}/${pod}-$(date +%s)-describe.log"
}

# Function to monitor and capture events
capture_events() {
    while true; do
        kubectl get events -n "$NAMESPACE" --sort-by='.lastTimestamp' --watch --output-watch-events >> "$EVENT_FILE" 2>&1 &
        EVENT_PID=$!
        wait $EVENT_PID
        sleep 5
    done
}

# Start event monitoring in background
capture_events &
EVENT_MONITOR_PID=$!

echo "Event monitoring started (PID: $EVENT_MONITOR_PID)"

# Main log streaming loop with auto-reconnect
while true; do
    echo "$(date '+%Y-%m-%d %H:%M:%S') - Connecting to pod logs..." | tee -a "$LOG_FILE"
    
    # Check for crashed/restarted pods before streaming
    CRASHED_PODS=$(kubectl get pods -n "$NAMESPACE" -l "$APP_LABEL" \
        -o jsonpath='{range .items[?(@.status.containerStatuses[0].restartCount>0)]}{.metadata.name}{"\n"}{end}')
    
    if [ -n "$CRASHED_PODS" ]; then
        for pod in $CRASHED_PODS; do
            capture_crash_logs "$pod"
        done
    fi
    
    # Stream logs from all pods with auto-reconnect
    kubectl logs -f -n "$NAMESPACE" -l "$APP_LABEL" \
        --container=ntc-qos-api \
        --timestamps \
        --max-log-requests=10 \
        --prefix 2>&1 | tee -a "$LOG_FILE"
    
    EXIT_CODE=$?
    echo "$(date '+%Y-%m-%d %H:%M:%S') - Log stream disconnected (exit code: $EXIT_CODE). Reconnecting in 5s..." | tee -a "$LOG_FILE"
    
    # Brief pause before reconnecting
    sleep 5
done

# Cleanup on exit (Ctrl+C)
trap "kill $EVENT_MONITOR_PID 2>/dev/null; echo 'Monitoring stopped.'; exit 0" SIGINT SIGTERM
