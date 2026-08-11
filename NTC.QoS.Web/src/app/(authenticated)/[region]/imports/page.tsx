import { type Metadata } from "next";
import { Upload } from "lucide-react";

import ImportTabs from "@/components/features/file/tabs/ImportTabs";

export const metadata: Metadata = { title: "Imports | File", description: "Import your files here" };

function ImportsPage() {
  return (
    <div className="space-y-6 p-6 bg-gray-50/50 min-h-screen">
      {/* Header Section */}
      <div className="flex items-center gap-3">
        <div className="p-2.5 bg-primary/10 rounded-lg">
          <Upload className="h-6 w-6 text-primary" />
        </div>
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-gray-900">
            File Imports
          </h1>
          <p className="text-muted-foreground text-sm mt-0.5">
            Upload and manage data files for network infrastructure and performance monitoring.
          </p>
        </div>
      </div>

      {/* Tabs Section */}
      <div className="bg-card rounded-2xl border shadow-xl overflow-hidden">
        <ImportTabs />
      </div>
    </div>
  );
}

export default ImportsPage;
