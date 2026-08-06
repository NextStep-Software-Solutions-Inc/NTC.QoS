import { Grid, Home, Ruler, Users } from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

interface DemographicsCardProps {
  population: number;
  households: number;
  density: string;
  area: string;
}

export function DemographicsCard({ population, households, density, area }: DemographicsCardProps) {
  return (
    <Card className="h-full flex flex-col">
      <CardHeader className="flex flex-row items-center space-x-2 pb-2">
        <Users className="h-5 w-5 text-red-500" />
        <CardTitle className="text-base font-medium">Demographics</CardTitle>
      </CardHeader>
      <CardContent className="flex-1 grid grid-cols-2 gap-4 content-center">
        <div className="flex items-center space-x-4 p-2 rounded-lg hover:bg-gray-50 transition-colors">
          <div className="p-2 bg-blue-100 rounded-full">
            <Users className="h-4 w-4 text-blue-600" />
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Population</p>
            <p className="font-bold text-lg">{population.toLocaleString()}</p>
          </div>
        </div>
        <div className="flex items-center space-x-4 p-2 rounded-lg hover:bg-gray-50 transition-colors">
          <div className="p-2 bg-green-100 rounded-full">
            <Home className="h-4 w-4 text-green-600" />
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Households</p>
            <p className="font-bold text-lg">{households.toLocaleString()}</p>
          </div>
        </div>
        <div className="flex items-center space-x-4 p-2 rounded-lg hover:bg-gray-50 transition-colors">
          <div className="p-2 bg-purple-100 rounded-full">
            <Ruler className="h-4 w-4 text-purple-600" />
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Total Land Area (km²)</p>
            <p className="font-bold text-lg">{area}</p>
          </div>
        </div>
        <div className="flex items-center space-x-4 p-2 rounded-lg hover:bg-gray-50 transition-colors">
          <div className="p-2 bg-orange-100 rounded-full">
            <Grid className="h-4 w-4 text-orange-600" />
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Density</p>
            <p className="font-bold text-lg">{density}</p>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
