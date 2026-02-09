"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  FolderKanban,
  Clock,
  Users,
  Settings,
  BookOpen,
  ChevronLeft,
  ChevronRight,
  Play,
  Square,
  Timer,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useUiStore } from "@/stores/ui-store";
import { useTimerStore } from "@/stores/timer-store";
import { useEffect, useState } from "react";

const navigation = [
  { name: "Dashboard", href: "/dashboard", icon: LayoutDashboard },
  { name: "Projects", href: "/projects", icon: FolderKanban },
  { name: "Worklogs", href: "/worklogs", icon: Clock },
  { name: "Resources", href: "/resources", icon: Users },
  { name: "Admin", href: "/admin", icon: Settings },
  { name: "Lookups", href: "/admin/lookups", icon: BookOpen },
];

function formatTime(seconds: number) {
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = seconds % 60;
  return `${h.toString().padStart(2, "0")}:${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`;
}

function TimerWidget({ collapsed }: { collapsed: boolean }) {
  const { isRunning, elapsed, tick, description } = useTimerStore();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  useEffect(() => {
    if (!isRunning) return;
    const interval = setInterval(() => tick(), 1000);
    return () => clearInterval(interval);
  }, [isRunning, tick]);

  if (!mounted) return null;

  if (!isRunning) {
    return collapsed ? (
      <div className="flex justify-center py-2">
        <Timer className="h-5 w-5 text-muted-foreground" />
      </div>
    ) : null;
  }

  return (
    <div
      className={cn(
        "border-t border-border p-3",
        collapsed ? "text-center" : ""
      )}
    >
      <div className="flex items-center gap-2">
        <div className="relative">
          <div className="h-2 w-2 rounded-full bg-red-500 animate-pulse" />
        </div>
        {!collapsed && (
          <div className="flex-1 min-w-0">
            <p className="text-xs font-medium text-foreground truncate">
              {description || "Timer running"}
            </p>
            <p className="text-lg font-mono font-bold text-accent-orange">
              {formatTime(elapsed)}
            </p>
          </div>
        )}
      </div>
    </div>
  );
}

export function Sidebar({ collapsed }: { collapsed: boolean }) {
  const pathname = usePathname();
  const toggleSidebar = useUiStore((s) => s.toggleSidebar);

  return (
    <aside
      className={cn(
        "flex flex-col bg-primary-navy-dark text-white transition-all duration-300 h-full",
        collapsed ? "w-16" : "w-60"
      )}
    >
      {/* Logo */}
      <div className="flex items-center justify-between p-4 border-b border-white/10">
        {!collapsed && (
          <Link href="/dashboard" className="flex items-center gap-2">
            <div className="h-8 w-8 rounded-lg bg-accent-orange flex items-center justify-center font-bold text-white text-sm">
              S
            </div>
            <span className="text-lg font-bold tracking-tight">Softim</span>
          </Link>
        )}
        {collapsed && (
          <Link
            href="/dashboard"
            className="h-8 w-8 rounded-lg bg-accent-orange flex items-center justify-center font-bold text-white text-sm mx-auto"
          >
            S
          </Link>
        )}
      </div>

      {/* Navigation */}
      <nav className="flex-1 py-4 space-y-1 px-2">
        {navigation.map((item) => {
          const isActive =
            pathname === item.href || pathname.startsWith(item.href + "/");
          return (
            <Link
              key={item.name}
              href={item.href}
              className={cn(
                "flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors",
                isActive
                  ? "bg-white/15 text-white"
                  : "text-white/70 hover:bg-white/10 hover:text-white",
                collapsed && "justify-center px-2"
              )}
              title={collapsed ? item.name : undefined}
            >
              <item.icon className="h-5 w-5 shrink-0" />
              {!collapsed && <span>{item.name}</span>}
            </Link>
          );
        })}
      </nav>

      {/* Timer widget */}
      <TimerWidget collapsed={collapsed} />

      {/* Collapse toggle */}
      <button
        onClick={toggleSidebar}
        className="flex items-center justify-center py-3 border-t border-white/10 text-white/50 hover:text-white transition-colors"
      >
        {collapsed ? (
          <ChevronRight className="h-4 w-4" />
        ) : (
          <ChevronLeft className="h-4 w-4" />
        )}
      </button>
    </aside>
  );
}
