import React from "react";
import { ResponsiveDialog } from "@/components/ResponsiveDialog";
import { ChatInterface } from "../components/ChatInterface";

interface AIChatDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export default function AIChatDialog({ open, onOpenChange }: AIChatDialogProps) {
  return (
    <ResponsiveDialog
      open={open}
      onOpenChange={onOpenChange}
      title="AI Chat Assistant"
      description="Ask questions about broadband coverage, service quality, and telecommunications data."
      size="wide"
      className="h-[90vh] max-h-[90vh] overflow-hidden"
      preventOutsideClick={true}
    >
      <div className="h-full flex flex-col min-h-0">
        <ChatInterface />
      </div>
    </ResponsiveDialog>
  );
}
