import React from "react";
import { CheckCircle, Plus } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";

interface CreatedProviderDialogProps {
  onNext: () => void;
  onPrev?: () => void;
}

export default function ProviderCreated({ onNext, onPrev }: CreatedProviderDialogProps) {
  return (
    <div className="p-6 max-w-md mx-auto bg-white rounded-lg shadow-md border">
      {/* Success header */}
      <div className="flex items-center space-x-2 mb-4">
        <CheckCircle className="text-green-500" size={24} />
        <h2 className="text-lg font-semibold">Provider created successfully!</h2>
      </div>

      <p className="text-sm text-muted-foreground mb-4">
        You have successfully created a provider. You can now add services for this provider.
      </p>

      {/* Provider card */}
      <Card className="mb-4">
        <CardContent className="flex items-center justify-between p-4">
          <div className="flex items-center space-x-3">
            <div className="w-8 h-8 rounded-full bg-gray-300 flex items-center justify-center font-semibold text-sm"></div>
            <div>
              <Badge variant="secondary" className="mb-1">
                Step Jaramillo
              </Badge>
              <p className="text-xs text-muted-foreground">Provider Step</p>
            </div>
          </div>
          <div className="text-sm font-medium">Test</div>
        </CardContent>
      </Card>

      {/* Action buttons */}
      <div className="flex justify-end space-x-2">
        <Button variant="ghost" onClick={onPrev}>
          Close
        </Button>
        <Button onClick={onNext}>
          <Plus /> Add Services
        </Button>
      </div>
    </div>
  );
}
