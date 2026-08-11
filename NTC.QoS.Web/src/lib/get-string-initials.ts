export function getStringInitials(str: string) {
  if (typeof str !== "string" || str.trim() === "") {
    return "";
  }
  // Split the string by spaces, take the first two words, and return their initials
  return str
    .trim()
    .toLocaleUpperCase()
    .split(" ")
    .slice(0, 2)
    .map((word) => word.charAt(0))
    .join("");
}
