"use client";

import { use } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useTranslations } from "next-intl";
import { useProjectByCode } from "@/queries/projects";
import { cn } from "@/lib/utils";
import { LayoutGrid, List, Clock, MessageSquare } from "lucide-react";

const tabs = [
  { key: "board", icon: LayoutGrid },
  { key: "tasks", icon: List },
  { key: "worklogs", icon: Clock },
  { key: "discussion", icon: MessageSquare },
] as const;

export default function ProjectLayout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ code: string }>;
}) {
  const { code } = use(params);
  const pathname = usePathname();
  const { data: project } = useProjectByCode(code);
  const t = useTranslations("Projects.views");
  const tProjects = useTranslations("Projects");

  return (
    <div className="flex flex-col h-full">
      <div className="border-b border-border bg-card px-6 pt-4">
        <h1 className="text-xl font-bold text-foreground mb-3">
          {project?.name ?? tProjects("title")}
        </h1>
        <nav className="flex gap-1 -mb-px">
          {tabs.map((tab) => {
            const href = `/projects/${code}/${tab.key}`;
            const isActive = pathname === href || pathname.startsWith(href + "/");
            return (
              <Link
                key={tab.key}
                href={href}
                className={cn(
                  "flex items-center gap-1.5 px-3 py-2 text-sm font-medium border-b-2 transition-colors",
                  isActive
                    ? "border-primary text-primary"
                    : "border-transparent text-muted-foreground hover:text-foreground hover:border-border"
                )}
              >
                <tab.icon className="h-4 w-4" />
                {t(tab.key as "board")}
              </Link>
            );
          })}
        </nav>
      </div>

      <div className="flex-1 overflow-auto p-6">{children}</div>
    </div>
  );
}
