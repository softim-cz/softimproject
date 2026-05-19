"use client";

import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { useState, useRef, useEffect, useTransition } from "react";
import { Languages, Check } from "lucide-react";
import { setLocale } from "@/i18n/actions";
import { locales, localeLabels, type Locale } from "@/i18n/config";

export function LocaleSwitcher() {
  const currentLocale = useLocale() as Locale;
  const t = useTranslations("Topbar");
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [isPending, startTransition] = useTransition();
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  function handleSelect(locale: Locale) {
    setOpen(false);
    if (locale === currentLocale) return;
    startTransition(async () => {
      await setLocale(locale);
      router.refresh();
    });
  }

  return (
    <div className="relative" ref={menuRef}>
      <button
        onClick={() => setOpen(!open)}
        className="flex items-center gap-1 p-2 rounded-lg hover:bg-muted transition-colors"
        title={t("switchLanguage")}
        aria-label={t("switchLanguage")}
        disabled={isPending}
      >
        <Languages className="h-5 w-5 text-muted-foreground" />
        <span className="hidden md:inline text-xs font-medium uppercase text-muted-foreground">
          {currentLocale}
        </span>
      </button>

      {open && (
        <div className="absolute right-0 mt-2 w-44 rounded-lg border border-border bg-popover shadow-lg z-50">
          <div className="py-1">
            {locales.map((locale) => (
              <button
                key={locale}
                onClick={() => handleSelect(locale)}
                className="flex items-center justify-between w-full px-3 py-2 text-sm text-popover-foreground hover:bg-muted transition-colors"
              >
                <span>{localeLabels[locale]}</span>
                {locale === currentLocale && <Check className="h-4 w-4 text-accent-orange" />}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
