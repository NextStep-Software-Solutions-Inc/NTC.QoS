"use client";

import { useRouter } from "next/navigation";
import { AlertTriangle, ArrowLeft, Home } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export default function TimeoutPage() {
  const router = useRouter();

  const handleGoBack = () => {
    router.back();
  };

  const handleGoHome = () => {
    router.push("/");
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-orange-100 dark:bg-orange-900/20 text-orange-600 dark:text-orange-400 mb-4">
            <AlertTriangle className="h-8 w-8" />
          </div>
          <CardTitle className="text-2xl font-bold text-gray-900 dark:text-gray-100">Request Timeout</CardTitle>
          <CardDescription className="text-gray-600 dark:text-gray-400">
            The server is taking too long to respond. This might be due to a slow connection or server issues.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="text-sm text-gray-500 dark:text-gray-400 text-center space-y-2">
            <p>Please try one of the following options:</p>
            <ul className="list-disc list-inside space-y-1 text-left">
              <li>Click &quot;Try Again&quot; to retry the request</li>
              <li>Check your internet connection</li>
              <li>Try again in a few moments</li>
            </ul>
          </div>
          <div className="flex flex-col gap-3">
            <Button onClick={handleGoBack} className="w-full">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Go Back
            </Button>

            <Button onClick={handleGoHome} variant="ghost" className="w-full">
              <Home className="h-4 w-4 mr-2" />
              Go Home
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
