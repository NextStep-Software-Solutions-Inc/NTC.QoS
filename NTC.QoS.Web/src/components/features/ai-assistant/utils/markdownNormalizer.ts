/**
 * Normalize markdown content to ensure proper rendering
 * Converts non-standard markdown syntax to standard format
 */
export function normalizeMarkdown(content: string): string {
    if (!content) return content;

    let normalized = content;

    // Convert + bullets to standard - bullets
    normalized = normalized.replace(/^\s*\+\s+/gm, '- ');

    // Convert * bullets at start of line to - bullets (for consistency)
    normalized = normalized.replace(/^(\s*)\*\s+/gm, '$1- ');

    // Ensure proper spacing after list markers
    normalized = normalized.replace(/^(\s*-)\s*/gm, '$1 ');

    // Fix nested lists - ensure they have proper indentation (2 or 4 spaces)
    // This converts irregular indentation to standard 2-space indentation
    const lines = normalized.split('\n');
    const processedLines = lines.map(line => {
        // Count leading spaces
        const leadingSpaces = line.match(/^(\s*)/)?.[1]?.length || 0;

        // If it's a list item, normalize indentation to multiples of 2
        if (line.trim().startsWith('-')) {
            const level = Math.floor(leadingSpaces / 2);
            const normalizedIndent = '  '.repeat(level); // 2 spaces per level
            return normalizedIndent + line.trim();
        }

        return line;
    });

    normalized = processedLines.join('\n');

    // Ensure blank lines between major sections (helps with paragraph spacing)
    // Add blank line before headers if not present
    normalized = normalized.replace(/([^\n])\n(#{1,6}\s)/g, '$1\n\n$2');

    // Ensure double newlines for paragraph breaks are preserved
    normalized = normalized.replace(/\n{3,}/g, '\n\n');

    return normalized;
}
