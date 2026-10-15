# AbortController Support in Mutation Hooks

This document explains how to use the AbortController signal support in mutation hooks to cancel ongoing requests.

## Overview

All mutation hooks created with `createMutationHookFromTemplate` now automatically support request cancellation via AbortController. The signal is automatically created and passed to the API handler for each request.

## How It Works

### 1. Type Updates

The `MutationBlueprintFromTemplate` type now includes an optional `signal` parameter in the fetcher:

```typescript
export type MutationBlueprintFromTemplate<...> = {
  endpoint: TEndpoint;
  fetcher: ({ 
    finalUrl, 
    payload, 
    signal // <-- AbortSignal is now available
  }: { 
    finalUrl: string; 
    payload?: TPayload; 
    signal?: AbortSignal 
  }) => Promise<TResponse>;
  // ...
};
```

### 2. Mutation Hook Enhancement

The `createMutationHookFromTemplate` function now:

- Creates a new `AbortController` for each mutation request
- Passes the `signal` to the fetcher
- Exposes an `abort()` method on the mutation result
- Automatically cleans up the controller when the request completes

### 3. Handler Updates

All mutation handlers (POST, PUT, PATCH, DELETE) have been updated to accept and use the signal:

```typescript
export const getAreaBroadbandInfoRequest: MutationBlueprintFromTemplate<...> = {
  endpoint,
  fetcher: async ({ finalUrl, payload, signal }) => {
    return await request.post<APIResponse<BroadbandInfoData>>(
      finalUrl, 
      payload, 
      signal ? { signal } : undefined // Signal is passed to the request
    );
  },
};
```

## Usage Example

### Basic Usage

```typescript
const MyComponent = () => {
  // The mutation hook now returns an abort() method
  const { mutate, isPending, abort, reset } = useAreaBroadbandInfo();

  const handleCancel = () => {
    // Abort the ongoing request
    abort();
    
    // Reset the mutation state
    reset();
  };

  const handleAnalyze = () => {
    mutate({
      pathParams: {},
      payload: {
        latitude: 14.5995,
        longitude: 120.9842,
        radius: 500
      }
    });
  };

  return (
    <div>
      <button onClick={handleAnalyze} disabled={isPending}>
        Analyze
      </button>
      {isPending && (
        <button onClick={handleCancel}>
          Cancel Request
        </button>
      )}
    </div>
  );
};
```

### Real-World Example (AdminBroadbandPanel)

```typescript
const PointAnalysisTab = ({ selectedPoint }) => {
  const { mutate, data, isPending, reset, abort } = usePointBroadbandInfo();

  const handleCancelRequest = useCallback(() => {
    // Abort the network request
    abort();
    
    // Reset mutation state
    reset();
    
    // Reset local UI state
    setHasAnalyzed(false);
  }, [abort, reset]);

  return (
    <div>
      {isPending && (
        <Button 
          onClick={handleCancelRequest}
          variant="outline"
          size="sm"
        >
          <X className="mr-2 h-4 w-4" />
          Cancel
        </Button>
      )}
    </div>
  );
};
```

## Benefits

1. **Request Cancellation**: Properly abort ongoing HTTP requests when they're no longer needed
2. **Resource Cleanup**: Prevent memory leaks and unnecessary network usage
3. **Better UX**: Allow users to cancel slow or unnecessary operations
4. **Automatic Handling**: Signal creation and cleanup are handled automatically

## Technical Details

### Signal Flow

1. User triggers mutation via `mutate()`
2. `createMutationHookFromTemplate` creates new `AbortController`
3. Signal is extracted and passed to the fetcher
4. Fetcher passes signal to the `request` service methods
5. Signal is included in the `fetch()` init options
6. If `abort()` is called, the request is cancelled
7. Controller is cleaned up on request completion or error

### Error Handling

When a request is aborted, it will throw an `AbortError`:

```typescript
try {
  await mutate({ ... });
} catch (error) {
  if (error.name === 'AbortError') {
    console.log('Request was cancelled');
  }
}
```

## Migration Notes

- All existing mutation handlers have been updated to support signals
- No breaking changes - signal support is optional
- The `abort()` method is always available on mutation results
- Calling `abort()` when no request is pending is safe (no-op)

## Files Modified

- `src/types/reusable/api.ts` - Updated `MutationBlueprintFromTemplate` type
- `src/hooks/createMutationHookFromTemplate.ts` - Added AbortController support and `abort()` method
- `src/components/features/*/handlers/*.ts` - Updated all mutation handlers to use signal
- `src/components/admin-map/components/AdminBroadbandPanel.tsx` - Updated cancel handlers to use `abort()`

## See Also

- [MDN: AbortController](https://developer.mozilla.org/en-US/docs/Web/API/AbortController)
- [MDN: AbortSignal](https://developer.mozilla.org/en-US/docs/Web/API/AbortSignal)
- [Fetch API: Using AbortSignal](https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API/Using_Fetch#canceling_a_request)
