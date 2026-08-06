"use client";

import { FileText } from "lucide-react";

import { StandardReportsTab } from "@/components/features/reports/components/StandardReportsTab";
import { AIReportsManager } from "@/components/features/ai-report/components/AIReportsManager";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

export default function ReportsPage() {
    return (
        <div className="space-y-6 p-6 bg-gray-50/50 min-h-screen">
            {/* Main Tabs */}
            <Tabs defaultValue="standard" className="w-full">
                <TabsList className="grid w-full grid-cols-2 mb-6">
                    <TabsTrigger value="standard" className="flex items-center gap-2">
                        <FileText className="h-4 w-4" />
                        <span className="hidden sm:inline">Standard Reports</span>
                        <span className="sm:hidden">Standard</span>
                    </TabsTrigger>
                    <TabsTrigger value="ai" className="flex items-center gap-2">
                        <FileText className="h-4 w-4" />
                        <span className="hidden sm:inline">AI Reports</span>
                        <span className="sm:hidden">AI</span>
                    </TabsTrigger>
                </TabsList>

                {/* Standard Reports Tab */}
                <TabsContent value="standard">
                    <StandardReportsTab />
                </TabsContent>

                {/* AI Reports Tab */}
                <TabsContent value="ai">
                    <AIReportsManager showGenerateButton showDeleteButton />
                </TabsContent>
            </Tabs>
        </div>
    );
}
