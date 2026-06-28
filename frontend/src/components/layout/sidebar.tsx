"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useTranslations } from "next-intl";
import {
  LayoutDashboard,
  FolderKanban,
  Clock,
  Users,
  Settings,
  ChevronLeft,
  ChevronRight,
  Timer,
} from "lucide-react";
import { cn, formatElapsedTime } from "@/lib/utils";
import { useUiStore } from "@/stores/ui-store";
import { useTimerStore } from "@/stores/timer-store";
import { useEffect } from "react";

const navigation = [
  { key: "dashboard", href: "/dashboard", icon: LayoutDashboard },
  { key: "projects", href: "/projects", icon: FolderKanban },
  { key: "worklogs", href: "/worklogs", icon: Clock },
  { key: "resources", href: "/resources", icon: Users },
  { key: "admin", href: "/admin", icon: Settings },
] as const;

function TimerWidget({ collapsed }: { collapsed: boolean }) {
  const { isRunning, elapsed, tick, description } = useTimerStore();
  const t = useTranslations("Timer");

  useEffect(() => {
    if (!isRunning) return;
    const interval = setInterval(() => tick(), 1000);
    return () => clearInterval(interval);
  }, [isRunning, tick]);

  if (!isRunning) {
    return collapsed ? (
      <div className="flex justify-center py-2">
        <Timer className="h-5 w-5 text-muted-foreground" />
      </div>
    ) : null;
  }

  return (
    <div className={cn("border-t border-border p-3", collapsed ? "text-center" : "")}>
      <div className="flex items-center gap-2">
        <div className="relative">
          <div className="h-2 w-2 rounded-full bg-red-500 animate-pulse" />
        </div>
        {!collapsed && (
          <div className="flex-1 min-w-0">
            <p className="text-xs font-medium text-foreground truncate">
              {description || t("running")}
            </p>
            <p className="text-lg font-mono font-bold text-accent-orange">
              {formatElapsedTime(elapsed)}
            </p>
          </div>
        )}
      </div>
    </div>
  );
}

export function Sidebar({ collapsed }: { collapsed: boolean }) {
  const pathname = usePathname();
  const t = useTranslations("Nav");
  const tTopbar = useTranslations("Topbar");
  const toggleSidebar = useUiStore((s) => s.toggleSidebar);

  return (
    <aside
      className={cn(
        "flex flex-col bg-primary-navy-dark text-white transition-all duration-300 h-full",
        collapsed ? "w-16" : "w-60"
      )}
    >
      <div className="flex items-center justify-between p-4 border-b border-white/10">
        {!collapsed && (
          <Link href="/dashboard" className="flex items-center gap-2">
            <div className="h-8 w-8 rounded-lg bg-accent-orange flex items-center justify-center font-bold text-white text-xs tracking-tight">
              PM
            </div>
            <span className="text-lg font-bold tracking-tight">ProjectMan</span>
          </Link>
        )}
        {collapsed && (
          <Link
            href="/dashboard"
            className="h-8 w-8 rounded-lg bg-accent-orange flex items-center justify-center font-bold text-white text-xs tracking-tight mx-auto"
          >
            PM
          </Link>
        )}
      </div>

      <nav className="flex-1 py-4 space-y-1 px-2">
        {navigation.map((item) => {
          const isActive = pathname === item.href || pathname.startsWith(item.href + "/");
          const label = t(item.key);
          return (
            <Link
              key={item.key}
              href={item.href}
              className={cn(
                "flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors",
                isActive
                  ? "bg-white/15 text-white"
                  : "text-white/70 hover:bg-white/10 hover:text-white",
                collapsed && "justify-center px-2"
              )}
              title={collapsed ? label : undefined}
            >
              <item.icon className="h-5 w-5 shrink-0" />
              {!collapsed && <span>{label}</span>}
            </Link>
          );
        })}
      </nav>

      <TimerWidget collapsed={collapsed} />

      <button
        onClick={toggleSidebar}
        className="flex items-center justify-center py-3 border-t border-white/10 text-white/50 hover:text-white transition-colors"
        aria-label={collapsed ? tTopbar("expandSidebar") : tTopbar("collapseSidebar")}
      >
        {collapsed ? <ChevronRight className="h-4 w-4" /> : <ChevronLeft className="h-4 w-4" />}
      </button>
    </aside>
  );
}
