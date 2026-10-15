"use client";

import { useState } from "react";
import { Wifi } from "lucide-react";
import { Cell, Label, Pie, PieChart, ResponsiveContainer } from "recharts";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

interface ServiceProviderCardProps {
  providers: { name: string; count: number; fill: string }[];
}

export function ServiceProviderCard({ providers }: ServiceProviderCardProps) {
  const [selectedProvider, setSelectedProvider] = useState(providers[0]?.name);

  const activeProvider = providers.find((p) => p.name === selectedProvider) || providers[0];

  return (
    <Card className="h-full flex flex-col">
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <div className="flex items-center space-x-2">
          <Wifi className="h-5 w-5 text-green-500" />
          <CardTitle className="text-base font-medium">Service Provider</CardTitle>
        </div>
        <Select value={selectedProvider} onValueChange={setSelectedProvider}>
          <SelectTrigger className="w-[200px] h-8 text-xs overflow-hidden truncate">
            <div className="flex items-center gap-2 min-w-0 flex-1">
              {activeProvider && (
                <div className="w-2 h-2 rounded-full flex-shrink-0" style={{ backgroundColor: activeProvider.fill }} />
              )}
              <SelectValue className="truncate" />
            </div>
          </SelectTrigger>
          <SelectContent>
            {providers.map((provider) => (
              <SelectItem key={provider.name} value={provider.name}>
                {provider.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </CardHeader>
      <CardContent className="flex-1 flex items-center justify-center">
        <div className="h-[200px] w-full relative">
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={providers}
                cx="50%"
                cy="50%"
                innerRadius={60}
                outerRadius={80}
                paddingAngle={5}
                dataKey="count"
                stroke="none"
                cornerRadius={5}>
                {providers.map((entry, index) => (
                  <Cell
                    key={`cell-${index}`}
                    fill={entry.fill}
                    opacity={entry.name === selectedProvider ? 1 : 0.2}
                    stroke={entry.name === selectedProvider ? entry.fill : "none"}
                    strokeWidth={2}
                  />
                ))}
                <Label
                  content={({ viewBox }) => {
                    if (viewBox && "cx" in viewBox && "cy" in viewBox) {
                      return (
                        <text x={viewBox.cx} y={viewBox.cy} textAnchor="middle" dominantBaseline="middle">
                          <tspan x={viewBox.cx} y={viewBox.cy} className="fill-foreground text-3xl font-bold">
                            {activeProvider?.count.toLocaleString()}
                          </tspan>
                          <tspan
                            x={viewBox.cx}
                            y={(viewBox.cy || 0) + 25}
                            className="fill-muted-foreground text-xs font-medium truncate"
                            style={{
                              maxWidth: "80px",
                              overflow: "hidden",
                              textOverflow: "ellipsis",
                              whiteSpace: "nowrap",
                            }}>
                            {activeProvider?.name}
                          </tspan>
                        </text>
                      );
                    }
                  }}
                />
              </Pie>
            </PieChart>
          </ResponsiveContainer>
        </div>
      </CardContent>
    </Card>
  );
}
