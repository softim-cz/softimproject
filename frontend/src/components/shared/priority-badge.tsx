import { TicketPriority } from "@/types";
import { cn } from "@/lib/utils";

const priorityConfig = {
  [TicketPriority.Low]: {
    label: "Low",
    className: "bg-gray-100 text-gray-600",
  },
  [TicketPriority.Medium]: {
    label: "Medium",
    className: "bg-blue-100 text-blue-700",
  },
  [TicketPriority.High]: {
    label: "High",
    className: "bg-orange-100 text-orange-700",
  },
  [TicketPriority.Critical]: {
    label: "Critical",
    className: "bg-red-100 text-red-700",
  },
};

export function PriorityBadge({ priority }: { priority: TicketPriority }) {
  const config = priorityConfig[priority];
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
