"use client";

import React from "react";
import Link from "next/link";
import { ChevronRight } from "lucide-react";

import { Button } from "@/components/ui/button";

export default function HeroSection() {
  return (
    <section
      id="hero"
      className="flex min-h-screen flex-col items-center justify-center text-white"
      style={{
        background: `
          linear-gradient(
            to bottom,
            rgba(0,0,0,0.25) 0%, 
            rgba(0,0,0,0) 15%
          ),
          linear-gradient(
            to bottom,
            #0C43D1 0%, 
            #DE0E23 100%
          )
        `,
      }}>
      <div className="container mx-auto flex max-w-7xl flex-col items-center justify-center gap-6 px-4 py-16 pt-[180px]">
        <h1 className="text-center text-5xl sm:text-6xl lg:text-[80px] font-bold tracking-tight leading-[1.1]">
          Philippine National <br className="hidden md:block" /> Broadband Map
        </h1>

        <div className="container flex flex-col items-center justify-center gap-8 sm:gap-12">
          <div className="max-w-[800px]">
            <p className="text-center text-lg sm:text-xl md:text-2xl font-medium text-white/90">
              Check broadband availability, providers, and network <br className="hidden sm:block" />
              performance across the Philippines.
            </p>
          </div>

          <div>
            <Link href="/public/map">
              <Button className="flex items-center gap-2 bg-white text-black transition-colors duration-300 hover:bg-black hover:text-white">
                Explore Map
                <ChevronRight />
              </Button>
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}
