import { useState } from "react";
import { Plus } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export function AddServiceWithProviderDialog({ providerId: _providerId }: { providerId?: string }) {
  const [name, setName] = useState("");
  const [serviceType, setServiceType] = useState("Wired");

  return (
    <Dialog>
      <form>
        <DialogTrigger asChild>
          <Button variant="outline">
            <Plus className="mx-1" /> Add Service
          </Button>
        </DialogTrigger>
        <DialogContent className="w-fit">
          <DialogTitle className="text-[16px] font-semibold mb-[-.5rem]">Create other service type</DialogTitle>
          <div className="flex  items-center justify-between space-x-3">
            <section className="grid gap-3">
              <Input
                id="name"
                name="name"
                placeholder="Enter service name"
                value={name}
                onChange={(e) => setName(e.target.value)}
              />
            </section>
            <section className="flex items-center gap-x-1">
              <Input
                type="radio"
                id="description-1"
                name="description"
                placeholder="Enter service description"
                className="w-5 h-5 rounded-md"
                checked={serviceType === "Wired"}
                onChange={() => setServiceType("Wired")}
              />
              <Label htmlFor="description-1">Wired</Label>
            </section>
            <section className="flex justify-between items-center gap-x-1">
              <Input
                type="radio"
                id="description-2"
                name="description"
                placeholder="Enter service description"
                className="w-5 h-5 rounded-md"
                checked={serviceType === "Wireless"}
                onChange={() => setServiceType("Wireless")}
              />
              <Label htmlFor="description-2">Wireless</Label>
            </section>
          </div>
          <Button className="w-full">Add</Button>
        </DialogContent>
      </form>
    </Dialog>
  );
}
