/**
 * usePopupPositioner
 *
 * Measures the trigger button and the popup at open time, then computes a
 * `position: fixed` style that:
 *   • always places the popup to the LEFT of the trigger button
 *   • vertically centres the popup on the button
 *   • clamps left, top, and bottom so the popup never bleeds off any viewport edge
 *   • re-runs the calculation whenever the window is resized
 *
 * Usage:
 *   const { btnRef, popupRef, popupStyle } = usePopupPositioner(isOpen);
 *
 *   // attach to trigger button
 *   <button ref={btnRef} …>
 *
 *   // attach to popup container (motion.div or plain div)
 *   <div ref={popupRef} style={{ ...popupStyle, zIndex: Z }}>
 */

"use client";

import { useCallback, useEffect, useLayoutEffect, useRef, useState } from "react";
import type { CSSProperties, RefObject } from "react";

// NOTE: isMounted is intentionally exposed so callers can guard ReactDOM.createPortal
// against SSR — createPortal requires document.body to exist.

const MARGIN = 8; // minimum distance from viewport edge
const GAP = 8; // gap between button left edge and popup right edge

export interface PopupPositionerResult {
  btnRef: RefObject<HTMLButtonElement | null>;
  popupRef: RefObject<HTMLDivElement | null>;
  popupStyle: CSSProperties;
  /** True only after client-side mount — use to guard createPortal calls */
  isMounted: boolean;
}

export function usePopupPositioner(isOpen: boolean): PopupPositionerResult {
  const btnRef = useRef<HTMLButtonElement>(null);
  const popupRef = useRef<HTMLDivElement>(null);

  // isMounted becomes true only after client-side hydration so callers can
  // safely call createPortal(content, document.body).
  const [isMounted, setIsMounted] = useState(false);
  useEffect(() => {
    setIsMounted(true);
  }, []);

  // Start off-screen so there is no flash before the first reposition.
  // Uses `right` (not `left`) so the calculation never needs popupWidth,
  // which avoids the common case where offsetWidth is 0 on first mount.
  const [popupStyle, setPopupStyle] = useState<CSSProperties>({
    position: "fixed",
    right: -9999,
    top: -9999,
  });

  const reposition = useCallback(() => {
    if (!btnRef.current || !popupRef.current) return;

    const btnRect = btnRef.current.getBoundingClientRect();
    const popupHeight = popupRef.current.offsetHeight;
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;

    // Place the popup's right edge GAP pixels to the LEFT of the button's
    // left edge. Using CSS `right` here means we never need popupWidth,
    // which may still be 0 if the element hasn't laid out yet.
    const right = viewportWidth - btnRect.left + GAP;

    // If popup height hasn't been computed yet, skip vertical positioning
    // and wait for the RAF retry to fire with the real dimensions.
    if (popupHeight === 0) return;

    // Vertically centre on the button; clamp within viewport
    let top = btnRect.top + btnRect.height / 2 - popupHeight / 2;
    if (top < MARGIN) top = MARGIN;
    if (top > viewportHeight - popupHeight - MARGIN) {
      top = viewportHeight - popupHeight - MARGIN;
    }

    setPopupStyle({ position: "fixed", right, top });
  }, []);

  // Run synchronously after the popup DOM node has been laid out so that
  // offsetWidth / offsetHeight reflect the actual rendered dimensions.
  useLayoutEffect(() => {
    if (!isOpen) return;

    reposition();

    // Retry on the next animation frame so refs and measured dimensions are
    // available even when the popup mounts through animation wrappers.
    const frameId = window.requestAnimationFrame(() => {
      reposition();
    });

    return () => window.cancelAnimationFrame(frameId);
  }, [isOpen, reposition]);

  // Re-position whenever the viewport resizes while the popup is open
  useEffect(() => {
    if (!isOpen) return;

    const popup = popupRef.current;
    const resizeObserver =
      popup && typeof ResizeObserver !== "undefined" ? new ResizeObserver(() => reposition()) : null;

    window.addEventListener("resize", reposition);
    if (popup && resizeObserver) {
      resizeObserver.observe(popup);
    }

    return () => {
      window.removeEventListener("resize", reposition);
      resizeObserver?.disconnect();
    };
  }, [isOpen, reposition]);

  return { btnRef, popupRef, popupStyle, isMounted };
}
