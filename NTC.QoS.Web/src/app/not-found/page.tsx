"use client";

import { useParams, useRouter } from "next/navigation";
import { AlertTriangle, ArrowLeft, Home } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export default function RegionNotFoundPage() {
  const params = useParams();
  const router = useRouter();
  const regionId = params.region as string;

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-yellow-100 dark:bg-yellow-900/20 text-yellow-600 dark:text-yellow-400 mb-4">
            <AlertTriangle className="h-8 w-8" />
          </div>
          <CardTitle className="text-2xl font-bold text-gray-900 dark:text-gray-100">Region Not Found</CardTitle>
          <CardDescription className="text-gray-600 dark:text-gray-400">
            The region with ID{" "}
            <code className="bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded text-sm font-mono">{regionId}</code> does
            not exist.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <p className="text-sm text-gray-500 dark:text-gray-400 text-center">
            This region may have been removed or the URL might be incorrect.
          </p>
          <div className="flex flex-col gap-3">
            <Button onClick={() => router.back()} variant="outline" className="w-full">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Go Back
            </Button>
            <Button onClick={() => router.push("/select-region")} className="w-full">
              <Home className="h-4 w-4 mr-2" />
              Select Region
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
