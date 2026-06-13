"use client";

import { Command } from "cmdk";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useTranslations } from "next-intl";
import { Search } from "lucide-react";
import { useProjects } from "@/queries/projects";

/**
 * Global command palette (Ctrl/⌘+K or the topbar search button). Jumps to any
 * project or top-level page. Kept always-mounted so the keyboard shortcut works;
 * renders the dialog only when open.
 */
export function CommandPalette({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (value: boolean) => void;
}) {
  const t = useTranslations("CommandPalette");
  const router = useRouter();
  const { data: projects } = useProjects();

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === "k") {
        e.preventDefault();
        onOpenChange(!open);
      } else if (e.key === "Escape" && open) {
        onOpenChange(false);
      }
    };
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [open, onOpenChange]);

  if (!open) return null;

  const go = (href: string) => {
    onOpenChange(false);
    router.push(href);
  };

  const nav = [
    { href: "/dashboard", label: t("nav.dashboard") },
    { href: "/projects", label: t("nav.projects") },
    { href: "/worklogs", label: t("nav.worklogs") },
    { href: "/resources", label: t("nav.resources") },
    { href: "/admin", label: t("nav.admin") },
  ];

  const groupClass =
    "[&_[cmdk-group-heading]]:px-2 [&_[cmdk-group-heading]]:py-1 [&_[cmdk-group-heading]]:text-xs [&_[cmdk-group-heading]]:font-medium [&_[cmdk-group-heading]]:text-muted-foreground";
  const itemClass =
    "flex items-center gap-2 px-2 py-2 rounded-lg text-sm text-foreground cursor-pointer data-[selected=true]:bg-muted";

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center pt-[15vh]">
      <div className="absolute inset-0 bg-black/50" onClick={() => onOpenChange(false)} />
      <Command
        label={t("placeholder")}
        className="relative w-full max-w-lg mx-4 rounded-xl border border-border bg-card shadow-xl overflow-hidden"
      >
        <div className="flex items-center gap-2 border-b border-border px-3">
          <Search className="h-4 w-4 text-muted-foreground shrink-0" />
          <Command.Input
            autoFocus
            placeholder={t("placeholder")}
            className="flex-1 bg-transparent py-3 text-sm text-foreground outline-none placeholder:text-muted-foreground"
          />
        </div>
        <Command.List className="max-h-80 overflow-y-auto p-2">
          <Command.Empty className="py-6 text-center text-sm text-muted-foreground">
            {t("empty")}
          </Command.Empty>

          <Command.Group heading={t("navGroup")} className={groupClass}>
            {nav.map((n) => (
              <Command.Item
                key={n.href}
                value={`nav ${n.label}`}
                onSelect={() => go(n.href)}
                className={itemClass}
              >
                {n.label}
              </Command.Item>
            ))}
          </Command.Group>

          {projects && projects.length > 0 && (
            <Command.Group heading={t("projectsGroup")} className={groupClass}>
              {projects.map((p) => (
                <Command.Item
                  key={p.id}
                  value={`project ${p.code} ${p.name}`}
                  onSelect={() => go(`/projects/${p.code}/board`)}
                  className={itemClass}
                >
                  <span className="font-mono text-xs text-muted-foreground">{p.code}</span>
                  <span className="truncate">{p.name}</span>
                </Command.Item>
              ))}
            </Command.Group>
          )}
        </Command.List>
      </Command>
    </div>
  );
}
