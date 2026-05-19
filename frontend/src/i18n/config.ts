export const locales = ["cs", "en"] as const;
export type Locale = (typeof locales)[number];

export const defaultLocale: Locale = "cs";

export const LOCALE_COOKIE_NAME = "NEXT_LOCALE";

export const localeLabels: Record<Locale, string> = {
  cs: "Čeština",
  en: "English",
};
