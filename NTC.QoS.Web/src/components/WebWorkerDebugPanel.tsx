"use client";

import { useEffect, useRef, useState } from "react";
import { ChevronDown, ChevronUp, Eye, EyeOff, Move } from "lucide-react";

type WorkerStats = {
  workerInitialized: boolean;
  totalProcessed: number;
  lastBatchSize: number;
  lastProcessingTime: number;
  averageProcessingTime: number;
  totalBatches: number;
  totalUnique: number;
  totalDuplicates: number;
  lastUniqueCount: number;
  lastDuplicateCount: number;
};

type WorkerStatsMap = { nap: WorkerStats; cell: WorkerStats; speedtest: WorkerStats };

type WorkerEvent = {
  type: "WORKER_INIT" | "WORKER_PROCESS" | "WORKER_COMPLETE";
  count?: number;
  duration?: number;
  workerType?: "nap" | "cell" | "speedtest";
  newCount?: number;
  duplicateCount?: number;
};

// Declare global event for worker stats
declare global {
  interface WindowEventMap {
    "worker-stats": CustomEvent<WorkerEvent>;
  }
}

/**
 * Debug panel to monitor Web Worker activity
 * Automatically shows only in development mode
 *
 * Usage:
 * ```tsx
 * import { WebWorkerDebugPanel } from "@/components/WebWorkerDebugPanel";
 *
 * // In your component - will only show in dev mode
 * <WebWorkerDebugPanel />
 * ```
 */
export const WebWorkerDebugPanel = () => {
  const [stats, setStats] = useState<WorkerStatsMap>({
    nap: {
      workerInitialized: false,
      totalProcessed: 0,
      lastBatchSize: 0,
      lastProcessingTime: 0,
      averageProcessingTime: 0,
      totalBatches: 0,
      totalUnique: 0,
      totalDuplicates: 0,
      lastUniqueCount: 0,
      lastDuplicateCount: 0,
    },
    cell: {
      workerInitialized: false,
      totalProcessed: 0,
      lastBatchSize: 0,
      lastProcessingTime: 0,
      averageProcessingTime: 0,
      totalBatches: 0,
      totalUnique: 0,
      totalDuplicates: 0,
      lastUniqueCount: 0,
      lastDuplicateCount: 0,
    },
    speedtest: {
      workerInitialized: false,
      totalProcessed: 0,
      lastBatchSize: 0,
      lastProcessingTime: 0,
      averageProcessingTime: 0,
      totalBatches: 0,
      totalUnique: 0,
      totalDuplicates: 0,
      lastUniqueCount: 0,
      lastDuplicateCount: 0,
    },
  });
  const [logs, setLogs] = useState<string[]>([]);
  const [isExpanded, setIsExpanded] = useState(true);
  const [visibleWorkers, setVisibleWorkers] = useState<{ nap: boolean; cell: boolean; speedtest: boolean }>({
    nap: true,
    cell: true,
    speedtest: true,
  });

  // Dragging state
  const [position, setPosition] = useState({ x: 0, y: 0 });
  const [isDragging, setIsDragging] = useState(false);
  const dragStartRef = useRef({ x: 0, y: 0 });

  // Only show in development mode
  const isDevelopment = process.env.NODE_ENV === "development";

  const toggleWorkerVisibility = (workerType: "nap" | "cell" | "speedtest") => {
    setVisibleWorkers((prev) => ({ ...prev, [workerType]: !prev[workerType] }));
  };

  // Handle drag start
  const handleDragStart = (e: React.MouseEvent | React.TouchEvent) => {
    e.preventDefault();
    setIsDragging(true);
    let clientX: number;
    let clientY: number;
    if ("touches" in e && e.touches.length > 0) {
      clientX = e.touches[0]!.clientX;
      clientY = e.touches[0]!.clientY;
    } else if ("clientX" in e) {
      clientX = e.clientX;
      clientY = e.clientY;
    } else {
      return;
    }
    dragStartRef.current = { x: clientX - position.x, y: clientY - position.y };
  };

  // Handle drag move and end
  useEffect(() => {
    if (!isDragging) return;

    const handleMove = (e: MouseEvent | TouchEvent) => {
      let clientX: number;
      let clientY: number;
      if ("touches" in e && e.touches.length > 0) {
        clientX = e.touches[0]!.clientX;
        clientY = e.touches[0]!.clientY;
      } else if ("clientX" in e) {
        clientX = e.clientX;
        clientY = e.clientY;
      } else {
        return;
      }
      setPosition({ x: clientX - dragStartRef.current.x, y: clientY - dragStartRef.current.y });
    };

    const handleEnd = () => {
      setIsDragging(false);
    };

    document.addEventListener("mousemove", handleMove);
    document.addEventListener("mouseup", handleEnd);
    document.addEventListener("touchmove", handleMove);
    document.addEventListener("touchend", handleEnd);

    return () => {
      document.removeEventListener("mousemove", handleMove);
      document.removeEventListener("mouseup", handleEnd);
      document.removeEventListener("touchmove", handleMove);
      document.removeEventListener("touchend", handleEnd);
    };
  }, [isDragging]);

  useEffect(() => {
    const handleWorkerEvent = (event: CustomEvent<WorkerEvent>) => {
      const { type, count, duration, workerType = "nap" } = event.detail;
      const workerLabel = workerType.toUpperCase();

      if (type === "WORKER_INIT") {
        setStats((prev) => ({ ...prev, [workerType]: { ...prev[workerType], workerInitialized: true } }));
        setLogs((prev) => [...prev.slice(-9), `🔧 ${workerLabel} Worker initialized`]);
      } else if (type === "WORKER_PROCESS" && count) {
        setLogs((prev) => [...prev.slice(-9), `📤 ${workerLabel}: Processing ${count} items...`]);
      } else if (type === "WORKER_COMPLETE" && count !== undefined && duration !== undefined) {
        setStats((prev) => {
          const currentStats = prev[workerType];
          const newBatches = currentStats.totalBatches + 1;
          const newTotal = currentStats.totalProcessed + count;
          const newAverage = (currentStats.averageProcessingTime * currentStats.totalBatches + duration) / newBatches;
          const newUnique = currentStats.totalUnique + (event.detail.newCount ?? count);
          const newDuplicates = currentStats.totalDuplicates + (event.detail.duplicateCount ?? 0);

          return {
            ...prev,
            [workerType]: {
              ...currentStats,
              totalProcessed: newTotal,
              lastBatchSize: count,
              lastProcessingTime: duration,
              averageProcessingTime: newAverage,
              totalBatches: newBatches,
              totalUnique: newUnique,
              totalDuplicates: newDuplicates,
              lastUniqueCount: event.detail.newCount ?? count,
              lastDuplicateCount: event.detail.duplicateCount ?? 0,
            },
          };
        });
        setLogs((prev) => [
          ...prev.slice(-9),
          `✅ ${workerLabel}: Processed ${count} items (${event.detail.newCount ?? count} unique, ${event.detail.duplicateCount ?? 0} duplicates) in ${duration.toFixed(2)}ms`,
        ]);
      }
    };

    window.addEventListener("worker-stats", handleWorkerEvent as EventListener);

    return () => {
      window.removeEventListener("worker-stats", handleWorkerEvent as EventListener);
    };
  }, []);

  if (!isDevelopment) {
    return null;
  }

  const anyWorkerActive =
    stats.nap.workerInitialized || stats.cell.workerInitialized || stats.speedtest.workerInitialized;
  const totalProcessed = stats.nap.totalProcessed + stats.cell.totalProcessed + stats.speedtest.totalProcessed;
  const totalBatches = stats.nap.totalBatches + stats.cell.totalBatches + stats.speedtest.totalBatches;

  return (
    <div
      className="fixed z-[9999] w-96 rounded-lg border border-gray-300 bg-white shadow-2xl"
      style={{
        bottom: position.y === 0 ? "1rem" : "auto",
        right: position.x === 0 ? "1rem" : "auto",
        top: position.y !== 0 ? position.y : "auto",
        left: position.x !== 0 ? position.x : "auto",
      }}>
      {/* Header with Toggle and Drag Handle */}
      <div className="flex items-center justify-between border-b p-3 hover:bg-gray-50">
        {/* Drag handle */}
        <div
          className="cursor-grab active:cursor-grabbing p-1 hover:bg-gray-200 rounded mr-2"
          onMouseDown={handleDragStart}
          onTouchStart={handleDragStart}
          title="Drag to move">
          <Move size={14} className="text-gray-500" />
        </div>
        <div className="flex flex-1 cursor-pointer items-center gap-2" onClick={() => setIsExpanded(!isExpanded)}>
          <h3 className="text-sm font-bold">🔧 Web Worker Monitor</h3>
          <span className={`text-xs ${anyWorkerActive ? "text-green-600" : "text-red-600"}`}>
            {anyWorkerActive ? "● Active" : "● Inactive"}
          </span>
        </div>
        <button
          className="rounded p-1 hover:bg-gray-200"
          onClick={(e) => {
            e.stopPropagation();
            setIsExpanded(!isExpanded);
          }}>
          {isExpanded ? <ChevronDown size={16} /> : <ChevronUp size={16} />}
        </button>
      </div>

      {/* Collapsible Content */}
      {isExpanded && (
        <div className="p-4">
          {/* Worker Visibility Toggles */}
          <div className="mb-3 flex gap-2 border-b pb-2">
            <button
              onClick={() => toggleWorkerVisibility("nap")}
              className={`flex items-center gap-1 rounded px-2 py-1 text-[10px] font-medium transition-colors ${
                visibleWorkers.nap
                  ? "bg-blue-100 text-blue-700 hover:bg-blue-200"
                  : "bg-gray-100 text-gray-400 hover:bg-gray-200"
              }`}>
              {visibleWorkers.nap ? <Eye size={12} /> : <EyeOff size={12} />}
              NAP
            </button>
            <button
              onClick={() => toggleWorkerVisibility("cell")}
              className={`flex items-center gap-1 rounded px-2 py-1 text-[10px] font-medium transition-colors ${
                visibleWorkers.cell
                  ? "bg-green-100 text-green-700 hover:bg-green-200"
                  : "bg-gray-100 text-gray-400 hover:bg-gray-200"
              }`}>
              {visibleWorkers.cell ? <Eye size={12} /> : <EyeOff size={12} />}
              Cell
            </button>
            <button
              onClick={() => toggleWorkerVisibility("speedtest")}
              className={`flex items-center gap-1 rounded px-2 py-1 text-[10px] font-medium transition-colors ${
                visibleWorkers.speedtest
                  ? "bg-orange-100 text-orange-700 hover:bg-orange-200"
                  : "bg-gray-100 text-gray-400 hover:bg-gray-200"
              }`}>
              {visibleWorkers.speedtest ? <Eye size={12} /> : <EyeOff size={12} />}
              SpeedTest
            </button>
          </div>

          <div className="mb-3 space-y-2 text-xs">
            {/* NAP Worker Stats */}
            {visibleWorkers.nap && (
              <div className="rounded bg-blue-50 p-2">
                <div className="mb-1 flex items-center justify-between font-semibold text-blue-900">
                  <span>📍 NAP Worker</span>
                  <span className={stats.nap.workerInitialized ? "text-green-600" : "text-gray-400"}>
                    {stats.nap.workerInitialized ? "●" : "○"}
                  </span>
                </div>
                {stats.nap.workerInitialized && (
                  <div className="space-y-0.5 text-[11px]">
                    <div className="flex justify-between">
                      <span className="text-gray-700">Processed:</span>
                      <span className="font-semibold">{stats.nap.totalProcessed.toLocaleString()}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-700">Unique:</span>
                      <span className="font-semibold text-green-600">{stats.nap.totalUnique.toLocaleString()}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-700">Duplicates:</span>
                      <span className="font-semibold text-red-600">{stats.nap.totalDuplicates.toLocaleString()}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-700">Batches:</span>
                      <span className="font-semibold">{stats.nap.totalBatches}</span>
                    </div>
                    {stats.nap.lastProcessingTime > 0 && (
                      <div className="flex justify-between">
                        <span className="text-gray-700">Last:</span>
                        <span className="font-semibold">
                          {stats.nap.lastProcessingTime.toFixed(2)}ms ({stats.nap.lastUniqueCount} unique,{" "}
                          {stats.nap.lastDuplicateCount} dup)
                        </span>
                      </div>
                    )}
                  </div>
                )}
              </div>
            )}

            {/* Cell Worker Stats */}
            {visibleWorkers.cell && (
              <div className="rounded bg-green-50 p-2">
                <div className="mb-1 flex items-center justify-between font-semibold text-green-900">
                  <span>📡 Cell Worker</span>
                  <span className={stats.cell.workerInitialized ? "text-green-600" : "text-gray-400"}>
                    {stats.cell.workerInitialized ? "●" : "○"}
                  </span>
                </div>
                {stats.cell.workerInitialized && (
                  <div className="space-y-0.5 text-[11px]">
                    <div className="flex justify-between">
                      <span className="text-gray-700">Processed:</span>
                      <span className="font-semibold">{stats.cell.totalProcessed.toLocaleString()}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-700">Unique:</span>
                      <span className="font-semibold text-green-600">{stats.cell.totalUnique.toLocaleString()}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-700">Duplicates:</span>
                      <span className="font-semibold text-red-600">{stats.cell.totalDuplicates.toLocaleString()}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-700">Batches:</span>
                      <span className="font-semibold">{stats.cell.totalBatches}</span>
                    </div>
                    {stats.cell.lastProcessingTime > 0 && (
                      <div className="flex justify-between">
                        <span className="text-gray-700">Last:</span>
                        <span className="font-semibold">
                          {stats.cell.lastProcessingTime.toFixed(2)}ms ({stats.cell.lastUniqueCount} unique,{" "}
                          {stats.cell.lastDuplicateCount} dup)
                        </span>
                      </div>
                    )}
                  </div>
                )}
              </div>
            )}

            {/* SpeedTest Worker Stats */}
            {visibleWorkers.speedtest && (
              <div className="rounded bg-orange-50 p-2">
                <div className="mb-1 flex items-center justify-between font-semibold text-orange-900">
                  <span>⚡ SpeedTest Worker</span>
                  <span className={stats.speedtest.workerInitialized ? "text-green-600" : "text-gray-400"}>
                    {stats.speedtest.workerInitialized ? "●" : "○"}
                  </span>
                </div>
                {stats.speedtest.workerInitialized && (
                  <div className="space-y-0.5 text-[11px]">
                    <div className="flex justify-between">
                      <span className="text-gray-700">Processed:</span>
                      <span className="font-semibold">{stats.speedtest.totalProcessed.toLocaleString()}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-700">Unique:</span>
                      <span className="font-semibold text-green-600">
                        {stats.speedtest.totalUnique.toLocaleString()}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-700">Duplicates:</span>
                      <span className="font-semibold text-red-600">
                        {stats.speedtest.totalDuplicates.toLocaleString()}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-700">Batches:</span>
                      <span className="font-semibold">{stats.speedtest.totalBatches}</span>
                    </div>
                    {stats.speedtest.lastProcessingTime > 0 && (
                      <div className="flex justify-between">
                        <span className="text-gray-700">Last:</span>
                        <span className="font-semibold">
                          {stats.speedtest.lastProcessingTime.toFixed(2)}ms ({stats.speedtest.lastUniqueCount} unique,{" "}
                          {stats.speedtest.lastDuplicateCount} dup)
                        </span>
                      </div>
                    )}
                  </div>
                )}
              </div>
            )}

            {/* Combined Stats */}
            {totalBatches > 0 && (
              <div className="rounded bg-gray-100 p-2">
                <div className="mb-1 font-semibold text-gray-900">📊 Combined</div>
                <div className="space-y-0.5 text-[11px]">
                  <div className="flex justify-between">
                    <span className="text-gray-700">Total Processed:</span>
                    <span className="font-semibold">{totalProcessed.toLocaleString()}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-700">Total Batches:</span>
                    <span className="font-semibold">{totalBatches}</span>
                  </div>
                </div>
              </div>
            )}
          </div>

          <div className="border-t pt-2">
            <div className="mb-1 text-xs font-semibold text-gray-700">Recent Activity:</div>
            <div className="max-h-48 space-y-1 overflow-y-auto rounded bg-gray-50 p-2 text-[10px] font-mono">
              {logs.length === 0 ? (
                <div className="text-gray-400">No activity yet...</div>
              ) : (
                logs.map((log, idx) => (
                  <div key={idx} className="break-words text-gray-700">
                    {log}
                  </div>
                ))
              )}
            </div>
          </div>

          <div className="mt-2 text-[10px] text-gray-500">💡 Open DevTools Console for detailed logs</div>
        </div>
      )}
    </div>
  );
};
