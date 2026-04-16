"use client";

import { usePathname } from "next/navigation";
import { Bell, Search, LogOut, User, Settings, Wifi, WifiOff, ChevronRight } from "lucide-react";
import { useAuth } from "@/lib/auth/use-auth";
import { useNotifications } from "@/queries/notifications";
import { useSignalR } from "@/lib/signalr/signalr-provider";
import { HubConnectionState } from "@microsoft/signalr";
import { useState, useRef, useEffect } from "react";
import { cn } from "@/lib/utils";

function getBreadcrumbs(pathname: string) {
  const segments = pathname.split("/").filter(Boolean);
  const breadcrumbs: { label: string; href: string }[] = [];
  let path = "";

  for (const segment of segments) {
    path += `/${segment}`;
    // Skip dynamic segments that look like UUIDs
    const isUuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(segment);
    const label = isUuid ? "..." : segment.charAt(0).toUpperCase() + segment.slice(1);
    breadcrumbs.push({ label, href: path });
  }

  return breadcrumbs;
}

function ConnectionIndicator() {
  const { connectionState } = useSignalR();

  if (connectionState === HubConnectionState.Connected) {
    return (
      <div className="flex items-center gap-1 text-green-600" title="Connected">
        <Wifi className="h-4 w-4" />
      </div>
    );
  }

  if (connectionState === HubConnectionState.Reconnecting) {
    return (
      <div
        className="flex items-center gap-1 text-yellow-600 animate-pulse"
        title="Reconnecting..."
      >
        <Wifi className="h-4 w-4" />
      </div>
    );
  }

  return (
    <div className="flex items-center gap-1 text-muted-foreground" title="Disconnected">
      <WifiOff className="h-4 w-4" />
    </div>
  );
}

function NotificationBell() {
  const { data: notifications } = useNotifications();
  const unreadCount = notifications?.filter((n) => !n.isRead).length ?? 0;

  return (
    <button
      className="relative p-2 rounded-lg hover:bg-muted transition-colors"
      aria-label="Notifications"
    >
      <Bell className="h-5 w-5 text-muted-foreground" />
      {unreadCount > 0 && (
        <span className="absolute -top-0.5 -right-0.5 h-4 min-w-[16px] rounded-full bg-accent-orange text-white text-[10px] font-bold flex items-center justify-center px-1">
          {unreadCount > 99 ? "99+" : unreadCount}
        </span>
      )}
    </button>
  );
}

function UserMenu() {
  const { user, logout } = useAuth();
  const [open, setOpen] = useState(false);
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

  const displayName = user?.name || user?.username || "User";
  const initials = displayName
    .split(" ")
    .map((n: string) => n[0])
    .join("")
    .toUpperCase()
    .slice(0, 2);

  return (
    <div className="relative" ref={menuRef}>
      <button
        onClick={() => setOpen(!open)}
        className="flex items-center gap-2 p-1.5 rounded-lg hover:bg-muted transition-colors"
      >
        <div className="h-8 w-8 rounded-full bg-primary-navy text-white flex items-center justify-center text-xs font-bold">
          {initials}
        </div>
      </button>

      {open && (
        <div className="absolute right-0 mt-2 w-56 rounded-lg border border-border bg-popover shadow-lg z-50">
          <div className="p-3 border-b border-border">
            <p className="text-sm font-medium text-popover-foreground">{displayName}</p>
            <p className="text-xs text-muted-foreground">{user?.username}</p>
          </div>
          <div className="py-1">
            <button className="flex items-center gap-2 w-full px-3 py-2 text-sm text-popover-foreground hover:bg-muted transition-colors">
              <User className="h-4 w-4" />
              Profile
            </button>
            <button className="flex items-center gap-2 w-full px-3 py-2 text-sm text-popover-foreground hover:bg-muted transition-colors">
              <Settings className="h-4 w-4" />
              Settings
            </button>
            <button
              onClick={() => logout()}
              className="flex items-center gap-2 w-full px-3 py-2 text-sm text-red-600 hover:bg-muted transition-colors"
            >
              <LogOut className="h-4 w-4" />
              Sign out
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

export function Topbar() {
  const pathname = usePathname();
  const breadcrumbs = getBreadcrumbs(pathname);

  return (
    <header className="h-14 border-b border-border bg-card flex items-center justify-between px-6 shrink-0">
      {/* Breadcrumbs */}
      <nav className="flex items-center gap-1 text-sm">
        {breadcrumbs.map((crumb, index) => (
          <span key={crumb.href} className="flex items-center gap-1">
            {index > 0 && <ChevronRight className="h-3.5 w-3.5 text-muted-foreground" />}
            <span
              className={cn(
                index === breadcrumbs.length - 1
                  ? "font-medium text-foreground"
                  : "text-muted-foreground"
              )}
            >
              {crumb.label}
            </span>
          </span>
        ))}
      </nav>

      {/* Right section */}
      <div className="flex items-center gap-2">
        {/* Command palette trigger */}
        <button className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-border text-sm text-muted-foreground hover:bg-muted transition-colors">
          <Search className="h-4 w-4" />
          <span className="hidden md:inline">Search...</span>
          <kbd className="hidden md:inline-flex items-center gap-0.5 rounded border border-border bg-muted px-1.5 py-0.5 text-[10px] font-mono text-muted-foreground">
            Ctrl+K
          </kbd>
        </button>

        <ConnectionIndicator />
        <NotificationBell />
        <UserMenu />
      </div>
    </header>
  );
}
