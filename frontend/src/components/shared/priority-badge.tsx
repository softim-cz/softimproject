export function PriorityBadge({
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
