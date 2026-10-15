import { CheckCircle2, Globe, MapPin, Ruler } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

interface LocationCardProps {
  status: string;
  area: string;
}

export function LocationCard({ status, area }: LocationCardProps) {
  return (
    <Card className="h-full overflow-hidden relative border-blue-200 bg-gradient-to-br from-white to-blue-50/50 shadow-sm hover:shadow-md transition-all duration-200">
      <div className="absolute -top-6 -right-6 opacity-[0.03]">
        <Globe className="h-48 w-48 text-blue-900" />
      </div>
      <CardHeader className="pb-2 relative z-10">
        <div className="flex items-center space-x-3">
          <div className="p-2.5 bg-blue-100 rounded-xl shadow-sm">
            <MapPin className="h-5 w-5 text-blue-600" />
          </div>
          <CardTitle className="text-lg font-semibold text-blue-900">Location Status</CardTitle>
        </div>
      </CardHeader>
      <CardContent className="pt-6 relative z-10">
        <div className="grid grid-cols-2 gap-4">
          <div className="flex flex-col space-y-2 p-4 rounded-xl bg-white/80 border border-blue-100 shadow-sm backdrop-blur-sm">
            <div className="flex items-center gap-2 text-sm text-muted-foreground font-medium">
              <CheckCircle2 className="h-4 w-4 text-green-500" />
              Connectivity Status
            </div>
            <div className="flex items-center pt-1">
              <Badge
                variant="secondary"
                className="bg-green-100 text-green-700 hover:bg-green-200 px-3 py-1 text-sm font-semibold border-green-200">
                {status}
              </Badge>
            </div>
          </div>
          <div className="flex flex-col space-y-2 p-4 rounded-xl bg-white/80 border border-blue-100 shadow-sm backdrop-blur-sm">
            <div className="flex items-center gap-2 text-sm text-muted-foreground font-medium">
              <Ruler className="h-4 w-4 text-blue-500" />
              Total Land Area (km²)
            </div>
            <p className="text-2xl font-bold text-gray-900 tracking-tight pt-1">{area}</p>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
