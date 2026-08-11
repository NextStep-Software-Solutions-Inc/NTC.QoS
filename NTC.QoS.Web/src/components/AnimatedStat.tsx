"use client";

import React from "react";

import { animateNumber, parseShortNumber } from "@/lib/animated-number";

interface AnimatedStatProps {
  start?: number;
  end: string;
  duration?: number;
}
export default function AnimatedStat({ start = 0, end, duration = 5000 }: AnimatedStatProps) {
  const [value, suffix] = parseShortNumber(end);
  const [displayValue, setDisplayValue] = React.useState(start);

  React.useEffect(() => {
    return animateNumber(value, duration, setDisplayValue);
  }, [value, duration]);

  return (
    <>
      {displayValue}
      {suffix}
    </>
  );
}
