"use client";

import { useProjects } from "@/queries/projects";
import { useWorklogs } from "@/queries/worklogs";
import { useDashboardStats } from "@/queries/stats";
import { HealthIndicator } from "@/components/shared/health-indicator";
import { CardSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import {
  FolderKanban,
  Clock,
  Plus,
  TrendingUp,
  AlertTriangle,
  CheckCircle2,
  ListTodo,
} from "lucide-react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { format, startOfWeek, endOfWeek, isPast, isToday } from "date-fns";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Legend,
} from "recharts";
import type { Project } from "@/types";

function ProjectHealthCards() {
  const { data: projects, isLoading, error } = useProjects();
  const t = useTranslations("Dashboard");

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {Array.from({ length: 3 }).map((_, i) => (
          <CardSkeleton key={i} />
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
        {t("loadProjectsFailed")}
      </div>
    );
  }

  if (!projects?.length) {
    return (
      <EmptyState
        icon={<FolderKanban className="h-12 w-12" />}
        title={t("noProjectsYet")}
        description={t("createFirstProject")}
        action={
          <Link
            href="/projects"
            className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity"
          >
            <Plus className="h-4 w-4" />
            {t("newProject")}
          </Link>
        }
      />
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      {projects.slice(0, 6).map((project: Project) => (
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
            <HealthIndicator score={project.healthScore} showLabel={false} />
          </div>
          <div className="space-y-2">
            {project.budgetHours && (
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">{t("hoursLabel")}</span>
                <span className="text-card-foreground">
                  {project.spentHours.toFixed(1)} / {project.budgetHours}h
                </span>
              </div>
            )}
            <div className="flex gap-2">
              {project.isOverBudget && (
                <span className="flex items-center gap-1 text-xs text-red-600">
                  <AlertTriangle className="h-3 w-3" />
                  {t("overBudget")}
                </span>
              )}
              {project.isOverDeadline && (
                <span className="flex items-center gap-1 text-xs text-orange-600">
                  <Clock className="h-3 w-3" />
                  {t("overdue")}
                </span>
              )}
              {!project.isOverBudget && !project.isOverDeadline && (
                <span className="flex items-center gap-1 text-xs text-green-600">
                  <CheckCircle2 className="h-3 w-3" />
                  {t("onTrack")}
                </span>
              )}
            </div>
          </div>
        </Link>
      ))}
    </div>
  );
}

function WeeklyHoursChart() {
  const now = new Date();
  const weekStart = startOfWeek(now, { weekStartsOn: 1 });
  const weekEnd = endOfWeek(now, { weekStartsOn: 1 });
  const { data: worklogs, isLoading } = useWorklogs({
    from: format(weekStart, "yyyy-MM-dd"),
    to: format(weekEnd, "yyyy-MM-dd"),
  });

  const daysOfWeek = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
  const chartData = daysOfWeek.map((day, i) => {
    const date = new Date(weekStart);
    date.setDate(date.getDate() + i);
    const dateStr = format(date, "yyyy-MM-dd");
    const dayHours =
      worklogs?.filter((w) => w.date === dateStr).reduce((sum, w) => sum + w.hours, 0) || 0;
    return { day, hours: dayHours };
  });

  if (isLoading) {
    return <div className="h-64 animate-pulse rounded-lg bg-muted" />;
  }

  return (
    <ResponsiveContainer width="100%" height={250}>
      <BarChart data={chartData}>
        <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
        <XAxis dataKey="day" stroke="var(--muted-foreground)" fontSize={12} />
        <YAxis stroke="var(--muted-foreground)" fontSize={12} />
        <Tooltip
          contentStyle={{
            background: "var(--card)",
            border: "1px solid var(--border)",
            borderRadius: "8px",
            color: "var(--card-foreground)",
          }}
        />
        <Bar dataKey="hours" fill="var(--accent-orange)" radius={[4, 4, 0, 0]} />
      </BarChart>
    </ResponsiveContainer>
  );
}

function QuickActions() {
  const t = useTranslations("Dashboard");
  return (
    <div className="flex flex-wrap gap-3">
      <Link
        href="/projects"
        className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity"
      >
        <Plus className="h-4 w-4" />
        {t("newProject")}
      </Link>
      <Link
        href="/worklogs"
        className="inline-flex items-center gap-2 px-4 py-2 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors"
      >
        <Clock className="h-4 w-4" />
        {t("logTime")}
      </Link>
    </div>
  );
}

function DashboardStats() {
  const t = useTranslations("Dashboard");
  const now = new Date();
  const weekStart = startOfWeek(now, { weekStartsOn: 1 });
  const weekEnd = endOfWeek(now, { weekStartsOn: 1 });
  const { data: projects } = useProjects();
  const { data: worklogs } = useWorklogs({
    from: format(weekStart, "yyyy-MM-dd"),
    to: format(weekEnd, "yyyy-MM-dd"),
  });

  const activeProjects = projects?.filter((project) => project.status === "Active").length ?? 0;
  const visibleTickets =
    projects?.reduce((sum, project) => sum + (project.ticketCount ?? 0), 0) ?? 0;
  const weeklyHours = worklogs?.reduce((sum, worklog) => sum + worklog.hours, 0) ?? 0;

  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
      <div className="rounded-lg border border-border bg-card p-5">
        <div className="flex items-center gap-3">
          <div className="h-10 w-10 rounded-lg bg-primary-navy/10 flex items-center justify-center">
            <FolderKanban className="h-5 w-5 text-primary-navy" />
          </div>
          <div>
            <p className="text-2xl font-bold text-card-foreground">{activeProjects}</p>
            <p className="text-sm text-muted-foreground">{t("activeProjects")}</p>
          </div>
        </div>
      </div>
      <div className="rounded-lg border border-border bg-card p-5">
        <div className="flex items-center gap-3">
          <div className="h-10 w-10 rounded-lg bg-accent-orange/10 flex items-center justify-center">
            <TrendingUp className="h-5 w-5 text-accent-orange" />
          </div>
          <div>
            <p className="text-2xl font-bold text-card-foreground">{visibleTickets}</p>
            <p className="text-sm text-muted-foreground">{t("visibleTickets")}</p>
          </div>
        </div>
      </div>
      <div className="rounded-lg border border-border bg-card p-5">
        <div className="flex items-center gap-3">
          <div className="h-10 w-10 rounded-lg bg-green-500/10 flex items-center justify-center">
            <Clock className="h-5 w-5 text-green-600" />
          </div>
          <div>
            <p className="text-2xl font-bold text-card-foreground">{weeklyHours.toFixed(1)}</p>
            <p className="text-sm text-muted-foreground">{t("hoursThisWeek")}</p>
          </div>
        </div>
      </div>
    </div>
  );
}

function TicketsByStateChart() {
  const { data: stats, isLoading } = useDashboardStats();
  const t = useTranslations("Dashboard");

  if (isLoading) return <div className="h-64 animate-pulse rounded-lg bg-muted" />;

  const items = stats?.ticketsByState ?? [];
  if (items.length === 0)
    return <p className="text-sm text-muted-foreground text-center py-8">{t("noTicketsYet")}</p>;

  return (
    <ResponsiveContainer width="100%" height={250}>
      <PieChart>
        <Pie
          data={items}
          dataKey="count"
          nameKey="stateName"
          cx="50%"
          cy="50%"
          innerRadius={60}
          outerRadius={90}
          paddingAngle={3}
        >
          {items.map((item) => (
            <Cell key={item.stateId} fill={item.stateColor} />
          ))}
        </Pie>
        <Tooltip
          formatter={(value) => [t("ticketsCount", { count: Number(value) || 0 }), ""]}
          contentStyle={{
            background: "var(--card)",
            border: "1px solid var(--border)",
            borderRadius: "8px",
            color: "var(--card-foreground)",
          }}
        />
        <Legend
          formatter={(value) => <span className="text-xs text-muted-foreground">{value}</span>}
        />
      </PieChart>
    </ResponsiveContainer>
  );
}

function MyOpenTickets() {
  const { data: stats, isLoading } = useDashboardStats();
  const t = useTranslations("Dashboard");

  if (isLoading)
    return (
      <div className="space-y-2">
        {Array.from({ length: 3 }).map((_, i) => (
          <div key={i} className="h-14 animate-pulse rounded-lg bg-muted" />
        ))}
      </div>
    );

  const tickets = stats?.myOpenTickets ?? [];

  if (tickets.length === 0)
    return (
      <div className="flex items-center gap-3 py-6 text-muted-foreground">
        <CheckCircle2 className="h-5 w-5 text-green-500 shrink-0" />
        <span className="text-sm">{t("noOpenTicketsAssigned")}</span>
      </div>
    );

  return (
    <div className="space-y-2">
      {tickets.map((ticket) => {
        const overdue =
          ticket.dueDate && !isToday(new Date(ticket.dueDate)) && isPast(new Date(ticket.dueDate));
        return (
          <Link
            key={ticket.id}
            href={`/projects/${ticket.projectCode}/tickets/${ticket.key}`}
            className="flex items-center justify-between gap-3 rounded-lg border border-border bg-card px-4 py-3 hover:shadow-sm transition-shadow"
          >
            <div className="flex items-center gap-3 min-w-0">
              <span className="text-xs font-mono text-muted-foreground shrink-0">{ticket.key}</span>
              <span className="text-sm text-foreground truncate">{ticket.title}</span>
            </div>
            <div className="flex items-center gap-2 shrink-0">
              {overdue && <AlertTriangle className="h-3.5 w-3.5 text-destructive" />}
              <span
                className="text-xs px-2 py-0.5 rounded-full font-medium"
                style={{ background: ticket.taskStateColor + "22", color: ticket.taskStateColor }}
              >
                {ticket.taskStateName}
              </span>
              <span
                className="text-xs px-2 py-0.5 rounded-full font-medium"
                style={{
                  background: ticket.ticketPriorityColor + "22",
                  color: ticket.ticketPriorityColor,
                }}
              >
                {ticket.ticketPriorityName}
              </span>
            </div>
          </Link>
        );
      })}
    </div>
  );
}

export default function DashboardPage() {
  const t = useTranslations("Dashboard");
  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">{t("title")}</h1>
          <p className="text-sm text-muted-foreground mt-1">{t("subtitle")}</p>
        </div>
        <QuickActions />
      </div>

      <DashboardStats />

      <section>
        <h2 className="text-lg font-semibold text-foreground mb-4">{t("projectHealth")}</h2>
        <ProjectHealthCards />
      </section>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <section>
          <h2 className="text-lg font-semibold text-foreground mb-4">{t("hoursLoggedThisWeek")}</h2>
          <div className="rounded-lg border border-border bg-card p-5">
            <WeeklyHoursChart />
          </div>
        </section>

        <section>
          <h2 className="text-lg font-semibold text-foreground mb-4">{t("ticketsByState")}</h2>
          <div className="rounded-lg border border-border bg-card p-5">
            <TicketsByStateChart />
          </div>
        </section>
      </div>

      <section>
        <h2 className="text-lg font-semibold text-foreground mb-4 flex items-center gap-2">
          <ListTodo className="h-5 w-5" />
          {t("myOpenTickets")}
        </h2>
        <MyOpenTickets />
      </section>
    </div>
  );
}
