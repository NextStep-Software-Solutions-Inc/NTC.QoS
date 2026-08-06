/** @type {import('prettier').Config & import('prettier-plugin-tailwindcss').PluginOptions} */
const config = {
  plugins: ["prettier-plugin-tailwindcss", "@ianvs/prettier-plugin-sort-imports", "prettier-plugin-sort-json"],
  importOrder: ["^(react/(.*)$)|^(react$)", "^(next/(.*)$)|^(next$)", "<THIRD_PARTY_MODULES>", "", "^@/(.*)$", "^[./]"],
  importOrderParserPlugins: ["typescript", "jsx", "decorators-legacy"],
  trailingComma: "all",
  arrowParens: "always",
  bracketSameLine: true,
  bracketSpacing: true,
  objectWrap: "collapse",
  printWidth: 120,
  semi: true,
  tabWidth: 2,
  singleAttributePerLine: false,
};

export default config;
