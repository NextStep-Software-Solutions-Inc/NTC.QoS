"use client";

import { useState } from "react";
import { SignInButton, useClerk, useUser } from "@clerk/nextjs";
import { Check, Copy, Lock } from "lucide-react";

import { Button } from "@/components/ui/button";

export default function UnauthorizedPage() {
  const { isLoaded, user } = useUser();
  const { signOut, openSignIn } = useClerk();
  const [copied, setCopied] = useState(false);
  const CopyIcon = copied ? Check : Copy;

  const copyToClipboard = async () => {
    if (user?.id) {
      try {
        await navigator.clipboard.writeText(user.id);
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
      } catch (err) {
        console.error("Failed to copy:", err);
      }
    }
  };

  const handleSignOutAndReAuth = async () => {
    try {
      await signOut();
      // Open sign-in modal after signing out
      setTimeout(() => {
        openSignIn({ fallbackRedirectUrl: window.location.href });
      }, 100);
    } catch (error) {
      console.error("Error signing out:", error);
    }
  };

  return (
    <div className="fixed inset-0 flex items-center justify-center bg-gray-50 dark:bg-gray-900 p-6">
      <div className="w-full h-full  bg-white dark:bg-gray-800 shadow-lg rounded-lg p-8 text-center flex flex-col items-center justify-center gap-6">
        <div className="mx-auto flex items-center justify-center h-20 w-20 rounded-full bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400">
          <Lock className="" size={50} />
        </div>

        <h1 className="text-3xl sm:text-4xl font-bold text-gray-900 dark:text-gray-100">Unauthorized</h1>
        <p className="max-w-prose text-sm text-muted-foreground">
          You do not have permission to view this page. Please sign in with an account that has access.
        </p>

        <div className="mt-2">
          <div className="text-xs text-zinc-500 dark:text-zinc-400 mb-1">User ID</div>
          <div className="flex items-center gap-2">
            <div
              className={`font-mono text-sm bg-gray-100 dark:bg-gray-700 px-3 py-2 rounded-md border border-gray-200 dark:border-gray-600 flex-1 text-gray-900 dark:text-gray-100 transition-all duration-300 ${
                copied
                  ? "ring-2 ring-green-500 dark:ring-green-400 shadow-lg shadow-green-500/25 dark:shadow-green-400/25"
                  : ""
              }`}>
              {isLoaded ? (user ? user.id : "Not signed in") : "Loading..."}
            </div>
            {user?.id && (
              <Button variant="outline" size="sm" onClick={copyToClipboard} className="shrink-0">
                <CopyIcon className="h-4 w-4" />
                {copied ? "Copied!" : "Copy"}
              </Button>
            )}
          </div>
        </div>

        <div className="mt-6 flex flex-wrap justify-center gap-3">
          {user ? (
            <Button variant="outline" onClick={handleSignOutAndReAuth}>
              Sign out & re-authenticate
            </Button>
          ) : (
            <SignInButton mode="modal">
              <Button>Sign in</Button>
            </SignInButton>
          )}
          <Button variant="ghost" onClick={() => (window.location.href = "/")}>
            Go to home
          </Button>
        </div>
      </div>
    </div>
  );
}
