import { type Barangay } from "@/components/features/barangay/types/Barangay";
import { type BarangayCoverageLightDto } from "@/components/features/barangay-coverage/types/BarangayCoverage";

export type BarangayWithCoverage = Barangay & {
    coverage?: BarangayCoverageLightDto;
    // Local coverage ID - used when coverage is created in the current session
    // before refetching from server
    localCoverageId?: string;
    wiredPercentage: number;
    wirelessPercentage: number;
    isDirty: boolean;
};

export type GroupedBarangays = {
    provinceName: string;
    provinceId: string;
    municipalities: {
        municipalityName: string;
        municipalityId: string;
        barangays: BarangayWithCoverage[];
    }[];
};

export type ViewMode = "grouped" | "flatten";
