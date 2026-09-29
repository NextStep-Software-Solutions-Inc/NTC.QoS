"use client";

import React, { useEffect } from "react";
import { AlertCircle, RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";

interface SuperAdminErrorProps {
  error: Error & { digest?: string };
  reset: () => void;
}

export default function SuperAdminError({ error, reset }: SuperAdminErrorProps) {
  useEffect(() => {
    console.error("Super Admin layout error:", error);
  }, [error]);

  return (
    <main className="w-full px-4 flex-1 flex flex-col items-center justify-center min-h-[60vh]">
      <div className="max-w-md w-full text-center space-y-4 p-6 rounded-xl border border-destructive/20 bg-destructive/5 dark:bg-destructive/10">
        <div className="mx-auto h-12 w-12 rounded-full bg-destructive/10 flex items-center justify-center text-destructive">
          <AlertCircle className="h-6 w-6" />
        </div>

        <div className="space-y-1">
          <h1 className="text-xl font-bold text-foreground">Something went wrong</h1>
          <p className="text-xs text-muted-foreground">
            An unexpected error occurred while rendering this management view. This can happen during network interruptions or unexpected server responses.
          </p>
        </div>

        {error?.message && (
          <pre className="p-3 rounded-md bg-muted text-[11px] font-mono text-muted-foreground text-left overflow-auto max-h-32 border">
            {error.message}
          </pre>
        )}

        <div className="pt-2 flex items-center justify-center gap-2">
          <Button variant="default" size="sm" onClick={() => reset()} className="gap-1.5">
            <RefreshCw className="h-3.5 w-3.5" />
            Try again
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => window.location.reload()}
          >
            Reload Page
          </Button>
        </div>
      </div>
    </main>
  );
}
