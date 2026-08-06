import React, { Suspense } from "react";

import "@/styles/globals.css";

import { Inter } from "next/font/google";
import { ClerkProvider } from "@clerk/nextjs";
import { Analytics } from "@vercel/analytics/next";

import { CookieConsentBanner } from "@/components/CookieConsentBanner";
import { Toaster } from "@/components/ui/sonner";
import { TooltipProvider } from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";
import QueryClientProvider from "@/providers/QueryClientProvider";
import { FloatingAIChatButton } from "@/components/FloatingAIChatButton";

const inter = Inter({ subsets: ["latin"] });

const PUBLISHABLE_KEY = process.env.NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY;

function PublicMapLayout({ children }: { children: React.ReactNode }) {
  return (
    <ClerkProvider publishableKey={PUBLISHABLE_KEY} telemetry={false}>
      <html className={cn(inter.className, "scroll-smooth")} lang="en">
        <body className="overscroll-none">
          <Suspense fallback={null}>
            <QueryClientProvider>
              <Toaster richColors visibleToasts={3} expand closeButton />
              <TooltipProvider>{children}</TooltipProvider>
              <CookieConsentBanner />
              <FloatingAIChatButton />
              <Analytics />
            </QueryClientProvider>
          </Suspense>
        </body>
      </html>
    </ClerkProvider>
  );
}

export default PublicMapLayout;
