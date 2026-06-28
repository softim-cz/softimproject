"use client";

import Link from "next/link";
import { useTranslations } from "next-intl";
import { Users, BookOpen, Import, Plug, type LucideIcon } from "lucide-react";

interface Tile {
  key: string;
  href: string;
  icon: LucideIcon;
}

const tiles: Tile[] = [
  { key: "users", href: "/admin/users", icon: Users },
  { key: "lookups", href: "/admin/lookups", icon: BookOpen },
  { key: "migration", href: "/admin/migration", icon: Import },
  { key: "integrations", href: "/admin/integrations", icon: Plug },
];

export default function AdminHubPage() {
  const t = useTranslations("AdminHub");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">{t("title")}</h1>
        <p className="text-sm text-muted-foreground mt-1">{t("subtitle")}</p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {tiles.map((tile) => (
          <Link
            key={tile.key}
            href={tile.href}
            className="group rounded-lg border border-border bg-card p-5 transition-colors hover:border-accent-orange/50 hover:bg-muted"
          >
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary-navy/10 text-primary-navy dark:bg-accent-orange/15 dark:text-accent-orange">
                <tile.icon className="h-5 w-5" />
              </div>
              <h2 className="text-base font-semibold text-foreground group-hover:text-accent-orange transition-colors">
                {t(`${tile.key}.title`)}
              </h2>
            </div>
            <p className="mt-3 text-sm text-muted-foreground">{t(`${tile.key}.description`)}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}
