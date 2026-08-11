export default function getNormalizeString(str: string) {
  return str
    .toLowerCase() // lowercase everything
    .normalize("NFD") // normalize accented characters (e.g., é → e)
    .replace(/[\u0300-\u036f]/g, "") // remove diacritics
    .replace(/[^a-z0-9]/g, ""); // remove everything except letters and numbers
}
