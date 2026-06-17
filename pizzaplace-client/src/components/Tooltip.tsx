import { useState, useRef, useEffect, useLayoutEffect, useId, useCallback } from 'react';
import { createPortal } from 'react-dom';

interface TooltipProps {
  content: React.ReactNode;
  children: React.ReactNode;
  /** Delay before showing on hover, ms. */
  delay?: number;
  /** Extra class on the trigger wrapper (e.g. to position it). */
  className?: string;
}

/**
 * Accessible, theme-styled tooltip. Renders into a portal so it is never clipped
 * by a parent's overflow and can be clamped to the viewport near grid edges.
 * Shows on hover (after a short delay) and on keyboard focus; hides on
 * mouse-leave, blur, and Escape.
 */
export function Tooltip({ content, children, delay = 200, className }: TooltipProps) {
  const [open, setOpen] = useState(false);
  const [pos, setPos] = useState<{ top: number; left: number } | null>(null);
  const triggerRef = useRef<HTMLSpanElement>(null);
  const tipRef = useRef<HTMLDivElement>(null);
  const timer = useRef<ReturnType<typeof setTimeout>>(undefined);
  const id = useId();

  // Position centered above the trigger, clamped by the tooltip's ACTUAL width so a
  // small tooltip near a screen edge stays put rather than being shoved across.
  const reposition = useCallback(() => {
    const el = triggerRef.current;
    if (!el) return;
    const r = el.getBoundingClientRect();
    const margin = 8;
    const half = (tipRef.current?.offsetWidth ?? 0) / 2;
    const center = r.left + r.width / 2;
    const left = half
      ? Math.min(Math.max(center, half + margin), window.innerWidth - half - margin)
      : center;
    setPos({ top: r.top - margin, left });
  }, []);

  const show = useCallback(() => {
    timer.current = setTimeout(() => setOpen(true), delay);
  }, [delay]);

  const hide = useCallback(() => {
    clearTimeout(timer.current);
    setOpen(false);
    setPos(null);
  }, []);

  // Measure & place after the tooltip is in the DOM (before paint, so no flash).
  useLayoutEffect(() => {
    if (open) reposition();
  }, [open, reposition]);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') hide(); };
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [open, hide]);

  useEffect(() => () => clearTimeout(timer.current), []);

  return (
    <span
      ref={triggerRef}
      className={`tooltip-trigger${className ? ` ${className}` : ''}`}
      tabIndex={0}
      aria-describedby={open ? id : undefined}
      onMouseEnter={show}
      onMouseLeave={hide}
      onFocus={show}
      onBlur={hide}
    >
      {children}
      {open && createPortal(
        <div
          ref={tipRef}
          id={id}
          role="tooltip"
          className="tooltip"
          style={{
            top: pos?.top ?? 0,
            left: pos?.left ?? 0,
            visibility: pos ? 'visible' : 'hidden',
          }}
        >
          {content}
          <span className="tooltip-arrow" />
        </div>,
        document.body
      )}
    </span>
  );
}
