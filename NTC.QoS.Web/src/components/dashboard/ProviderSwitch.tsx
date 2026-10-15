import { useEffect } from "react";
import { useSearchParams } from "next/navigation";

import { cn } from "@/lib/utils";
import { Switch } from "../ui/switch";

interface ProviderSwitchProps {
  type: "cell" | "NAP" | "service" | "speedtests" | "";
  id: string;
  name: string;
  icon: React.ReactNode;
  onToggle?: (id: string) => void;
  className?: {
    icon?: string; // Icon className
    root?: string; // Root className
  };
  style?: {
    icon?: React.CSSProperties; // Icon style
    root?: React.CSSProperties; // Root style
  };
  count?: number; // Number indicator
}

const ProviderSwitch: React.FC<ProviderSwitchProps> = ({ id, type, name, icon, onToggle, className, style, count }) => {
  const searchParams = useSearchParams();
  const nameLowered = name.toLowerCase();
  const key = `${type ? `${type}-` : ""}${nameLowered}`; // Add dash if type has a value

  const nameSearchParam = searchParams.get(key);
  const isChecked = nameSearchParam === "true" || nameSearchParam === null; // Check if the search param is set to true or not set at all

  const handleToggle = (checked: boolean) => {
    onToggle?.(id);
    const params = new URLSearchParams(searchParams.toString());
    const key = `${type ? `${type}-` : ""}${nameLowered}`; // Add dash if type has a value
    if (checked) {
      params.set(key, "true");
    } else {
      params.set(key, "false");
    }
    const newUrl = `${window.location.pathname}?${params.toString()}`;
    window.history.replaceState({}, "", newUrl);
  };

  // if not checked, set searchParams to true as default
  useEffect(() => {
    if (nameSearchParam === null) {
      const params = new URLSearchParams(searchParams.toString());
      params.set(key, "true");
      const newUrl = `${window.location.pathname}?${params.toString()}`;
      window.history.replaceState({}, "", newUrl);
    }
  }, [nameSearchParam, nameLowered, type, searchParams, key]);

  return (
    <div
      className={cn("flex items-center justify-between py-2 sticky top-4 bg-white", className?.root)}
      style={style?.root}>
      <div className="flex items-center space-x-2">
        <div
          className={cn("relative flex h-6 w-6 items-center justify-center rounded-full", className?.icon)}
          style={style?.icon}>
          {icon}
          {!!count && (
            <span
              className={`absolute -right-2 top-[-11] flex max-h-5 max-w-max items-center justify-center rounded-sm bg-blue-500 px-1 py-1 text-[8px] text-white shadow-md`}>
              {count}
            </span>
          )}
        </div>
        <span className="text-sm">{name}</span>
      </div>
      <div className="flex items-center gap-1">
        <Switch checked={isChecked} onCheckedChange={handleToggle} />
      </div>
    </div>
  );
};

export default ProviderSwitch;
