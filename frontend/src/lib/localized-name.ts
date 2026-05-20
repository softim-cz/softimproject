import type { Locale } from "@/i18n/config";

/**
 * Pick the localized display name for a lookup item.
 * Falls back to the base `name` when the translation for the active locale is missing.
 */
export function localizedName(
  item: { name: string; nameCs?: string | null; nameEn?: string | null },
  locale: Locale
): string {
  const translated = locale === "cs" ? item.nameCs : item.nameEn;
  return translated && translated.trim().length > 0 ? translated : item.name;
}
