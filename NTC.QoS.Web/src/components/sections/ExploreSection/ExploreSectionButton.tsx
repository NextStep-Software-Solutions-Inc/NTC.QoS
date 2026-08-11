"use client";

import React from "react";
import Link from "next/link";

import { Button } from "@/components/ui/button";

function ExploreSectionButton() {
  return (
    <Link href="/coverage-map" passHref>
      <Button
        variant="default"
        className="bg-[#003584] text-white font-medium px-8 py-3 rounded-md text-base transition-all duration-200 ease-in-out hover:opacity-90 hover:-translate-y-0.5">
        Get Started
      </Button>
    </Link>
  );
}

export default ExploreSectionButton;
