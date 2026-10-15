export const download = () => {
  const fileContent = "This is a sample file for download.";
  const blob = new Blob([fileContent], { type: "text/plain" });
  const fileURL = URL.createObjectURL(blob);
  const link = document.createElement("a");

  link.href = fileURL;
  link.download = "Sample_Report.txt"; // temporary file name

  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);

  URL.revokeObjectURL(fileURL);
};
