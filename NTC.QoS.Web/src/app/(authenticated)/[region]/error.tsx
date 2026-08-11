"use client";

import React from "react";

import { Button } from "@/components/ui/button";

interface ErrorProps {
  error: Error;
  reset?: () => void;
}

export default function Error({ error, reset }: ErrorProps) {
  // Log the error to the console and (optionally) to remote logging
  console.error("Server Error in region layout:", error);

  return (
    <main className="w-full px-4 flex-1 flex flex-col items-center justify-center min-h-screen">
      <div className="max-w-xl text-center">
        <svg
          className="mx-auto h-12 w-12 text-destructive"
          viewBox="0 0 24 24"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          aria-hidden>
          <path d="M12 9v4" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
          <path d="M12 17h.01" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
          <path
            d="M10.29 3.86 1.82 18a2 2 0 0 0 1.79 3h16.78a2 2 0 0 0 1.79-3L13.71 3.86A2 2 0 0 0 11.29 3h-2a2 2 0 0 0-1 .86z"
            stroke="currentColor"
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
        <h1 className="mt-4 text-2xl font-semibold">Something went wrong</h1>
        <p className="mt-2 text-sm text-muted-foreground">An unexpected error occurred while loading this region.</p>
        <pre className="mt-4 p-3 rounded-md bg-surface text-xs text-foreground overflow-auto">{error?.message}</pre>
        <div className="mt-6 flex items-center justify-center gap-2">
          {reset && (
            <Button variant="secondary" onClick={() => reset()}>
              Try again
            </Button>
          )}
        </div>
      </div>
    </main>
  );
}
