/**
 * PDF Generation Utility
 * Converts markdown content to styled PDF using jsPDF
 */

interface PDFGeneratorOptions {
    title: string;
    content: string;
    filename?: string;
}

/**
 * Generates a PDF from markdown content with proper styling
 * @param options - PDF generation options including title, content, and optional filename
 * @returns Promise that resolves when PDF is generated and downloaded
 */
export async function generatePDFFromMarkdown(options: PDFGeneratorOptions): Promise<void> {
    const { title, content, filename } = options;

    // Dynamically load jsPDF
    const loadJsPDF = () => {
        return new Promise((resolve, reject) => {
            if ((window as any).jspdf) {
                resolve((window as any).jspdf.jsPDF);
                return;
            }
            const script = document.createElement('script');
            script.src = 'https://cdnjs.cloudflare.com/ajax/libs/jspdf/2.5.1/jspdf.umd.min.js';
            script.onload = () => resolve((window as any).jspdf.jsPDF);
            script.onerror = reject;
            document.head.appendChild(script);
        });
    };

    const jsPDF = await loadJsPDF() as any;
    const doc = new jsPDF();

    const pageWidth = doc.internal.pageSize.getWidth();
    const pageHeight = doc.internal.pageSize.getHeight();
    const margin = 20;
    const maxWidth = pageWidth - (margin * 2);
    let y = margin;

    // Helper function to check if we need a new page
    const checkPageBreak = (spaceNeeded: number) => {
        if (y + spaceNeeded > pageHeight - margin) {
            doc.addPage();
            y = margin;
            return true;
        }
        return false;
    };

    // Helper function to add text with wrapping
    const addText = (text: string, fontSize: number, isBold: boolean = false, leftMargin: number = 0) => {
        doc.setFontSize(fontSize);
        doc.setFont('helvetica', isBold ? 'bold' : 'normal');

        const lines = doc.splitTextToSize(text, maxWidth - leftMargin);
        const lineHeight = fontSize * 0.5;

        for (let i = 0; i < lines.length; i++) {
            checkPageBreak(lineHeight);
            doc.text(lines[i], margin + leftMargin, y);
            y += lineHeight;
        }
    };

    // Parse and render markdown content
    const lines = content.split('\n');

    for (let i = 0; i < lines.length; i++) {
        const line = lines[i];

        // Skip undefined lines (should not happen with split, but TypeScript safety)
        if (!line) {
            continue;
        }

        // Skip empty lines but add small space
        if (!line.trim()) {
            y += 3;
            continue;
        }

        // H1 headers
        if (line.startsWith('# ')) {
            checkPageBreak(20);
            y += 5; // Space before header
            addText(line.replace(/^#\s+/, ''), 18, true);
            y += 8; // Space after header
            // Add underline
            doc.setDrawColor(200, 200, 200);
            doc.line(margin, y, pageWidth - margin, y);
            y += 5;
        }
        // H2 headers
        else if (line.startsWith('## ')) {
            checkPageBreak(15);
            y += 4;
            addText(line.replace(/^##\s+/, ''), 14, true);
            y += 6;
        }
        // H3 headers
        else if (line.startsWith('### ')) {
            checkPageBreak(12);
            y += 3;
            addText(line.replace(/^###\s+/, ''), 12, true);
            y += 4;
        }
        // H4 headers
        else if (line.startsWith('#### ')) {
            checkPageBreak(11);
            y += 2;
            addText(line.replace(/^####\s+/, ''), 11, true);
            y += 3;
        }
        // Bullet lists
        else if (line.match(/^[-*]\s+/)) {
            const text = line.replace(/^[-*]\s+/, '');
            checkPageBreak(8);
            // Add bullet point
            doc.setFontSize(10);
            doc.text('•', margin + 5, y);
            // Add text with bold/italic parsing
            const processedText = text
                .replace(/\*\*(.+?)\*\*/g, '$1')
                .replace(/\*(.+?)\*/g, '$1')
                .replace(/`(.+?)`/g, '$1');
            addText(processedText, 10, false, 10);
            y += 2;
        }
        // Numbered lists
        else if (line.match(/^\d+\.\s+/)) {
            const match = line.match(/^(\d+)\.\s+(.+)/);
            if (match && match[1] && match[2]) {
                const num = match[1];
                const text = match[2];
                checkPageBreak(8);
                doc.setFontSize(10);
                doc.text(`${num}.`, margin + 5, y);
                const processedText = text
                    .replace(/\*\*(.+?)\*\*/g, '$1')
                    .replace(/\*(.+?)\*/g, '$1')
                    .replace(/`(.+?)`/g, '$1');
                addText(processedText, 10, false, 15);
                y += 2;
            }
        }
        // Blockquotes
        else if (line.startsWith('> ')) {
            checkPageBreak(10);
            doc.setFillColor(240, 248, 255);
            const quoteHeight = 10;
            doc.rect(margin, y - 3, maxWidth, quoteHeight, 'F');
            doc.setDrawColor(59, 130, 246);
            doc.line(margin, y - 3, margin, y + quoteHeight - 3);
            doc.line(margin + 1, y - 3, margin + 1, y + quoteHeight - 3);
            const text = line.replace(/^>\s+/, '');
            addText(text, 10, false, 5);
            y += 4;
        }
        // Regular paragraphs
        else {
            checkPageBreak(10);
            const processedText = line
                .replace(/\*\*(.+?)\*\*/g, '$1')
                .replace(/\*(.+?)\*/g, '$1')
                .replace(/`(.+?)`/g, '$1');
            addText(processedText, 10);
            y += 2;
        }
    }

    // Generate filename
    const pdfFilename = filename || `${title.replace(/[^a-z0-9]/gi, "_")}.pdf`;
    doc.save(pdfFilename);
}
