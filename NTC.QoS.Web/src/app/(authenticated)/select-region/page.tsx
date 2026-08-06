import { Metadata } from "next";
import Link from "next/link";
import { ArrowLeft } from "lucide-react";

import { Button } from "@/components/ui/button";
import RegionList from "./RegionList";

export const metadata: Metadata = { title: "Select Region", description: "Select a region to continue" };

export default function SelectRegionPage() {
  return (
    <main className="relative w-full h-screen flex flex-col overflow-hidden bg-gradient-to-br from-blue-950/10 via-background to-red-950/10">
      {/* Animated background elements - Philippine flag colors */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 w-96 h-96 bg-blue-600/10 rounded-full blur-3xl animate-pulse" />
        <div
          className="absolute -bottom-40 -left-40 w-96 h-96 bg-red-600/10 rounded-full blur-3xl animate-pulse"
          style={{ animationDelay: "1s" }}
        />
        <div
          className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-80 h-80 bg-yellow-500/5 rounded-full blur-3xl animate-pulse"
          style={{ animationDelay: "2s" }}
        />
      </div>

      {/* Header with back button */}
      <div className="relative z-10 w-full px-4 sm:px-6 lg:px-8 pt-6 pb-4">
        <div className="max-w-7xl mx-auto">
          <Link href="/">
            <Button variant="ghost" size="sm" className="mb-4 group hover:bg-accent/50 transition-all duration-300">
              <ArrowLeft className="w-4 h-4 mr-2 group-hover:-translate-x-1 transition-transform duration-300" />
              Back
            </Button>
          </Link>
          <div className="space-y-2 animate-in fade-in slide-in-from-bottom-4 duration-700">
            <h1 className="text-4xl sm:text-5xl font-bold bg-gradient-to-r from-blue-600 via-red-600 to-yellow-500 bg-clip-text text-transparent pb-1">
              Choose Your Region
            </h1>
            <p className="text-muted-foreground text-base sm:text-lg">
              Select a region to explore broadband coverage and quality metrics.
            </p>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="relative z-10 flex-1 w-full px-4 sm:px-6 lg:px-8 pb-6 overflow-hidden">
        <div className="max-w-7xl mx-auto h-full flex flex-col">
          <RegionList />
        </div>
      </div>
    </main>
  );
}
