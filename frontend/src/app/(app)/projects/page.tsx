"use client";

import { useState } from "react";
import { useProjects } from "@/queries/projects";
import { HealthIndicator } from "@/components/shared/health-indicator";
import { CardSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { FolderKanban, Search, AlertTriangle, Clock } from "lucide-react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { ProjectStatus } from "@/types";
import type { Project } from "@/types";

const statusColors: Record<ProjectStatus, string> = {
  [ProjectStatus.Active]: "bg-green-100 text-green-700",
  [ProjectStatus.OnHold]: "bg-yellow-100 text-yellow-700",
  [ProjectStatus.Completed]: "bg-blue-100 text-blue-700",
  [ProjectStatus.Archived]: "bg-muted text-muted-foreground",
};

export default function ProjectsPage() {
  const t = useTranslations("Projects");
  const tDashboard = useTranslations("Dashboard");
  const { data: projects, isLoading, error } = useProjects();
  const [search, setSearch] = useState("");

  const filteredProjects = projects?.filter(
    (p: Project) =>
      p.name.toLowerCase().includes(search.toLowerCase()) ||
      p.code.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">{t("title")}</h1>
        <p className="text-sm text-muted-foreground mt-1">{t("subtitle")}</p>
      </div>

      <div className="relative max-w-md">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <input
          type="text"
          placeholder={t("searchPlaceholder")}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full rounded-lg border border-input bg-background pl-10 pr-4 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
        />
      </div>

      {isLoading && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <CardSkeleton key={i} />
          ))}
        </div>
      )}

      {error && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
          {t("loadFailed")}
        </div>
      )}

      {filteredProjects && filteredProjects.length === 0 && (
        <EmptyState
          icon={<FolderKanban className="h-12 w-12" />}
          title={search ? t("noMatchingProjects") : t("noProjectsYet")}
          description={search ? t("adjustSearch") : t("createFirstProject")}
        />
      )}

      {filteredProjects && filteredProjects.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredProjects.map((project: Project) => (
            <Link
              key={project.id}
              href={`/projects/${project.code}/board`}
              className="block rounded-lg border border-border bg-card p-5 hover:shadow-md transition-shadow"
            >
              <div className="flex items-start justify-between mb-3">
                <div>
                  <h3 className="font-semibold text-card-foreground">{project.name}</h3>
                  <p className="text-xs text-muted-foreground font-mono">{project.code}</p>
                </div>
                <span
                  className={`px-2 py-0.5 rounded-full text-xs font-medium ${statusColors[project.status]}`}
                >
                  {t(`status.${project.status}` as "status.Active")}
                </span>
              </div>

              {project.parentProjectName && (
                <div className="mb-2">
                  <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-muted text-xs text-muted-foreground">
                    <FolderKanban className="h-3 w-3" />
                    {project.parentProjectName}
                  </span>
                </div>
              )}

              {project.description && (
                <p className="text-sm text-muted-foreground mb-3 line-clamp-2">
                  {project.description}
                </p>
              )}

              <div className="flex items-center justify-between">
                <HealthIndicator score={project.healthScore} size="sm" />
                <div className="flex gap-2">
                  {project.isOverBudget && <AlertTriangle className="h-4 w-4 text-red-500" />}
                  {project.isOverDeadline && <Clock className="h-4 w-4 text-orange-500" />}
                </div>
              </div>

              {project.budgetHours && (
                <div className="mt-3">
                  <div className="flex justify-between text-xs text-muted-foreground mb-1">
                    <span>{tDashboard("hoursLabel")}</span>
                    <span>
                      {project.spentHours.toFixed(1)} / {project.budgetHours}h
                    </span>
                  </div>
                  <div className="h-1.5 bg-muted rounded-full overflow-hidden">
                    <div
                      className="h-full rounded-full bg-accent-orange transition-all"
                      style={{
                        width: `${Math.min((project.spentHours / project.budgetHours) * 100, 100)}%`,
                      }}
                    />
                  </div>
                </div>
              )}
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
