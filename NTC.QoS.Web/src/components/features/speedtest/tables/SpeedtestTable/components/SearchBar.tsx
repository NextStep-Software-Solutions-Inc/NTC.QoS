"use client";

import { useEffect, useState } from "react";
import { motion } from "framer-motion";
import { Search, X } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

interface SearchBarProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  className?: string;
  debounceMs?: number;
  helpText?: string;
}

export function SearchBar({ 
  value, 
  onChange, 
  placeholder = "Search provider, address, or phone model...", 
  className = "",
  debounceMs = 300,
  helpText = "Searches: Provider name, Address, Phone model"
}: SearchBarProps) {
  // Local state for immediate UI updates
  const [localValue, setLocalValue] = useState(value);

  // Sync local value when external value changes
  useEffect(() => {
    setLocalValue(value);
  }, [value]);

  // Debounce the onChange callback
  useEffect(() => {
    const timer = setTimeout(() => {
      if (localValue !== value) {
        onChange(localValue);
      }
    }, debounceMs);

    return () => clearTimeout(timer);
  }, [localValue, onChange, debounceMs, value]);

  const handleClear = () => {
    setLocalValue("");
    onChange(""); // Clear immediately without debounce
  };
  return (
    <motion.div
      initial={{ opacity: 0, y: -10 }}
      animate={{ opacity: 1, y: 0 }}
      className={`relative group ${className}`}>
      <div className="absolute -inset-0.5 bg-gradient-to-r from-cyan-500 via-blue-500 to-purple-500 rounded-lg blur opacity-0 group-hover:opacity-30 transition duration-300" />
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 h-4 w-4 transition-colors group-hover:text-blue-500" />
        <Input
          placeholder={placeholder}
          value={localValue}
          onChange={(e) => setLocalValue(e.target.value)}
          className="pl-10 h-10 bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700 focus:border-blue-500 focus:ring-blue-500/20 transition-all"
          title={helpText}
        />
        {localValue && (
          <Button
            variant="ghost"
            size="sm"
            className="absolute right-1 top-1/2 -translate-y-1/2 h-7 w-7 p-0 hover:bg-red-100 hover:text-red-600 dark:hover:bg-red-900/30"
            onClick={handleClear}>
            <X className="h-4 w-4" />
          </Button>
        )}
      </div>
      {localValue && (
        <motion.p 
          initial={{ opacity: 0, height: 0 }}
          animate={{ opacity: 1, height: "auto" }}
          exit={{ opacity: 0, height: 0 }}
          className="text-xs text-gray-500 dark:text-gray-400 mt-1">
          {helpText}
        </motion.p>
      )}
    </motion.div>
  );  
}
