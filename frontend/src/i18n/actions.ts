"use server";

import { cookies } from "next/headers";
import { LOCALE_COOKIE_NAME, locales, type Locale } from "./config";

export async function setLocale(locale: Locale) {
  if (!(locales as readonly string[]).includes(locale)) {
    throw new Error(`Unsupported locale: ${locale}`);
  }
  const cookieStore = await cookies();
  cookieStore.set(LOCALE_COOKIE_NAME, locale, {
    path: "/",
    maxAge: 60 * 60 * 24 * 365,
    sameSite: "lax",
  });
}
