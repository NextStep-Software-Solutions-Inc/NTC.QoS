import React from "react";

import { type Technology } from "@/components/features/technology/types/Technology";
import { cn } from "@/lib/utils";

interface ProviderTechnologyGenerationProps {
  providerKey: string;
  providerWithTech: { generations: Array<{ id: string; name: string; technologies: Technology[] }> };
  providerColor: string;
}

export const ProviderTechnologyGeneration = ({
  providerWithTech,
  providerColor,
}: ProviderTechnologyGenerationProps) => {
  return (
    <div className="space-y-4 pl-4">
      {providerWithTech?.generations.map((generation) => {
        // TODO: Implement generation bands data fetching when the API endpoint is available
        return (
          <div key={generation.id} className="space-y-2">
            {generation.technologies?.map((tech) => (
              <div key={tech.id} className="flex flex-row items-center justify-between gap-4">
                <div
                  className={cn(
                    "flex w-[30%] items-center justify-end text-right text-sm font-bold",
                    `text-[${providerColor}]`,
                  )}>
                  <div className="max-w-[200px]">
                    <span>{`${tech.standard.toUpperCase()} ${generation.name.toUpperCase()}`}</span>
                  </div>
                </div>
                <div className="flex-1">
                  {/* TODO: Implement bands display when the API endpoint is available */}
                  <div className="text-sm text-gray-500">Bands will be displayed here</div>
                </div>
              </div>
            ))}
          </div>
        );
      })}
    </div>
  );
};
