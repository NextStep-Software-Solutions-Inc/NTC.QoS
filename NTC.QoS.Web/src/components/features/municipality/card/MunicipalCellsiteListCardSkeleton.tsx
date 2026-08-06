import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

export default function MunicipalCellsiteListCardSkeleton() {
  // Fixed counts for skeleton rendering
  const providerCount = 2;
  const siteCount = 2;

  return (
    <div className="w-full space-y-2">
      {Array.from({ length: providerCount }).map((_, providerIndex) => (
        <Card key={providerIndex} className="w-full max-w-screen-lg">
          {/* Card Header */}
          <CardHeader className="flex justify-between p-4">
            <div className="flex items-center gap-2">
              <Skeleton className="h-8 w-8 rounded-full" />
              <Skeleton className="h-5 w-32 rounded-lg" />
            </div>
            <div className="flex items-end gap-3">
              <Skeleton className="h-8 w-8 rounded-full" />
              <div className="flex flex-col gap-1">
                <Skeleton className="h-3 w-16 rounded" />
                <Skeleton className="h-3 w-12 rounded" />
              </div>
            </div>
          </CardHeader>

          {/* Card Content */}
          <CardContent className="space-y-2">
            {Array.from({ length: siteCount }).map((_, siteIndex) => (
              <div key={siteIndex} className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <Skeleton className="h-7 w-7 rounded-full" />
                  <div className="flex-1 space-y-1">
                    <Skeleton className="h-4 w-32 rounded" />
                    <Skeleton className="h-3 w-20 rounded" />
                    <Skeleton className="h-3 w-40 rounded" />
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <Skeleton className="h-6 w-16 rounded-full" />
                  <Skeleton className="h-8 w-20 rounded" />
                </div>
              </div>
            ))}
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
