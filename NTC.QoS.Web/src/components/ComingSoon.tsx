"use client";

import React from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft, Hourglass } from "lucide-react";

import { Button } from "@/components/ui/button";

interface ComingSoonProps {
  title?: string;
  description?: string;
  icon?: React.ReactNode;
  onBack?: () => void;
}

export default function ComingSoon({
  title = "Coming Soon..",
  description = "This feature is not available yet, but we’re working on it. Please check back later for updates.",
  icon = <Hourglass className="w-12 h-12 text-yellow-600" />,
  onBack,
}: ComingSoonProps) {
  const router = useRouter();

  const handleBack = () => {
    if (onBack) {
      onBack();
    } else {
      router.push("/"); // go to homepage
    }
  };

  return (
    <main className="min-h-screen flex flex-col items-center justify-center p-6 bg-gray-50">
      <div className="flex flex-col items-center text-center">
        <div className="bg-white p-10 rounded-2xl shadow-xl max-w-lg">
          <div className="flex justify-center mb-6">
            <div className="p-4 bg-yellow-100 rounded-full">{icon}</div>
          </div>
          <h1 className="text-3xl font-bold mb-4">{title}</h1>
          <p className="text-gray-600 mb-6">{description}</p>
          <Button variant="outline" className="flex items-center gap-2" onClick={handleBack}>
            <ArrowLeft className="w-4 h-4" /> Go Back
          </Button>
        </div>
      </div>
    </main>
  );
}
