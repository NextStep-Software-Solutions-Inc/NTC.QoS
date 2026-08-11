"use client";

import { useState } from "react";
import { BarChart3, ChevronLeft, Radio } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

// ─── Shared type ────────────────────────────────────────────────────────────
export type LinkFrequencyValues = {
  centerFrequency: number;
  lowerFrequency: number;
  upperFrequency: number;
  rxThreshold: number;
};

type Method = "center-bandwidth" | "upper-lower" | null;

// ─── Helper: frequency badge strip ──────────────────────────────────────────
function FreqBadges({ values }: { values: LinkFrequencyValues }) {
  const base = "px-2.5 py-0.5 rounded-full text-xs font-semibold ring-1 ring-white transition-colors cursor-default";
  return (
    <div className="flex gap-1.5 flex-wrap">
      <Badge className={`${base} bg-red-100 text-red-700 hover:bg-red-500 hover:text-white`}>L: {values.lowerFrequency}</Badge>
      <Badge className={`${base} bg-green-100 text-green-700 hover:bg-green-600 hover:text-white`}>C: {values.centerFrequency}</Badge>
      <Badge className={`${base} bg-blue-100 text-blue-700 hover:bg-blue-500 hover:text-white`}>U: {values.upperFrequency}</Badge>
      <Badge className={`${base} bg-purple-100 text-purple-700 hover:bg-purple-500 hover:text-white`}>RX: {values.rxThreshold} dBm</Badge>
    </div>
  );
}

// ─── Center + Bandwidth sub-form ─────────────────────────────────────────────
function CenterBandwidthForm({
  onConfirm,
  onBack,
  initial,
}: {
  onConfirm: (vals: LinkFrequencyValues) => void;
  onBack: () => void;
  initial?: LinkFrequencyValues;
}) {
  const [center, setCenter] = useState(initial?.centerFrequency?.toString() ?? "");
  const [bandwidth, setBandwidth] = useState(
    initial ? ((initial.upperFrequency - initial.lowerFrequency).toString()) : "",
  );
  const [rx, setRx] = useState(initial?.rxThreshold?.toString() ?? "");
  const [touched, setTouched] = useState(false);

  const centerNum = Number(center);
  const bwNum = Number(bandwidth);
  const rxNum = Number(rx);
  const isValid = center !== "" && bandwidth !== "" && rx !== "" && bwNum > 0;

  const handleConfirm = () => {
    setTouched(true);
    if (!isValid) return;
    onConfirm({
      centerFrequency: centerNum,
      lowerFrequency: centerNum - bwNum / 2,
      upperFrequency: centerNum + bwNum / 2,
      rxThreshold: rxNum,
    });
  };

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <Label>Center Frequency (MHz)</Label>
          <Input
            type="number"
            step="any"
            placeholder="e.g. 1940"
            value={center}
            onChange={(e) => setCenter(e.target.value)}
          />
          {touched && center === "" && <p className="text-xs text-destructive">Required</p>}
        </div>
        <div className="space-y-1.5">
          <Label>Bandwidth (MHz)</Label>
          <Input
            type="number"
            step="any"
            placeholder="e.g. 60"
            value={bandwidth}
            onChange={(e) => setBandwidth(e.target.value)}
          />
          {touched && (bandwidth === "" || bwNum <= 0) && (
            <p className="text-xs text-destructive">Must be &gt; 0</p>
          )}
        </div>
        <div className="space-y-1.5">
          <Label>RX Threshold (dBm)</Label>
          <Input
            type="number"
            step="any"
            placeholder="e.g. -90"
            value={rx}
            onChange={(e) => setRx(e.target.value)}
          />
          {touched && rx === "" && <p className="text-xs text-destructive">Required</p>}
        </div>
      </div>

      {center !== "" && bandwidth !== "" && bwNum > 0 && (
        <div className="rounded-md border bg-muted/40 p-3 space-y-1">
          <p className="text-xs text-muted-foreground font-medium">Computed</p>
          <div className="flex gap-2 flex-wrap text-xs">
            <span className="text-red-700 font-semibold">Lower: {centerNum - bwNum / 2} MHz</span>
            <span className="text-blue-700 font-semibold">Upper: {centerNum + bwNum / 2} MHz</span>
          </div>
        </div>
      )}

      <div className="flex justify-between pt-1">
        <Button variant="ghost" size="sm" onClick={onBack}>
          <ChevronLeft className="h-4 w-4 mr-1" />
          Back
        </Button>
        <Button onClick={handleConfirm}>Confirm</Button>
      </div>
    </div>
  );
}

// ─── Upper + Lower sub-form ──────────────────────────────────────────────────
function UpperLowerForm({
  onConfirm,
  onBack,
  initial,
}: {
  onConfirm: (vals: LinkFrequencyValues) => void;
  onBack: () => void;
  initial?: LinkFrequencyValues;
}) {
  const [lower, setLower] = useState(initial?.lowerFrequency?.toString() ?? "");
  const [upper, setUpper] = useState(initial?.upperFrequency?.toString() ?? "");
  const [rx, setRx] = useState(initial?.rxThreshold?.toString() ?? "");
  const [touched, setTouched] = useState(false);

  const lowerNum = Number(lower);
  const upperNum = Number(upper);
  const rxNum = Number(rx);
  const isValid = lower !== "" && upper !== "" && rx !== "" && upperNum > lowerNum;

  const handleConfirm = () => {
    setTouched(true);
    if (!isValid) return;
    onConfirm({
      lowerFrequency: lowerNum,
      upperFrequency: upperNum,
      centerFrequency: (lowerNum + upperNum) / 2,
      rxThreshold: rxNum,
    });
  };

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <Label>Lower Frequency (MHz)</Label>
          <Input
            type="number"
            step="any"
            placeholder="e.g. 1710"
            value={lower}
            onChange={(e) => setLower(e.target.value)}
          />
          {touched && lower === "" && <p className="text-xs text-destructive">Required</p>}
        </div>
        <div className="space-y-1.5">
          <Label>Upper Frequency (MHz)</Label>
          <Input
            type="number"
            step="any"
            placeholder="e.g. 1785"
            value={upper}
            onChange={(e) => setUpper(e.target.value)}
          />
          {touched && (upper === "" || upperNum <= lowerNum) && (
            <p className="text-xs text-destructive">Must be &gt; lower</p>
          )}
        </div>
        <div className="space-y-1.5">
          <Label>RX Threshold (dBm)</Label>
          <Input
            type="number"
            step="any"
            placeholder="e.g. -90"
            value={rx}
            onChange={(e) => setRx(e.target.value)}
          />
          {touched && rx === "" && <p className="text-xs text-destructive">Required</p>}
        </div>
      </div>

      {lower !== "" && upper !== "" && upperNum > lowerNum && (
        <div className="rounded-md border bg-muted/40 p-3 space-y-1">
          <p className="text-xs text-muted-foreground font-medium">Computed</p>
          <span className="text-xs text-green-700 font-semibold">
            Center: {(lowerNum + upperNum) / 2} MHz
          </span>
        </div>
      )}

      <div className="flex justify-between pt-1">
        <Button variant="ghost" size="sm" onClick={onBack}>
          <ChevronLeft className="h-4 w-4 mr-1" />
          Back
        </Button>
        <Button onClick={handleConfirm}>Confirm</Button>
      </div>
    </div>
  );
}

// ─── Public dialog ────────────────────────────────────────────────────────────
interface LinkFrequencyConfigDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Which link type is being configured — used only for the title */
  linkType: "Uplink" | "Downlink";
  /** Pre-fill from an already-configured link */
  initial?: LinkFrequencyValues;
  onConfirm: (values: LinkFrequencyValues) => void;
}

export default function LinkFrequencyConfigDialog({
  open,
  onOpenChange,
  linkType,
  initial,
  onConfirm,
}: LinkFrequencyConfigDialogProps) {
  const [method, setMethod] = useState<Method>(null);

  const handleClose = () => {
    setMethod(null);
    onOpenChange(false);
  };

  const handleConfirm = (vals: LinkFrequencyValues) => {
    onConfirm(vals);
    setMethod(null);
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o) handleClose(); }}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Configure {linkType}</DialogTitle>
        </DialogHeader>

        {!method && (
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground">Select an input method</p>
            <div className="space-y-2">
              <button
                onClick={() => setMethod("center-bandwidth")}
                className="w-full flex items-center gap-3 p-4 border rounded-lg hover:bg-muted/50 transition-colors text-left">
                <Radio className="h-5 w-5 text-muted-foreground shrink-0" />
                <div>
                  <p className="font-medium text-sm">Center Frequency + Bandwidth</p>
                  <p className="text-xs text-muted-foreground">Computes lower and upper from center &amp; bandwidth</p>
                </div>
              </button>
              <button
                onClick={() => setMethod("upper-lower")}
                className="w-full flex items-center gap-3 p-4 border rounded-lg hover:bg-muted/50 transition-colors text-left">
                <BarChart3 className="h-5 w-5 text-muted-foreground shrink-0" />
                <div>
                  <p className="font-medium text-sm">Upper + Lower Frequency</p>
                  <p className="text-xs text-muted-foreground">Computes center from upper and lower bounds</p>
                </div>
              </button>
            </div>
            <div className="flex justify-end pt-1">
              <Button variant="outline" onClick={handleClose}>Cancel</Button>
            </div>
          </div>
        )}

        {method === "center-bandwidth" && (
          <CenterBandwidthForm
            onConfirm={handleConfirm}
            onBack={() => setMethod(null)}
            initial={initial}
          />
        )}

        {method === "upper-lower" && (
          <UpperLowerForm
            onConfirm={handleConfirm}
            onBack={() => setMethod(null)}
            initial={initial}
          />
        )}
      </DialogContent>
    </Dialog>
  );
}

// Re-export helper for use in the main form
export { FreqBadges };
