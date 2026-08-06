"use client";

import Link from "next/link";
import { motion } from "framer-motion";
import { Cookie } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardFooter } from "@/components/ui/card";
import { useCookieConsent } from "@/hooks/useCookieConsent";

export function CookieConsentBanner() {
  const { hasResponded, isLoading, acceptCookies, declineCookies } = useCookieConsent();

  // Don't show banner if user has already responded or still loading
  if (isLoading || hasResponded) {
    return null;
  }

  return (
    <motion.div
      initial={{ y: 100, opacity: 0 }}
      animate={{ y: 0, opacity: 1 }}
      exit={{ y: 100, opacity: 0 }}
      transition={{ type: "spring", stiffness: 300, damping: 30 }}
      className="fixed bottom-4 left-4 right-4 z-[100] mx-auto max-w-screen-md md:bottom-6 md:left-6 md:right-6"
      role="dialog"
      aria-live="polite"
      aria-label="Cookie consent notice">
      <Card className="border-2 shadow-2xl">
        <CardContent className="flex flex-col gap-4 p-6 sm:flex-row sm:items-start sm:gap-6">
          <div className="flex shrink-0 items-center justify-center">
            <div className="rounded-full bg-primary/10 p-3">
              <Cookie className="h-6 w-6 text-primary" />
            </div>
          </div>
          <div className="flex-1 space-y-2">
            <h3 className="text-lg font-semibold leading-none tracking-tight">Cookie Notice</h3>
            <p className="text-sm text-muted-foreground">
              We use cookies to enhance your browsing experience, serve personalized content, and analyze our traffic.
              By clicking &quot;Accept&quot;, you consent to our use of cookies.{" "}
              <Link href="/help-and-support" className="underline underline-offset-4 hover:text-primary">
                Learn more
              </Link>
            </p>
          </div>
        </CardContent>
        <CardFooter className="flex flex-col gap-2 sm:flex-row sm:justify-end">
          <Button variant="outline" onClick={declineCookies} className="w-full sm:w-auto">
            Decline
          </Button>
          <Button onClick={acceptCookies} className="w-full sm:w-auto">
            Accept
          </Button>
        </CardFooter>
      </Card>
    </motion.div>
  );
}
