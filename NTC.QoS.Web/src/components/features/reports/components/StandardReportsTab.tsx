"use client";

import { Activity, FileText, Signal } from "lucide-react";

import { ConnectivityAssessmentTrigger } from "@/components/features/reports/components/ConnectivityAssessmentTrigger";
import { ConnectivityAndGapReport } from "@/components/features/reports/components/ConnectivityAndGapReport";
import { WiredBroadbandReport } from "@/components/features/reports/components/WiredBroadbandReport";
import { WirelessBroadbandReport } from "@/components/features/reports/components/WirelessBroadbandReport";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

export function StandardReportsTab() {
    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="space-y-1">
                <h2 className="text-2xl font-bold tracking-tight">Standard Reports</h2>
                <p className="text-muted-foreground">
                    Generate comprehensive reports on connectivity assessment, wired and wireless broadband coverage, and identify connectivity gaps across regions
                </p>
            </div>

            {/* Connectivity Assessment Trigger */}
            <ConnectivityAssessmentTrigger />

            {/* Report Type Tabs */}
            <Tabs defaultValue="wired" className="w-full">
                <TabsList className="grid w-full grid-cols-3">
                    <TabsTrigger value="wired" className="flex items-center gap-2">
                        <FileText className="h-4 w-4" />
                        <span className="hidden sm:inline">Wired Broadband</span>
                        <span className="sm:hidden">Wired</span>
                    </TabsTrigger>
                    <TabsTrigger value="wireless" className="flex items-center gap-2">
                        <Signal className="h-4 w-4" />
                        <span className="hidden sm:inline">Wireless Broadband</span>
                        <span className="sm:hidden">Wireless</span>
                    </TabsTrigger>
                    <TabsTrigger value="connectivity-gap" className="flex items-center gap-2">
                        <Activity className="h-4 w-4" />
                        <span className="hidden sm:inline">Connectivity &amp; Gap</span>
                        <span className="sm:hidden">Connect</span>
                    </TabsTrigger>
                </TabsList>

                <TabsContent value="wired">
                    <WiredBroadbandReport />
                </TabsContent>

                <TabsContent value="wireless">
                    <WirelessBroadbandReport />
                </TabsContent>

                <TabsContent value="connectivity-gap">
                    <ConnectivityAndGapReport />
                </TabsContent>
            </Tabs>
        </div>
    );
}
