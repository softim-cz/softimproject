import { TicketStatus } from "@/types";
import { cn } from "@/lib/utils";

const statusConfig = {
  [TicketStatus.Backlog]: {
    label: "Backlog",
    className: "bg-gray-100 text-gray-600",
  },
  [TicketStatus.Todo]: {
    label: "To Do",
    className: "bg-slate-100 text-slate-700",
  },
  [TicketStatus.InProgress]: {
    label: "In Progress",
    className: "bg-blue-100 text-blue-700",
  },
  [TicketStatus.Review]: {
    label: "Review",
    className: "bg-purple-100 text-purple-700",
  },
  [TicketStatus.Done]: {
    label: "Done",
    className: "bg-green-100 text-green-700",
  },
  [TicketStatus.Closed]: {
    label: "Closed",
    className: "bg-gray-200 text-gray-500",
  },
};

export function StatusBadge({ status }: { status: TicketStatus }) {
  const config = statusConfig[status];
  return (
    <span
      className={cn(
        "px-2 py-0.5 rounded-full text-xs font-medium",
        config.className
      )}
    >
      {config.label}
    </span>
  );
}

export function DynamicStateBadge({
  name,
  color,
}: {
  name: string;
  color: string;
}) {
  return (
    <span
      className="px-2 py-0.5 rounded-full text-xs font-medium"
      style={{
        backgroundColor: `${color}20`,
        color: color,
      }}
    >
      {name}
    </span>
  );
}
