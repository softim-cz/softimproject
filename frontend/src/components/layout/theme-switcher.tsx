"use client";

import { useTheme } from "next-themes";
import { useTranslations } from "next-intl";
import { useState, useRef, useEffect } from "react";
import { Sun, Moon, Monitor, Check } from "lucide-react";

type ThemeOption = "light" | "dark" | "system";

const OPTIONS: { value: ThemeOption; icon: typeof Sun }[] = [
  { value: "light", icon: Sun },
  { value: "dark", icon: Moon },
  { value: "system", icon: Monitor },
];

export function ThemeSwitcher() {
  const { theme, setTheme, resolvedTheme } = useTheme();
  const t = useTranslations("Topbar");
  const [open, setOpen] = useState(false);
  const [mounted, setMounted] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  // next-themes resolves the active theme only on the client; flip a mount flag
  // after first paint so SSR markup matches and we avoid a hydration mismatch.
  // The synchronous setState here is the intended one-shot guard, not a render loop.
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(() => setMounted(true), []);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  function handleSelect(value: ThemeOption) {
    setOpen(false);
    setTheme(value);
  }

  // The trigger icon reflects what's actually on screen (resolvedTheme handles
  // the "system" case), so the button mirrors the current appearance.
  const TriggerIcon = mounted && resolvedTheme === "dark" ? Moon : Sun;

  return (
    <div className="relative" ref={menuRef}>
      <button
        onClick={() => setOpen(!open)}
        className="flex items-center gap-1 p-2 rounded-lg hover:bg-muted transition-colors"
        title={t("switchTheme")}
        aria-label={t("switchTheme")}
      >
        <TriggerIcon className="h-5 w-5 text-muted-foreground" />
      </button>

      {open && (
        <div className="absolute right-0 mt-2 w-44 rounded-lg border border-border bg-popover shadow-lg z-50">
          <div className="py-1">
            {OPTIONS.map(({ value, icon: Icon }) => (
              <button
                key={value}
                onClick={() => handleSelect(value)}
                className="flex items-center justify-between w-full px-3 py-2 text-sm text-popover-foreground hover:bg-muted transition-colors"
              >
                <span className="flex items-center gap-2">
                  <Icon className="h-4 w-4 text-muted-foreground" />
                  {t(`theme_${value}`)}
                </span>
                {mounted && theme === value && <Check className="h-4 w-4 text-accent-orange" />}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
