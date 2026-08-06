import { useParams } from "next/navigation";
import { Hexagon } from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import { useLocationMap } from "@/providers/LocationMapContext";

interface TopProvincesListProps {
  provinces: { name: string; count: number }[];
}

export function TopProvincesList({ provinces }: TopProvincesListProps) {
  const { resolveUuid } = useLocationMap();
  const { region } = useParams<{ region: string }>();
  const maxCount = Math.max(...provinces.map((p) => p.count));

  return (
    <Card className="h-full">
      <CardHeader>
        <CardTitle className="text-base font-medium">Top Provinces with Most Cell Sites</CardTitle>
        <p className="text-sm text-muted-foreground">Provinces with highest tower count in {resolveUuid(region)}</p>
      </CardHeader>
      <CardContent className="space-y-6">
        {provinces.map((province, index) => (
          <div key={index} className="space-y-2">
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-3">
                <div className="relative flex items-center justify-center h-10 w-10">
                  <Hexagon className="h-10 w-10 text-blue-100 fill-blue-100" />
                  <span className="absolute text-blue-600 font-bold text-xs">{index + 1}</span>
                </div>
                <span className="font-medium text-sm">{province.name}</span>
              </div>
              <span className="font-bold text-sm">{province.count.toLocaleString()}</span>
            </div>
            <Progress
              value={(province.count / maxCount) * 100}
              className="h-2 bg-blue-50"
              indicatorClassName="bg-blue-500"
            />
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
