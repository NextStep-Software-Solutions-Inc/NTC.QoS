/**
 * Connectivity Assessment Types
 * Types for triggering and monitoring connectivity assessments
 */

/**
 * Request payload for starting a new connectivity assessment job
 */
export interface ConnectivityAssessmentJobRequest {
    regionIds?: string[];
    provinceIds?: string[];
    municipalityIds?: string[];
    barangayIds?: string[];
    forceRecalculate?: boolean;
    generateHierarchicalAssessments?: boolean;
    hierarchicalDepth?: number | null;
    maxHierarchicalLocations?: number | null;
    hierarchicalBatchSize?: number | null;
    startDate?: string | null;
    endDate?: string | null;
}

/**
 * Response from starting a connectivity assessment job
 */
export interface ConnectivityAssessmentJobResponse {
    data: {
        jobId: string;
    };
    succeeded: boolean;
    message: string;
    errors?: Record<string, string[]>;
}

/**
 * Job status enum
 */
export type JobStatus = "Pending" | "Processing" | "Completed" | "Failed" | "Cancelled";

/**
 * Response from checking job status
 */
export interface ConnectivityAssessmentStatusResponse {
    data: {
        jobId: string;
        status: JobStatus;
        message: string;
        lastUpdated: string;
        startTime: string;
        endTime: string;
        processingTime: string;
        processingCount: number;
        processedCount: number;
        totalCount: number;
        progressPercentage: number;
        processingPercentage: number;
        isRunning: boolean;
        isCompleted: boolean;
        statusDescription: string;
        databaseVerifiedCount: number;
        databaseVerified: boolean;
        databaseVerificationTime: string;
        hasDatabaseDiscrepancy: boolean;
        missingInDatabase: number;
        isStale: boolean;
        sampleLocationIds: string[];
        failedLocationCount: number;
        failedLocationErrors: string[];
        healthStatus: string;
    };
    succeeded: boolean;
    message: string;
    errors?: Record<string, string[]>;
}
