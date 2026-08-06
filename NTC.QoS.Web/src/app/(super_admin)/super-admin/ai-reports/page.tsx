import { AIReportsManager } from "@/components/features/ai-report/components/AIReportsManager";

export default function SuperAdminAIReportsPage() {
    return (
        <div className="container mx-auto p-4 sm:p-6">
            <AIReportsManager showGenerateButton={true} showDeleteButton={true} />
        </div>
    );
}
