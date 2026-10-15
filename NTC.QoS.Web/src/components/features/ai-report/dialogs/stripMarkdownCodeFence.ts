"use client";
// Helper to strip markdown code fence wrapper (```markdown ... ```)
// Note: This only removes the OUTERMOST wrapper fence. Any code blocks
// (e.g., ```python, ```json) nested inside the content will be preserved
// and rendered correctly as code blocks by the Markdown component.
export const stripMarkdownCodeFence = (content: string): string => {
    const trimmed = content.trim();
    // Check if content starts with ```markdown and ends with ```
    if (trimmed.startsWith("```markdown") && trimmed.endsWith("```")) {
        // Remove the opening ```markdown and closing ```
        return trimmed.slice(11, -3).trim();
    }
    // Also handle just ``` without language specifier
    if (trimmed.startsWith("```") && trimmed.endsWith("```")) {
        const firstNewline = trimmed.indexOf("\n");
        if (firstNewline !== -1) {
            return trimmed.slice(firstNewline + 1, -3).trim();
        }
    }
    return content;
};
