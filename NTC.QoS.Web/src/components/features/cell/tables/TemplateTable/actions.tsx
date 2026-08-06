import { Copy, Eye, MoreHorizontal } from "lucide-react";
import { toast } from "sonner";

import { type Template } from "@/components/features/Template/types/Template";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

interface ActionsProps {
  payment: Template;
  readonly?: boolean;
}

export default function Actions({ payment, readonly }: ActionsProps) {
  const handleCopyName = (name: string) => {
    void navigator.clipboard.writeText(name);
    toast.info("Copied to clipboard");
  };

  return (
    <>
      <DropdownMenu modal={false}>
        <DropdownMenuTrigger asChild>
          <Button variant="ghost" className="h-8 w-8 p-0">
            <span className="sr-only">Open menu</span>
            <MoreHorizontal />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          <DropdownMenuLabel>Actions</DropdownMenuLabel>
          <DropdownMenuItem onClick={() => handleCopyName(payment.id)}>
            <Copy />
            Copy payment ID
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem>
            <Eye />
            View customer
          </DropdownMenuItem>
          <DropdownMenuItem>
            <Eye />
            View payment details
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
      {!readonly && <Button variant="ghost">Edit</Button>}
    </>
  );
}
