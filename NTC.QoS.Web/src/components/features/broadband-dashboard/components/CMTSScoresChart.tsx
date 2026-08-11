"use client";

import {
  PolarAngleAxis,
  PolarGrid,
  PolarRadiusAxis,
  Radar,
  RadarChart,
  ResponsiveContainer,
  Tooltip,
  TooltipProps,
} from "recharts";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

interface CMTSScoresChartProps {
  data: { subject: string; a: number; fullMark: number }[];
  totalScore?: number;
  connectivityStatus?: string;
}

export function CMTSScoresChart({ data, totalScore, connectivityStatus }: CMTSScoresChartProps) {
  // Calculate total possible score (sum of all fullMark values)
  const totalPossible = data.reduce((sum, item) => sum + item.fullMark, 0);
  
  // Calculate color based on scoring rubric:
  // Served (11-15): Green
  // Underserved (6-10): Yellow
  // Unserved (≤5): Red
  const getScoreColor = (score: number) => {
    if (score >= 11) return "text-green-600";
    if (score >= 6) return "text-yellow-600";
    return "text-red-600";
  };

  const getScoreBgColor = (score: number) => {
    if (score >= 11) return "bg-green-100";
    if (score >= 6) return "bg-yellow-100";
    return "bg-red-100";
  };

  const getConnectivityBadgeClass = (status: string) => {
    const lowerStatus = status.toLowerCase();
    if (lowerStatus === "served") return "bg-green-100 text-green-700 border-green-200 hover:bg-green-200";
    if (lowerStatus === "underserved") return "bg-yellow-100 text-yellow-700 border-yellow-200 hover:bg-yellow-200";
    if (lowerStatus === "unserved") return "bg-red-100 text-red-700 border-red-200 hover:bg-red-200";
    return "bg-gray-100 text-gray-700 border-gray-200 hover:bg-gray-200";
  };

  const customTooltip = ({ active, payload, label }: TooltipProps<number, string>) => {
    if (active && payload && payload.length) {
      const score = payload[0]?.value ?? 0;
      const scoreColor = score >= 2.5 ? "text-green-500" : score >= 1.5 ? "text-yellow-500" : "text-red-500";

      return (
        <div className="bg-gradient-to-r from-blue-50 to-indigo-50 p-4 rounded-xl shadow-xl border-2 border-blue-200 backdrop-blur-sm">
          <div className="flex items-center space-x-2 mb-2">
            <div className="w-3 h-3 bg-blue-500 rounded-full animate-pulse"></div>
            <p className="font-bold text-lg text-blue-800">{label}</p>
          </div>
          <div className="flex items-center space-x-2">
            <span className="text-sm font-medium text-gray-600">Score:</span>
            <span className={`font-bold text-xl ${scoreColor} drop-shadow-sm`}>{score}/3</span>
          </div>
          <div className="mt-2 text-xs text-gray-500">Connectivity Rating</div>
        </div>
      );
    }
    return null;
  };

  return (
    <Card className="h-full">
      <CardHeader>
        <div className="flex items-start justify-between gap-4">
          <div className="flex-1">
            <CardTitle className="text-base font-medium">CMTS Scores</CardTitle>
            <p className="text-sm text-muted-foreground">Connectivity evaluation (0-3 scale)</p>
          </div>
          <div className="flex items-center gap-3">
            {connectivityStatus && (
              <div className="flex flex-col items-center">
                <span className="text-xs text-muted-foreground font-medium mb-1">Connectivity Status</span>
                <div className={`flex items-center px-4 py-1.5 rounded-lg ${getConnectivityBadgeClass(connectivityStatus)}`}>
                  <span className="text-2xl font-bold">
                    {connectivityStatus}
                  </span>
                </div>
              </div>
            )}
            {totalScore !== undefined && (
              <div className="flex flex-col items-center">
                <span className="text-xs text-muted-foreground font-medium mb-1">Overall Score</span>
                <div className={`flex items-center gap-2 px-4 py-1.5 rounded-lg ${getScoreBgColor(totalScore)}`}>
                  <span className={`text-2xl font-bold ${getScoreColor(totalScore)}`}>
                    {Math.round(totalScore)}
                  </span>
                  <span className="text-sm text-gray-600 font-medium">/{totalPossible}</span>
                </div>
              </div>
            )}
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <div className="h-[350px] w-full">
          <ResponsiveContainer width="100%" height="100%">
            <RadarChart cx="50%" cy="50%" outerRadius="80%" data={data}>
              <PolarGrid />
              <PolarAngleAxis dataKey="subject" tick={{ fontSize: 12 }} />
              <PolarRadiusAxis angle={30} domain={[0, 3]} tick={false} axisLine={false} />
              <Radar name="Score" dataKey="a" stroke="#8884d8" fill="#8884d8" fillOpacity={0.6} />
              <Tooltip content={customTooltip} />
            </RadarChart>
          </ResponsiveContainer>
        </div>
      </CardContent>
    </Card>
  );
}
