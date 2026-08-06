"use client";

import { useEffect, useState } from "react";

const COOKIE_CONSENT_KEY = "cookie_consent";

export type CookieConsentValue = "accepted" | "declined" | null;

export function useCookieConsent() {
  const [consent, setConsent] = useState<CookieConsentValue>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Check if consent has been given before
    try {
      const storedConsent = localStorage.getItem(COOKIE_CONSENT_KEY);
      if (storedConsent === "accepted" || storedConsent === "declined") {
        setConsent(storedConsent);
      }
    } catch (error) {
      // localStorage may not be available (e.g., incognito mode, disabled cookies)
      console.warn("Unable to access localStorage:", error);
    }
    setIsLoading(false);
  }, []);

  const updateConsent = (value: CookieConsentValue) => {
    try {
      if (value === null) {
        localStorage.removeItem(COOKIE_CONSENT_KEY);
      } else {
        localStorage.setItem(COOKIE_CONSENT_KEY, value);
      }
      setConsent(value);
    } catch (error) {
      console.warn("Unable to update consent in localStorage:", error);
      // Still update the state even if localStorage fails
      setConsent(value);
    }
  };

  const acceptCookies = () => {
    updateConsent("accepted");
  };

  const declineCookies = () => {
    updateConsent("declined");
  };

  const resetConsent = () => {
    updateConsent(null);
  };

  return { consent, isLoading, acceptCookies, declineCookies, resetConsent, hasResponded: consent !== null };
}
