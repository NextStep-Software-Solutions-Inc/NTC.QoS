"use client";

import React, { useCallback, useRef, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import {
  CheckCircle2,
  Database,
  Loader2,
  MapPin,
  Pause,
  Play,
  Square,
  Timer,
  TrendingUp,
  XCircle,
  Zap,
} from "lucide-react";
import { toast } from "sonner";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Progress } from "@/components/ui/progress";
import { cn } from "@/lib/utils";
import { request } from "@/services/fetch";

// ============================================================================
// Constants & Types
// ============================================================================

const PHONE_MODELS = [
  "Samsung Galaxy S24 Ultra",
  "Samsung Galaxy S23",
  "Samsung Galaxy A54",
  "iPhone 15 Pro Max",
  "iPhone 15",
  "iPhone 14",
  "iPhone 13",
  "Xiaomi 14",
  "Xiaomi Redmi Note 13",
  "OPPO Reno 11",
  "OPPO A98",
  "Vivo V30",
  "Vivo Y36",
  "Realme 12 Pro+",
  "Realme C55",
  "Huawei P60",
  "Google Pixel 8",
  "OnePlus 12",
  "Nothing Phone 2",
  "Infinix Note 30",
];

// Philippine MNCs (Mobile Network Codes)
const MNCS = ["02", "03", "05", "18"];

// Philippine geographic bounds with regional clusters for realistic distribution
const PH_REGIONS = [
  { name: "NCR", minLat: 14.35, maxLat: 14.78, minLng: 120.9, maxLng: 121.15, weight: 25 },
  { name: "Region III", minLat: 14.8, maxLat: 16.0, minLng: 120.0, maxLng: 121.5, weight: 12 },
  { name: "Region IV-A", minLat: 13.5, maxLat: 14.5, minLng: 120.5, maxLng: 122.0, weight: 15 },
  { name: "Region VII", minLat: 9.4, maxLat: 11.3, minLng: 123.0, maxLng: 124.5, weight: 12 },
  { name: "Region XI", minLat: 6.0, maxLat: 8.0, minLng: 125.0, maxLng: 126.5, weight: 10 },
  { name: "Region VI", minLat: 10.0, maxLat: 11.8, minLng: 121.8, maxLng: 123.5, weight: 8 },
  { name: "Region I", minLat: 15.5, maxLat: 18.5, minLng: 119.8, maxLng: 121.0, weight: 6 },
  { name: "Region II", minLat: 16.0, maxLat: 18.5, minLng: 121.0, maxLng: 122.5, weight: 4 },
  { name: "CAR", minLat: 16.3, maxLat: 17.8, minLng: 120.3, maxLng: 121.5, weight: 3 },
  { name: "Region V", minLat: 12.0, maxLat: 14.2, minLng: 122.5, maxLng: 124.5, weight: 5 },
  { name: "Region X", minLat: 7.0, maxLat: 9.0, minLng: 124.0, maxLng: 126.0, weight: 4 },
];

type SignalQuality = "Unknown" | "VeryWeak" | "Weak" | "Average" | "Good" | "Excellent";

interface SeederState {
  isRunning: boolean;
  isPaused: boolean;
  totalCount: number;
  completedCount: number;
  successCount: number;
  failCount: number;
  startTime: number | null;
  currentRate: number;
  estimatedTimeRemaining: string;
}

interface ReferenceData {
  serviceProviderIds: string[];
  technologyIds: string[];
  generationIds: string[];
}

// ============================================================================
// Utility Functions
// ============================================================================

function randomFloat(min: number, max: number, precision = 2): number {
  return parseFloat((Math.random() * (max - min) + min).toFixed(precision));
}

function randomInt(min: number, max: number): number {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

function randomElement<T>(arr: T[]): T {
  return arr[Math.floor(Math.random() * arr.length)] as T;
}

function weightedRandomRegion() {
  const totalWeight = PH_REGIONS.reduce((sum, r) => sum + r.weight, 0);
  let random = Math.random() * totalWeight;

  for (const region of PH_REGIONS) {
    random -= region.weight;
    if (random <= 0) return region;
  }
  return PH_REGIONS[0]!;
}

function randomDate(): string {
  const now = new Date();
  const daysAgo = randomInt(0, 365);
  const date = new Date(now.getTime() - daysAgo * 24 * 60 * 60 * 1000);
  date.setHours(randomInt(0, 23), randomInt(0, 59), randomInt(0, 59));
  return date.toISOString();
}

function calculateSignalQuality(rssi: number): SignalQuality {
  if (rssi >= -65) return "Excellent";
  if (rssi >= -75) return "Good";
  if (rssi >= -85) return "Average";
  if (rssi >= -95) return "Weak";
  if (rssi >= -110) return "VeryWeak";
  return "Unknown";
}

function formatTime(seconds: number): string {
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = Math.floor(seconds % 60);
  if (h > 0) return `${h}h ${m}m ${s}s`;
  if (m > 0) return `${m}m ${s}s`;
  return `${s}s`;
}

async function sleep(ms: number) {
  return new Promise((r) => setTimeout(r, ms));
}

function generateSpeedtestPayload(refData: ReferenceData) {
  const region = weightedRandomRegion();
  const lat = randomFloat(region.minLat, region.maxLat, 6);
  const lng = randomFloat(region.minLng, region.maxLng, 6);
  const rssi = randomInt(-110, -50);
  const signalQuality = calculateSignalQuality(rssi);

  // Speed varies by signal quality
  let maxDownload: number, maxUpload: number, maxPing: number;
  switch (signalQuality) {
    case "Excellent":
      maxDownload = 500;
      maxUpload = 100;
      maxPing = 20;
      break;
    case "Good":
      maxDownload = 200;
      maxUpload = 50;
      maxPing = 50;
      break;
    case "Average":
      maxDownload = 50;
      maxUpload = 20;
      maxPing = 100;
      break;
    case "Weak":
      maxDownload = 10;
      maxUpload = 5;
      maxPing = 200;
      break;
    default:
      maxDownload = 5;
      maxUpload = 2;
      maxPing = 500;
  }

  const payload: Record<string, unknown> = {
    cellIdentifier: randomInt(0, 268435455).toString(16).toUpperCase().padStart(8, "0"),
    latitude: lat,
    longitude: lng,
    mcc: "515",
    mnc: randomElement(MNCS),
    phoneModel: randomElement(PHONE_MODELS),
    tac: randomInt(0, 65535).toString().padStart(5, "0"),
    dateTime: randomDate(),
    ping: randomFloat(5, maxPing, 2),
    jitter: randomFloat(0.5, 50, 2),
    rssi,
    uploadSpeed: randomFloat(0.1, maxUpload, 2),
    downloadSpeed: randomFloat(0.5, maxDownload, 2),
    signalQuality,
    isFixedBroadband: false,
  };

  if (refData.serviceProviderIds.length > 0) {
    payload.serviceProviderId = randomElement(refData.serviceProviderIds);
  }

  if (refData.technologyIds.length > 0) {
    payload.technologyId = randomElement(refData.technologyIds);
  }

  if (refData.generationIds.length > 0) {
    payload.generationId = randomElement(refData.generationIds);
  }

  return payload;
}

// ============================================================================
// Component
// ============================================================================

interface SpeedtestSeederProps {
  onComplete?: () => void;
}

export function SpeedtestSeeder({ onComplete }: SpeedtestSeederProps) {
  const [open, setOpen] = useState(false);
  const [targetCount, setTargetCount] = useState(1000);
  const [batchSize, setBatchSize] = useState(100);
  const [state, setState] = useState<SeederState>({
    isRunning: false,
    isPaused: false,
    totalCount: 0,
    completedCount: 0,
    successCount: 0,
    failCount: 0,
    startTime: null,
    currentRate: 0,
    estimatedTimeRemaining: "-",
  });

  const abortControllerRef = useRef<AbortController | null>(null);
  const pauseRef = useRef(false);
  const refDataRef = useRef<ReferenceData>({ serviceProviderIds: [], technologyIds: [], generationIds: [] });
  const toastIdRef = useRef<string | number | null>(null);

  // Fetch reference data (providers, technologies, generations)
  const fetchReferenceData = useCallback(async () => {
    try {
      // Fetch service providers
      const providersRes = await request.get<{ data: { id: string }[] }>(
        "/service-providers?pageSize=1000&serviceTypes=wireless",
      );
      refDataRef.current.serviceProviderIds = providersRes.data?.map((p) => p.id) || [];

      // Fetch technologies
      const techRes = await request.get<{ data: { id: string }[] }>("/technologies?pageSize=1000");
      refDataRef.current.technologyIds = techRes.data?.map((t) => t.id) || [];

      // Fetch generations
      const genRes = await request.get<{ data: { id: string }[] }>("/generations?pageSize=1000");
      refDataRef.current.generationIds = genRes.data?.map((g) => g.id) || [];

      return true;
    } catch (error) {
      console.error("Failed to fetch reference data:", error);
      return false;
    }
  }, []);

  // Create a single speedtest
  const createSpeedtest = useCallback(async (payload: Record<string, unknown>) => {
    try {
      await request.post("/speedtests", payload);
      return true;
    } catch {
      return false;
    }
  }, []);

  // Update progress toast
  const updateProgressToast = useCallback((state: SeederState) => {
    const percentage = state.totalCount > 0 ? Math.round((state.completedCount / state.totalCount) * 100) : 0;

    const elapsed = state.startTime ? (Date.now() - state.startTime) / 1000 : 0;
    const rate = elapsed > 0 ? state.completedCount / elapsed : 0;

    const toastContent = (
      <div className="w-full space-y-2">
        <div className="flex items-center justify-between">
          <span className="font-semibold">Seeding Speedtests</span>
          <Badge variant={state.isPaused ? "secondary" : "default"} className="text-xs">
            {state.isPaused ? "Paused" : "Running"}
          </Badge>
        </div>
        <Progress value={percentage} className="h-2" />
        <div className="flex justify-between text-xs text-muted-foreground">
          <span>
            {state.completedCount.toLocaleString()} / {state.totalCount.toLocaleString()}
          </span>
          <span>{percentage}%</span>
        </div>
        <div className="grid grid-cols-3 gap-2 text-xs">
          <div className="flex items-center gap-1">
            <CheckCircle2 className="h-3 w-3 text-green-500" />
            <span>{state.successCount.toLocaleString()}</span>
          </div>
          <div className="flex items-center gap-1">
            <XCircle className="h-3 w-3 text-red-500" />
            <span>{state.failCount.toLocaleString()}</span>
          </div>
          <div className="flex items-center gap-1">
            <TrendingUp className="h-3 w-3 text-blue-500" />
            <span>{rate.toFixed(1)}/s</span>
          </div>
        </div>
        {!state.isPaused && state.estimatedTimeRemaining !== "-" && (
          <div className="flex items-center gap-1 text-xs text-muted-foreground">
            <Timer className="h-3 w-3" />
            <span>ETA: {state.estimatedTimeRemaining}</span>
          </div>
        )}
      </div>
    );

    if (toastIdRef.current) {
      toast.dismiss(toastIdRef.current);
    }

    toastIdRef.current = toast.loading(toastContent, {
      duration: Infinity,
      id: "seeder-progress",
      classNames: { content: "w-full" },
    });
  }, []);

  // Process a batch of requests with dynamic concurrency
  const processBatch = useCallback(
    async (
      startIndex: number,
      batchSize: number,
      totalCount: number,
      onProgress: (success: number, fail: number, completed: number) => void,
    ) => {
      const endIndex = Math.min(startIndex + batchSize, totalCount);

      // Dynamic concurrency: scale with batch size but cap to avoid overload
      const maxConcurrency = Math.min(200, Math.max(20, Math.floor(batchSize / 5)));
      const promises: Promise<boolean>[] = [];

      for (let i = startIndex; i < endIndex; i++) {
        if (pauseRef.current || abortControllerRef.current?.signal.aborted) {
          break;
        }

        const payload = generateSpeedtestPayload(refDataRef.current);
        promises.push(createSpeedtest(payload));

        // Flush when we reach the concurrency cap
        if (promises.length >= maxConcurrency) {
          const results = await Promise.all(promises);
          const success = results.filter(Boolean).length;
          const fail = results.length - success;
          onProgress(success, fail, results.length);
          promises.length = 0;
        }
      }

      // Process remaining
      if (promises.length > 0) {
        const results = await Promise.all(promises);
        const success = results.filter(Boolean).length;
        const fail = results.length - success;
        onProgress(success, fail, results.length);
      }
    },
    [createSpeedtest],
  );

  // Start seeding
  const startSeeding = useCallback(async () => {
    abortControllerRef.current = new AbortController();
    pauseRef.current = false;

    // Initialize and capture start time locally to avoid stale state closures
    const startTime = Date.now();

    // Initialize state
    setState((prev) => ({
      ...prev,
      isRunning: true,
      isPaused: false,
      totalCount: targetCount,
      completedCount: 0,
      successCount: 0,
      failCount: 0,
      startTime,
      currentRate: 0,
      estimatedTimeRemaining: "-",
    }));

    // Show initial toast
    toast.loading("Initializing seeder...", { id: "seeder-init" });

    // Fetch reference data
    const hasRefData = await fetchReferenceData();
    toast.dismiss("seeder-init");

    if (!hasRefData || refDataRef.current.technologyIds.length === 0) {
      toast.error("Failed to fetch reference data. Please ensure technologies exist.");
      setState((prev) => ({ ...prev, isRunning: false }));
      return;
    }

    toast.success(
      `Loaded ${refDataRef.current.serviceProviderIds.length} providers, ${refDataRef.current.technologyIds.length} technologies`,
    );

    let currentIndex = 0;
    let totalSuccess = 0;
    let totalFail = 0;

    // Process batches with rate limiting (1000 req / 10 sec)
    while (currentIndex < targetCount && !abortControllerRef.current.signal.aborted) {
      // Check for pause
      while (pauseRef.current && !abortControllerRef.current.signal.aborted) {
        await new Promise((r) => setTimeout(r, 500));
      }

      if (abortControllerRef.current.signal.aborted) break;

      const batchStart = currentIndex;
      const currentBatchSize = Math.min(batchSize, targetCount - currentIndex);

      const batchStartTime = Date.now();

      await processBatch(batchStart, currentBatchSize, targetCount, (success, fail, completed) => {
        totalSuccess += success;
        totalFail += fail;
        currentIndex += completed;

        // Use the captured startTime for elapsed calculation (avoid stale state)
        const elapsed = (Date.now() - startTime) / 1000;
        const safeElapsed = Math.max(elapsed, 0.001);
        const rate = currentIndex / safeElapsed;
        const safeRate = Math.max(rate, 0.01);
        const remaining = Math.max(0, Math.ceil((targetCount - currentIndex) / safeRate));

        setState((prev) => ({
          ...prev,
          completedCount: currentIndex,
          successCount: totalSuccess,
          failCount: totalFail,
          currentRate: rate,
          estimatedTimeRemaining: formatTime(remaining),
          startTime,
        }));
      });

      const elapsed = (Date.now() - startTime) / 1000;
      const safeElapsed = Math.max(elapsed, 0.001);
      const curRate = currentIndex / safeElapsed;
      const safeCurRate = Math.max(curRate, 0.01);
      const etaSeconds = Math.max(0, Math.ceil((targetCount - currentIndex) / safeCurRate));

      updateProgressToast({
        isRunning: true,
        isPaused: pauseRef.current,
        totalCount: targetCount,
        completedCount: currentIndex,
        successCount: totalSuccess,
        failCount: totalFail,
        startTime,
        currentRate: curRate,
        estimatedTimeRemaining: formatTime(etaSeconds),
      });

      // Rate-window: ensure at least a 10s window per batch is respected
      const batchDuration = Date.now() - batchStartTime;
      const waitMs = Math.max(0, 10000 - batchDuration);
      if (currentIndex < targetCount && !abortControllerRef.current.signal.aborted && waitMs > 0) {
        await sleep(waitMs);
      }
    }

    // Complete
    if (toastIdRef.current) {
      toast.dismiss(toastIdRef.current);
    }

    if (abortControllerRef.current.signal.aborted) {
      toast.warning(`Seeding stopped. Created ${totalSuccess.toLocaleString()} speedtests.`);
    } else {
      toast.success(`✅ Seeding complete! Created ${totalSuccess.toLocaleString()} speedtests (${totalFail} failed)`, {
        duration: 10000,
      });
    }

    setState((prev) => ({ ...prev, isRunning: false, isPaused: false }));
    onComplete?.();
  }, [
    targetCount,
    batchSize,
    fetchReferenceData,
    processBatch,
    updateProgressToast,
    state.startTime,
    state.estimatedTimeRemaining,
    onComplete,
  ]);

  // Pause/Resume
  const togglePause = useCallback(() => {
    pauseRef.current = !pauseRef.current;
    setState((prev) => ({ ...prev, isPaused: pauseRef.current }));

    if (pauseRef.current) {
      toast.info("Seeding paused", { duration: 2000 });
    } else {
      toast.info("Seeding resumed", { duration: 2000 });
    }
  }, []);

  // Stop
  const stopSeeding = useCallback(() => {
    abortControllerRef.current?.abort();
    pauseRef.current = false;
    setState((prev) => ({ ...prev, isRunning: false, isPaused: false }));
  }, []);

  const progress = state.totalCount > 0 ? Math.round((state.completedCount / state.totalCount) * 100) : 0;

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button
          variant="outline"
          size="sm"
          className="gap-2 border-orange-300 text-orange-600 hover:bg-orange-50 dark:border-orange-700 dark:text-orange-400 dark:hover:bg-orange-950">
          <Database className="h-4 w-4" />
          Seed Data
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Database className="h-5 w-5 text-orange-500" />
            Seed Speedtest Data
          </DialogTitle>
          <DialogDescription>
            Generate random speedtest data scattered across the Philippines. Rate limit: 1000 requests per 10 seconds.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          {/* Configuration */}
          {!state.isRunning && (
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="count">Total Records</Label>
                <Input
                  id="count"
                  type="number"
                  value={targetCount}
                  onChange={(e) => setTargetCount(Math.max(1, parseInt(e.target.value) || 1))}
                  min={1}
                  max={10000000}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="batch">Batch Size</Label>
                <Input
                  id="batch"
                  type="number"
                  value={batchSize}
                  onChange={(e) => setBatchSize(Math.max(1, Math.min(1000, parseInt(e.target.value) || 1000)))}
                  min={1}
                  max={1000}
                />
              </div>
            </div>
          )}

          {/* Progress */}
          {state.isRunning && (
            <Card className="border-0 bg-gradient-to-br from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-900">
              <CardContent className="pt-4 space-y-4">
                <div className="flex items-center justify-between">
                  <span className="text-sm font-medium">Progress</span>
                  <Badge variant={state.isPaused ? "secondary" : "default"}>
                    {state.isPaused ? "Paused" : "Running"}
                  </Badge>
                </div>

                <Progress value={progress} className="h-3" />

                <div className="flex justify-between text-sm">
                  <span>
                    {state.completedCount.toLocaleString()} / {state.totalCount.toLocaleString()}
                  </span>
                  <span className="font-bold">{progress}%</span>
                </div>

                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div className="flex items-center gap-2">
                    <CheckCircle2 className="h-4 w-4 text-green-500" />
                    <span>Success: {state.successCount.toLocaleString()}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <XCircle className="h-4 w-4 text-red-500" />
                    <span>Failed: {state.failCount.toLocaleString()}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Zap className="h-4 w-4 text-yellow-500" />
                    <span>Rate: {state.currentRate.toFixed(1)}/s</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Timer className="h-4 w-4 text-blue-500" />
                    <span>ETA: {state.estimatedTimeRemaining}</span>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Estimated Time */}
          {!state.isRunning && (
            <div className="rounded-lg bg-blue-50 dark:bg-blue-950/30 p-3 text-sm">
              <div className="flex items-center gap-2 text-blue-700 dark:text-blue-300">
                <Timer className="h-4 w-4" />
                <span>Estimated time: {formatTime(Math.ceil(targetCount / 1000) * 10)}</span>
              </div>
              <p className="text-xs text-blue-600 dark:text-blue-400 mt-1">
                Data will be distributed across all Philippine regions
              </p>
            </div>
          )}
        </div>

        <DialogFooter>
          {!state.isRunning ? (
            <>
              <Button variant="outline" onClick={() => setOpen(false)}>
                Cancel
              </Button>
              <Button
                onClick={startSeeding}
                className="gap-2 bg-gradient-to-r from-orange-500 to-red-500 hover:from-orange-600 hover:to-red-600">
                <Play className="h-4 w-4" />
                Start Seeding
              </Button>
            </>
          ) : (
            <>
              <Button variant="outline" onClick={togglePause} className="gap-2">
                {state.isPaused ? (
                  <>
                    <Play className="h-4 w-4" />
                    Resume
                  </>
                ) : (
                  <>
                    <Pause className="h-4 w-4" />
                    Pause
                  </>
                )}
              </Button>
              <Button variant="destructive" onClick={stopSeeding} className="gap-2">
                <Square className="h-4 w-4" />
                Stop
              </Button>
            </>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export default SpeedtestSeeder;
