# Web Worker Debug Panel Fix

## Problem

After removing console.log statements to fix performance issues, the `WebWorkerDebugPanel` stopped receiving updates and showed "Inactive" status with zero stats.

## Root Cause

The debug panel was relying on intercepting console.log messages, which were removed to prevent console flooding and rate limiting errors.

## Solution

Implemented a CustomEvent-based communication system between the worker hook and the debug panel.

## Changes Made

### 1. Worker Type Definition (`nap-processor.worker.ts`)

```typescript
export type WorkerResponse = {
    type: "NAPS_PROCESSED";
    geojson: GeoJSONFeatureCollection;
    count: number;
    duration: number; // ✅ Added timing data
};
```

### 2. Worker Processing (`nap-processor.worker.ts`)

```typescript
self.onmessage = (event: MessageEvent<WorkerMessage>) => {
    const { type, naps } = event.data;

    if (type === "PROCESS_NAPS") {
        const startTime = performance.now(); // ✅ Start timing
        
        // ... transform NAPs to GeoJSON ...
        
        const endTime = performance.now();
        const duration = endTime - startTime; // ✅ Calculate duration

        const response: WorkerResponse = {
            type: "NAPS_PROCESSED",
            geojson,
            count: features.length,
            duration, // ✅ Include timing
        };

        self.postMessage(response);
    }
};
```

### 3. Worker Hook Events (`useGeoJSONWorker.tsx`)

#### Event 1: Worker Initialization

```typescript
useEffect(() => {
    workerRef.current = new Worker(/* ... */);

    // ✅ Dispatch init event
    if (typeof window !== "undefined") {
        window.dispatchEvent(
            new CustomEvent("worker-stats", {
                detail: { type: "WORKER_INIT" },
            })
        );
    }
    // ...
}, []);
```

#### Event 2: Processing Start

```typescript
const processNAPs = useCallback((naps: NAP[]) => {
    // ✅ Dispatch process event
    if (typeof window !== "undefined") {
        window.dispatchEvent(
            new CustomEvent("worker-stats", {
                detail: { type: "WORKER_PROCESS", count: naps.length },
            })
        );
    }

    workerRef.current.postMessage({
        type: "PROCESS_NAPS",
        naps,
    });
}, []);
```

#### Event 3: Processing Complete

```typescript
workerRef.current.onmessage = (event: MessageEvent<WorkerResponse>) => {
    const { type, geojson, count } = event.data;

    if (type === "NAPS_PROCESSED") {
        const duration = (event.data as any).duration || 0;
        
        // ✅ Dispatch complete event
        if (typeof window !== "undefined") {
            window.dispatchEvent(
                new CustomEvent("worker-stats", {
                    detail: { type: "WORKER_COMPLETE", count, duration },
                })
            );
        }
        
        callbackRef.current(geojson, count);
    }
};
```

### 4. Debug Panel Listener (`WebWorkerDebugPanel.tsx`)

```typescript
useEffect(() => {
    const handleWorkerEvent = (event: Event) => {
        const customEvent = event as CustomEvent<WorkerEvent>;
        const { type, count, duration } = customEvent.detail;

        setStats((prev) => {
            const newStats = { ...prev };

            switch (type) {
                case "WORKER_INIT":
                    newStats.isActive = true;
                    break;
                case "WORKER_PROCESS":
                    newStats.totalProcessed = (newStats.totalProcessed || 0) + (count || 0);
                    break;
                case "WORKER_COMPLETE":
                    newStats.lastProcessTime = duration || 0;
                    newStats.totalTime = (newStats.totalTime || 0) + (duration || 0);
                    if (newStats.totalTime > 0) {
                        newStats.throughput = newStats.totalProcessed / (newStats.totalTime / 1000);
                    }
                    break;
            }

            return newStats;
        });
    };

    window.addEventListener("worker-stats", handleWorkerEvent);
    return () => window.removeEventListener("worker-stats", handleWorkerEvent);
}, []);
```

## Event Flow

```
1. useGeoJSONWorker initializes
   ↓
   📢 WORKER_INIT event → Debug Panel shows "Active"

2. useMapNAPs fetches page with 250 NAPs
   ↓
   processNAPs(naps) called
   ↓
   📢 WORKER_PROCESS event → Debug Panel increments count

3. Worker transforms data (measures time)
   ↓
   postMessage({ type: "NAPS_PROCESSED", count, duration })
   ↓
   useGeoJSONWorker receives message
   ↓
   📢 WORKER_COMPLETE event → Debug Panel updates timing/throughput
```

## Verification Steps

1. **Check Active Status:**
   - Debug panel should show "● Active" in green

2. **Verify Counts:**
   - Total Processed should increment by 250 for each page

3. **Check Timing:**
   - Last Process Time should show milliseconds for each batch

4. **Verify Throughput:**
   - Should calculate items/second from total processed / total time

5. **Test in Dev Only:**
   - Panel only appears when `NODE_ENV=development`

## Benefits

✅ **No console pollution** - Events don't clutter console  
✅ **Type-safe** - CustomEvent with typed detail property  
✅ **Performance metrics** - Real timing data from worker  
✅ **Reliable** - Direct event system, not log interception  
✅ **Development-only** - Zero overhead in production  

## Files Modified

- `src/workers/nap-processor.worker.ts` - Added timing measurement
- `src/hooks/useNAPGeoJSONWorker.tsx` - Added event dispatching
- `src/components/WebWorkerDebugPanel.tsx` - Switched to event listening

## Testing

```bash
# Start dev server
pnpm dev

# Navigate to map page
# Open browser console
# Look for debug panel in bottom-right corner
# Should show:
#   ● Active
#   Total Processed: 250, 500, 750, ... (incrementing)
#   Last Process Time: ~XX ms
#   Total Time: ~XXX ms
#   Throughput: ~XXXX items/sec
```

## Related Documentation

- `WEB_WORKER_TESTING_GUIDE.md` - Comprehensive testing guide
- `WEB_WORKER_QUICK_CHECK.md` - Quick verification checklist
