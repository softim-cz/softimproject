import { cn } from "@/lib/utils";

function getHealthColor(score: number) {
  if (score >= 80) return "bg-green-500";
  if (score >= 60) return "bg-yellow-500";
  if (score >= 40) return "bg-orange-500";
  return "bg-red-500";
}

function getHealthLabel(score: number) {
  if (score >= 80) return "Healthy";
  if (score >= 60) return "At Risk";
  if (score >= 40) return "Warning";
  return "Critical";
}

export function HealthIndicator({
  score,
  showLabel = true,
  size = "md",
}: {
  score: number;
  showLabel?: boolean;
  size?: "sm" | "md" | "lg";
}) {
  const sizeClasses = {
    sm: "h-2 w-2",
    md: "h-3 w-3",
    lg: "h-4 w-4",
  };

  return (
    <div className="flex items-center gap-2">
      <span className={cn("rounded-full inline-block", sizeClasses[size], getHealthColor(score))} />
      {showLabel && (
        <span className="text-sm text-muted-foreground">
          {getHealthLabel(score)} ({score}%)
        </span>
      )}
    </div>
  );
}
