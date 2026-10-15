import { Loader } from "lucide-react";

import { Button, type ButtonProps } from "@/components/ui/button";
import { cn } from "@/lib/utils";

interface SubmitButtonProps extends ButtonProps {
  loading?: boolean;
}
export default function SubmitButton({ loading, ...props }: SubmitButtonProps) {
  return (
    <Button {...props} disabled={loading ?? props.disabled}>
      <Loader className={cn("hidden animate-spin transition-all", { block: loading })} />
      {props.children}
    </Button>
  );
}
