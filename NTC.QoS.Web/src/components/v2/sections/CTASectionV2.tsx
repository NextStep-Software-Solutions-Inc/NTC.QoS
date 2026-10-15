"use client";

import Link from "next/link";
import { CheckCircle2 } from "lucide-react";

import MapIllustration from "../illustrations/MapIllustration";

const benefits = [
  "Real-time coverage data across all Philippine regions",
  "Comprehensive speed test analytics and trends",
  "Transparent ISP performance comparisons",
  "Interactive maps with advanced filtering",
  "Regular updates from official NTC sources",
  "Mobile-optimized experience for on-the-go access",
];

export default function CTASectionV2() {
  return (
    <section className="py-20 lg:py-32 bg-gradient-to-br from-gray-50 to-yellow-50 relative overflow-hidden">
      {/* Background decoration */}
      <div className="absolute top-0 left-0 w-96 h-96 bg-red-200 rounded-full blur-3xl opacity-20 -z-10" />
      <div className="absolute bottom-0 right-0 w-96 h-96 bg-blue-200 rounded-full blur-3xl opacity-20 -z-10" />

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="grid lg:grid-cols-2 gap-12 items-center">
          {/* Content */}
          <div className="space-y-8">
            <div className="inline-flex items-center gap-2 px-4 py-2 bg-red-100 text-red-700 rounded-full text-sm font-medium">
              Free & Official
            </div>

            <h2 className="text-3xl sm:text-4xl lg:text-5xl font-bold text-gray-900 leading-tight">
              Start Exploring{" "}
              <span className="bg-gradient-to-r from-red-600 to-yellow-600 bg-clip-text text-transparent">
                Broadband Data
              </span>{" "}
              Today
            </h2>

            <p className="text-lg text-gray-600 leading-relaxed">
              Access the most comprehensive and up-to-date broadband coverage information in the Philippines. No
              registration required for public access.
            </p>

            {/* Benefits list */}
            <ul className="space-y-4">
              {benefits.map((benefit, index) => (
                <li key={index} className="flex items-start gap-3">
                  <CheckCircle2 className="w-6 h-6 text-red-600 flex-shrink-0 mt-0.5" />
                  <span className="text-gray-700">{benefit}</span>
                </li>
              ))}
            </ul>

            {/* CTA Buttons */}
            <div className="flex flex-col sm:flex-row gap-4 pt-4">
              <Link
                href="/public/map"
                className="inline-flex items-center justify-center px-8 py-4 bg-gradient-to-r from-red-600 to-red-700 text-white rounded-xl font-semibold shadow-lg hover:shadow-xl transform hover:-translate-y-0.5 transition-all duration-200">
                View Coverage Map
              </Link>

              <Link
                href="/about-us"
                className="inline-flex items-center justify-center px-8 py-4 bg-white text-gray-700 rounded-xl font-semibold border-2 border-gray-200 hover:border-red-300 hover:bg-red-50 transition-all duration-200">
                Learn More
              </Link>
            </div>
          </div>

          {/* Illustration */}
          <div className="relative">
            <div className="relative bg-white rounded-3xl shadow-2xl p-8 border border-gray-100">
              <div className="aspect-square bg-gradient-to-br from-red-50 to-yellow-50 rounded-2xl flex items-center justify-center overflow-hidden">
                <MapIllustration />
              </div>

              {/* Floating badges */}
              <div className="absolute -bottom-4 -right-4 bg-white rounded-xl shadow-lg px-4 py-3 animate-float animation-delay-2000">
                <div className="flex items-center gap-2">
                  <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse" />
                  <span className="text-sm font-semibold text-gray-700">Updated Daily</span>
                </div>
              </div>
            </div>

            {/* Decorative elements */}
            <div className="absolute -z-10 inset-0 bg-gradient-to-br from-red-500 to-yellow-600 rounded-3xl transform rotate-6 opacity-10 blur-xl" />
          </div>
        </div>
      </div>
    </section>
  );
}
