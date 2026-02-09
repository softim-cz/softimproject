"use client";

import { use } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useProject } from "@/queries/projects";
import { cn } from "@/lib/utils";
import { LayoutGrid, List, Clock, Settings } from "lucide-react";

const tabs = [
  { name: "Board", href: "board", icon: LayoutGrid },
  { name: "Tasks", href: "tasks", icon: List },
  { name: "Worklogs", href: "worklogs", icon: Clock },
  { name: "Settings", href: "settings", icon: Settings },
];

export default function ProjectLayout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ id: string }>;
}) {
  const { id: projectId } = use(params);
  const pathname = usePathname();
  const { data: project } = useProject(projectId);

  return (
    <div className="flex flex-col h-full">
      {/* Project header + tabs */}
      <div className="border-b border-border bg-card px-6 pt-4">
        <h1 className="text-xl font-bold text-foreground mb-3">
          {project?.name ?? "Project"}
        </h1>
        <nav className="flex gap-1 -mb-px">
          {tabs.map((tab) => {
            const href = `/projects/${projectId}/${tab.href}`;
            const isActive = pathname === href || pathname.startsWith(href + "/");
            return (
              <Link
                key={tab.href}
                href={href}
                className={cn(
                  "flex items-center gap-1.5 px-3 py-2 text-sm font-medium border-b-2 transition-colors",
                  isActive
                    ? "border-primary text-primary"
                    : "border-transparent text-muted-foreground hover:text-foreground hover:border-border"
                )}
              >
                <tab.icon className="h-4 w-4" />
                {tab.name}
              </Link>
            );
          })}
        </nav>
      </div>

      {/* Page content */}
      <div className="flex-1 overflow-hidden p-6">{children}</div>
    </div>
  );
}
