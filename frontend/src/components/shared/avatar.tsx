import { cn } from "@/lib/utils";

const sizeClasses = {
  sm: "h-6 w-6 text-[10px]",
  md: "h-7 w-7 text-xs",
  lg: "h-8 w-8 text-xs",
} as const;

export type AvatarSize = keyof typeof sizeClasses;

/** Iniciály z celého jména – první písmena prvních dvou slov, velkými. */
export function getInitials(name: string): string {
  return name
    .split(" ")
    .map((n) => n[0])
    .join("")
    .toUpperCase()
    .slice(0, 2);
}

interface AvatarProps {
  /** Zobrazované jméno – použije se pro iniciály (a jako alt). */
  name: string;
  /** Volitelná URL obrázku; když chybí, vykreslí se iniciály. */
  src?: string | null;
  size?: AvatarSize;
  /** Barevná varianta podkladu iniciál. */
  variant?: "navy" | "muted";
  className?: string;
}

export function Avatar({ name, src, size = "md", variant = "navy", className }: AvatarProps) {
  const base = cn("rounded-full shrink-0", sizeClasses[size], className);

  if (src) {
    // Avatary jsou externí URL (GitHub/Entra) bez předem známé sady hostů,
    // proto zůstává <img> místo next/image, které vyžaduje konfiguraci
    // images.remotePatterns. Direktiva je tu na jednom místě místo roztroušená.
    // eslint-disable-next-line @next/next/no-img-element
    return <img src={src} alt="" className={base} />;
  }

  return (
    <div
      className={cn(
        base,
        "flex items-center justify-center",
        variant === "navy"
          ? "bg-primary-navy text-white font-bold"
          : "bg-muted text-muted-foreground font-medium"
      )}
    >
      {getInitials(name)}
    </div>
  );
}
