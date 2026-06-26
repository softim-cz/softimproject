"use client";

import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import rehypeRaw from "rehype-raw";
import rehypeSanitize from "rehype-sanitize";
import { cn } from "@/lib/utils";

/**
 * Renders Markdown (GitHub-flavored) as styled HTML.
 *
 * Content may also contain raw HTML (e.g. ticket descriptions imported from
 * e-mail). rehype-raw parses that HTML so it renders properly instead of
 * showing as escaped text, and rehype-sanitize strips anything unsafe so the
 * combination stays XSS-safe. Links open in a new tab and images are
 * constrained so large uploads never blow out the layout.
 */
export function MarkdownContent({ content, className }: { content: string; className?: string }) {
  return (
    <div className={cn("prose prose-sm dark:prose-invert max-w-none text-foreground", className)}>
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        rehypePlugins={[rehypeRaw, rehypeSanitize]}
        components={{
          a: ({ ...props }) => <a {...props} target="_blank" rel="noopener noreferrer" />,
          img: ({ alt, ...props }) => (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              {...props}
              alt={alt ?? ""}
              className="max-w-full h-auto rounded border border-border"
            />
          ),
        }}
      >
        {content}
      </ReactMarkdown>
    </div>
  );
}
