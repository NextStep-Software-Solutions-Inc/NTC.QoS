"use client";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

interface SpeedData {
  date: Date;
  download?: number;
  upload?: number;
}

interface MonthlyAverage {
  avgDownload: number;
  avgUpload: number;
}

interface MonthlyAccumulator extends MonthlyAverage {
  sumDownload: number;
  sumUpload: number;
  countDownload: number;
  countUpload: number;
}

export default function SpeedtestMonthlyPerformance({ data = [] }: { data?: SpeedData[] }) {
  const monthlyAverages: Record<string, MonthlyAccumulator> = data.reduce(
    (acc, curr) => {
      const key = `${curr.date.getFullYear()}-${String(curr.date.getMonth() + 1).padStart(2, "0")}`;

      acc[key] ??= { avgDownload: 0, avgUpload: 0, sumDownload: 0, sumUpload: 0, countDownload: 0, countUpload: 0 };

      const group = acc[key];
      if (typeof curr.download === "number") {
        group.sumDownload += curr.download;
        group.countDownload++;
      }
      if (typeof curr.upload === "number") {
        group.sumUpload += curr.upload;
        group.countUpload++;
      }

      group.avgDownload = group.countDownload > 0 ? group.sumDownload / group.countDownload : 0;
      group.avgUpload = group.countUpload > 0 ? group.sumUpload / group.countUpload : 0;

      return acc;
    },
    {} as Record<string, MonthlyAccumulator>,
  );

  return (
    <Card className="w-full mx-auto mt-5">
      <CardHeader>
        <CardTitle>Monthly Performance Summary</CardTitle>
        <CardDescription>Detailed breakdown of speed test results for 2024</CardDescription>
      </CardHeader>
      <CardContent className="grid gap-3">
        {Object.entries(monthlyAverages).map(([month, stats]) => {
          if (!stats) return null;

          const { avgDownload, avgUpload } = stats;
          const [year, m] = month.split("-");
          const date = new Date(Number(year), Number(m) - 1);
          const monthName = date.toLocaleString("en-US", { month: "long", year: "numeric" });

          return (
            <div key={month} className="mb-4 flex items-center justify-between rounded-md border px-4 py-3">
              <div className="grid grid-cols-3 gap-4 w-full text-center">
                <div className="font-bold text-left">{monthName}</div>
                <div className="text-gray-500">
                  Avg Upload: <span className="font-bold text-red-500 ">{avgUpload.toFixed(2)}</span> Mbps
                </div>
                <div className="text-right ">
                  Avg Download: <span className="font-bold text-blue-500">{avgDownload.toFixed(2)}</span> Mbps
                </div>
              </div>
            </div>
          );
        })}
      </CardContent>
    </Card>
  );
}
