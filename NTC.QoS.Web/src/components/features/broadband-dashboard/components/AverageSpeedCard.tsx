import { Check } from "lucide-react";

import { Card, CardContent } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";

interface AverageSpeedCardProps {
  download: number;
  upload: number;
  speedTestCount?: number;
  meetsRequirements: boolean;
  action?: { label: string; onClick: () => void };
}

export function AverageSpeedCard({
  download,
  upload,
  speedTestCount,
  meetsRequirements,
  action,
}: AverageSpeedCardProps) {
  const ceilingSpeed = Math.max(download, upload) * 1.5; // Max speed for progress bar calculation + 50%

  const downloadProgress = Math.min((download / ceilingSpeed) * 100, 100);
  const uploadProgress = Math.min((upload / ceilingSpeed) * 100, 100);

  return (
    <Card className="w-full">
      <CardContent className="pt-6 space-y-6">
        <div className="flex justify-between items-center">
          <h3 className="font-semibold text-lg">Average Speed</h3>
          {action && (
            <button
              onClick={(e) => {
                e.stopPropagation();
                action.onClick();
              }}
              className="text-xs font-medium text-blue-600 hover:text-blue-800 hover:underline focus:outline-none">
              {action.label}
            </button>
          )}
        </div>
        <div className="space-y-2">
          <div className="flex justify-between text-sm font-medium">
            <span>Average Download</span>
            <span>{download} Mbps</span>
          </div>
          <Progress value={downloadProgress} className="h-2 bg-blue-100" indicatorClassName="bg-blue-500" />
        </div>

        <div className="space-y-2">
          <div className="flex justify-between text-sm font-medium">
            <span>Average Upload</span>
            <span>{upload} Mbps</span>
          </div>
          <Progress value={uploadProgress} className="h-2 bg-green-100" indicatorClassName="bg-green-500" />
        </div>

        <div className="flex items-center space-x-2 text-sm text-muted-foreground">
          <span>Speed Tests: {speedTestCount}</span>
        </div>

        {meetsRequirements && (
          <div className="flex items-center space-x-2 text-sm font-medium">
            <Check className="h-4 w-4" />
            <span>Meets minimum requirements</span>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
