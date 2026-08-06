"use client";

import { motion } from "framer-motion";
import { RadioTower } from "lucide-react";

export function LoadingAnimation() {
  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      className="flex flex-col items-center justify-center py-20">
      <div className="relative mb-8">
        {/* Central tower */}
        <motion.div
          className="relative z-10"
          animate={{ scale: [1, 1.05, 1] }}
          transition={{ duration: 2, repeat: Infinity }}>
          <RadioTower className="h-24 w-24 text-blue-500" />
        </motion.div>

        {/* Animated signal waves - bigger and more obvious - perfect circles */}
        <motion.div
          className="absolute inset-0 flex items-center justify-center"
          initial={{ scale: 0.15, opacity: 0 }}
          animate={{ scale: 2.5, opacity: [0, 0.8, 0] }}
          transition={{ duration: 3, repeat: Infinity, ease: [0.4, 0, 0.6, 1], delay: 0, times: [0, 0.5, 1] }}>
          <div className="w-32 h-32 aspect-square rounded-full border-2 border-blue-400/70" />
        </motion.div>
        <motion.div
          className="absolute inset-0 flex items-center justify-center"
          initial={{ scale: 0.15, opacity: 0 }}
          animate={{ scale: 2.5, opacity: [0, 0.8, 0] }}
          transition={{ duration: 3, repeat: Infinity, ease: [0.4, 0, 0.6, 1], delay: 1, times: [0, 0.5, 1] }}>
          <div className="w-32 h-32 aspect-square rounded-full border-2 border-indigo-400/70" />
        </motion.div>
        <motion.div
          className="absolute inset-0 flex items-center justify-center"
          initial={{ scale: 0.15, opacity: 0 }}
          animate={{ scale: 2.5, opacity: [0, 0.8, 0] }}
          transition={{ duration: 3, repeat: Infinity, ease: [0.4, 0, 0.6, 1], delay: 2, times: [0, 0.5, 1] }}>
          <div className="w-32 h-32 aspect-square rounded-full border-2 border-purple-400/70" />
        </motion.div>
      </div>

      <motion.p
        className="text-xl font-semibold text-gray-600 dark:text-gray-400"
        animate={{ opacity: [0.5, 1, 0.5] }}
        transition={{ duration: 1.5, repeat: Infinity }}>
        Scanning cell sites...
      </motion.p>
      <p className="text-sm text-gray-500 dark:text-gray-500 mt-2">Fetching tower data from the network</p>
    </motion.div>
  );
}
