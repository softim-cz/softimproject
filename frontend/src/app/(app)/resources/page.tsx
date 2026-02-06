"use client";

import { useState, useMemo } from "react";
import { useWorklogs } from "@/queries/worklogs";
import { Skeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { Users, ChevronLeft, ChevronRight } from "lucide-react";
import {
  format,
  startOfWeek,
  addDays,
  addWeeks,
  subWeeks,
} from "date-fns";
import { cn } from "@/lib/utils";
import type { Worklog } from "@/types";

function getHeatColor(hours: number, maxHours: number = 8): string {
  if (hours === 0) return "bg-gray-100";
  const ratio = hours / maxHours;
  if (ratio <= 0.25) return "bg-green-100";
  if (ratio <= 0.5) return "bg-green-300";
  if (ratio <= 0.75) return "bg-green-500 text-white";
  if (ratio <= 1) return "bg-green-700 text-white";
  return "bg-red-500 text-white"; // Over-allocated
}

export default function ResourcesPage() {
  const [weekOffset, setWeekOffset] = useState(0);
  const now = new Date();
  const currentWeekStart = startOfWeek(
    weekOffset === 0 ? now : addWeeks(now, weekOffset),
    { weekStartsOn: 1 }
  );

  const weekDays = Array.from({ length: 7 }, (_, i) =>
    addDays(currentWeekStart, i)
  );

  const from = format(weekDays[0], "yyyy-MM-dd");
  const to = format(weekDays[6], "yyyy-MM-dd");

  const { data: worklogs, isLoading, error } = useWorklogs({ from, to });

  // Group worklogs by user
  const userRows = useMemo(() => {
    if (!worklogs) return [];

    const userMap = new Map<
      string,
      { displayName: string; dailyHours: Map<string, number> }
    >();

    for (const w of worklogs) {
      if (!userMap.has(w.userId)) {
        userMap.set(w.userId, {
          displayName: w.user.displayName,
          dailyHours: new Map(),
        });
      }
      const user = userMap.get(w.userId)!;
      const current = user.dailyHours.get(w.date) || 0;
      user.dailyHours.set(w.date, current + w.hours);
    }

    return Array.from(userMap.entries()).map(([userId, data]) => ({
      userId,
      displayName: data.displayName,
      dailyHours: data.dailyHours,
    }));
  }, [worklogs]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Resources</h1>
        <p className="text-sm text-muted-foreground mt-1">
          Capacity heatmap and team allocation
        </p>
      </div>

      {/* Week navigation */}
      <div className="flex items-center gap-4">
        <button
          onClick={() => setWeekOffset((o) => o - 1)}
          className="p-2 rounded-lg border border-border hover:bg-muted transition-colors"
        >
          <ChevronLeft className="h-4 w-4" />
        </button>
        <span className="text-sm font-medium text-foreground">
          {format(weekDays[0], "MMM d")} - {format(weekDays[6], "MMM d, yyyy")}
        </span>
        <button
          onClick={() => setWeekOffset((o) => o + 1)}
          className="p-2 rounded-lg border border-border hover:bg-muted transition-colors"
        >
          <ChevronRight className="h-4 w-4" />
        </button>
        {weekOffset !== 0 && (
          <button
            onClick={() => setWeekOffset(0)}
            className="text-sm text-accent-orange hover:underline"
          >
            This week
          </button>
        )}
      </div>

      {/* Legend */}
      <div className="flex items-center gap-4 text-xs text-muted-foreground">
        <span>Hours:</span>
        <div className="flex items-center gap-1">
          <div className="h-4 w-4 rounded bg-gray-100 border border-border" />
          <span>0</span>
        </div>
        <div className="flex items-center gap-1">
          <div className="h-4 w-4 rounded bg-green-100" />
          <span>1-2</span>
        </div>
        <div className="flex items-center gap-1">
          <div className="h-4 w-4 rounded bg-green-300" />
          <span>3-4</span>
        </div>
        <div className="flex items-center gap-1">
          <div className="h-4 w-4 rounded bg-green-500" />
          <span>5-6</span>
        </div>
        <div className="flex items-center gap-1">
          <div className="h-4 w-4 rounded bg-green-700" />
          <span>7-8</span>
        </div>
        <div className="flex items-center gap-1">
          <div className="h-4 w-4 rounded bg-red-500" />
          <span>8+</span>
        </div>
      </div>

      {/* Heatmap */}
      {isLoading && (
        <div className="space-y-2">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-12 w-full" />
          ))}
        </div>
      )}

      {error && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/5 p-4 text-sm text-destructive">
          Failed to load resource data.
        </div>
      )}

      {worklogs && userRows.length === 0 && (
        <EmptyState
          icon={<Users className="h-12 w-12" />}
          title="No data for this week"
          description="No worklogs have been recorded for this period."
        />
      )}

      {userRows.length > 0 && (
        <div className="rounded-lg border border-border overflow-hidden">
          <table className="w-full">
            <thead>
              <tr className="bg-muted/50">
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider w-48">
                  Team Member
                </th>
                {weekDays.map((day) => (
                  <th
                    key={day.toISOString()}
                    className="px-2 py-3 text-center text-xs font-medium text-muted-foreground uppercase tracking-wider"
                  >
                    <div>{format(day, "EEE")}</div>
                    <div className="text-[10px] font-normal">
                      {format(day, "MMM d")}
                    </div>
                  </th>
                ))}
                <th className="px-4 py-3 text-center text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Total
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {userRows.map((row) => {
                const weekTotal = weekDays.reduce((sum, day) => {
                  const dateStr = format(day, "yyyy-MM-dd");
                  return sum + (row.dailyHours.get(dateStr) || 0);
                }, 0);

                return (
                  <tr key={row.userId} className="hover:bg-muted/30">
                    <td className="px-4 py-3 text-sm font-medium text-foreground">
                      {row.displayName}
                    </td>
                    {weekDays.map((day) => {
                      const dateStr = format(day, "yyyy-MM-dd");
                      const hours = row.dailyHours.get(dateStr) || 0;
                      return (
                        <td
                          key={dateStr}
                          className="px-2 py-3 text-center"
                        >
                          <div
                            className={cn(
                              "mx-auto h-10 w-full max-w-[60px] rounded flex items-center justify-center text-xs font-medium",
                              getHeatColor(hours)
                            )}
                          >
                            {hours > 0 ? hours.toFixed(1) : ""}
                          </div>
                        </td>
                      );
                    })}
                    <td className="px-4 py-3 text-center text-sm font-bold text-foreground">
                      {weekTotal.toFixed(1)}h
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
