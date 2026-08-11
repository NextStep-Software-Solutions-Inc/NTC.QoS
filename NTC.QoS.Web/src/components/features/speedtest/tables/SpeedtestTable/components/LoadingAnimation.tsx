"use client";

import { motion } from "framer-motion";
import { Activity } from "lucide-react";

export function LoadingAnimation() {
  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="flex flex-col items-center justify-center py-20">
      <div className="relative">
        {/* Outer ring */}
        <motion.div
          animate={{ rotate: 360 }}
          transition={{ duration: 3, repeat: Infinity, ease: "linear" }}
          className="absolute inset-0 rounded-full border-4 border-transparent border-t-cyan-500 border-r-blue-500"
          style={{ width: 80, height: 80 }}
        />

        {/* Middle ring */}
        <motion.div
          animate={{ rotate: -360 }}
          transition={{ duration: 2, repeat: Infinity, ease: "linear" }}
          className="absolute inset-2 rounded-full border-4 border-transparent border-t-blue-500 border-r-purple-500"
          style={{ width: 64, height: 64 }}
        />

        {/* Center icon */}
        <div className="flex items-center justify-center w-20 h-20">
          <motion.div
            animate={{ scale: [1, 1.1, 1] }}
            transition={{ duration: 1.5, repeat: Infinity }}
            className="w-10 h-10 rounded-full bg-gradient-to-br from-cyan-500 to-blue-600 flex items-center justify-center shadow-lg">
            <Activity className="w-5 h-5 text-white" />
          </motion.div>
        </div>
      </div>

      <motion.div
        initial={{ opacity: 0, y: 10 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.2 }}
        className="mt-6 text-center">
        <h3 className="text-lg font-semibold bg-gradient-to-r from-cyan-600 to-blue-600 bg-clip-text text-transparent">
          Loading Speedtests
        </h3>
        <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">Fetching performance data...</p>
      </motion.div>

      {/* Animated dots */}
      <div className="flex items-center gap-1 mt-4">
        {[0, 1, 2].map((i) => (
          <motion.div
            key={i}
            animate={{ scale: [1, 1.5, 1], backgroundColor: ["#06b6d4", "#3b82f6", "#06b6d4"] }}
            transition={{ duration: 0.8, repeat: Infinity, delay: i * 0.2 }}
            className="w-2 h-2 rounded-full bg-cyan-500"
          />
        ))}
      </div>
    </motion.div>
  );
}
