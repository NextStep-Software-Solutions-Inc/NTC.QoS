// Add this utility function at the top or in a utils file
export default function getSlugify(str: string) {
  return str.toLowerCase().replace(/[()]/g, "").replace(/%/g, "-").trim().replace(/\s+/g, "-");
}
