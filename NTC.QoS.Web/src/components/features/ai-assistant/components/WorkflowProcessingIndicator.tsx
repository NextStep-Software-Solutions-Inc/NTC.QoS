"use client";

import { motion } from "framer-motion";
import { Bot, Workflow, Zap } from "lucide-react";

const workflowMessages = [
    "Processing workflow...",
    "Analyzing your request...",
    "Working on your task...",
    "Executing workflow steps...",
];

export function WorkflowProcessingIndicator({ progress }: { progress?: number }) {
    return (
        <div className="flex gap-3 mb-6">
            {/* Avatar */}
            <div className="flex-shrink-0 h-10 w-10 rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center relative overflow-hidden">
                <motion.div
                    className="absolute inset-0 bg-gradient-to-r from-transparent via-white/20 to-transparent"
                    animate={{ x: ["-100%", "100%"] }}
                    transition={{ duration: 1.5, repeat: Infinity, ease: "linear" }}
                />
                <Bot className="h-5 w-5 text-white relative z-10" />
            </div>

            {/* Workflow Processing Animation */}
            <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 px-5 py-4 rounded-2xl rounded-tl-sm shadow-sm max-w-[280px]">
                <div className="flex items-center gap-3">
                    {/* Animated workflow icon */}
                    <motion.div
                        className="relative"
                        animate={{ rotate: [0, 10, -10, 0] }}
                        transition={{ duration: 2, repeat: Infinity, ease: "easeInOut" }}
                    >
                        <Workflow className="h-5 w-5 text-blue-500" />
                        <motion.div
                            className="absolute -top-1 -right-1"
                            animate={{
                                opacity: [0, 1, 0],
                                scale: [0.8, 1.2, 0.8],
                            }}
                            transition={{ duration: 1.5, repeat: Infinity }}
                        >
                            <Zap className="h-3 w-3 text-yellow-500" />
                        </motion.div>
                    </motion.div>

                    {/* Workflow processing text with animated dots */}
                    <div className="flex items-center gap-1">
                        <motion.span
                            className="text-sm text-gray-600 dark:text-gray-400 font-medium"
                            initial={{ opacity: 0 }}
                            animate={{ opacity: 1 }}
                            transition={{ duration: 0.3 }}
                        >
                            Processing workflow
                        </motion.span>
                        <span className="flex">
                            {[0, 1, 2].map((i) => (
                                <motion.span
                                    key={i}
                                    className="text-gray-600 dark:text-gray-400 font-medium"
                                    animate={{ opacity: [0, 1, 0] }}
                                    transition={{
                                        duration: 1.2,
                                        repeat: Infinity,
                                        delay: i * 0.2,
                                    }}
                                >
                                    .
                                </motion.span>
                            ))}
                        </span>
                    </div>
                </div>

                {/* Progress bar with actual progress */}
                <div className="mt-3 h-1 bg-gray-100 dark:bg-gray-700 rounded-full overflow-hidden">
                    <motion.div
                        className="h-full bg-gradient-to-r from-blue-500 via-indigo-500 to-purple-500"
                        initial={{ width: "0%" }}
                        animate={{ width: progress !== undefined ? `${progress}%` : "100%" }}
                        transition={progress !== undefined ? { duration: 0.3 } : { duration: 2, repeat: Infinity, ease: "easeInOut" }}
                    />
                </div>

                {/* Progress text */}
                {progress !== undefined && (
                    <div className="mt-2 text-xs text-gray-500 dark:text-gray-400 text-center">
                        {progress}% complete
                    </div>
                )}
            </div>
        </div>
    );
}