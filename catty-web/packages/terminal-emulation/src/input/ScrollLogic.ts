import type { WheelDirection } from "./InputTypes";

function clampInt(v: number, min: number, max: number): number {
  if (!Number.isFinite(v)) {
    return min;
  }
  return Math.max(min, Math.min(max, Math.trunc(v)));
}

export function wheelDirectionFromDelta(deltaY: number): WheelDirection {
  return deltaY < 0 ? "up" : "down";
}

export function wheelScrollLinesFromDelta(deltaY: number, deltaMode: number, rows: number): number {
  if (!Number.isFinite(deltaY) || deltaY === 0) {
    return 0;
  }
  const sign = deltaY < 0 ? -1 : 1;
  const abs = Math.abs(deltaY);
  const max = Math.max(1, rows * 3);

  // 0: pixels, 1: lines, 2: pages
  if (deltaMode === 2) {
    return sign * Math.max(1, rows);
  }
  if (deltaMode === 1) {
    return sign * clampInt(Math.round(abs), 1, max);
  }

  // Pixel-based (trackpads): a small fraction of a line per pixel.
  return sign * clampInt(Math.round(abs / 40), 1, max);
}

export function wheelNotchesFromDelta(deltaY: number, deltaMode: number): number {
  const abs = Math.abs(deltaY);
  if (deltaMode === 2) {
    // Page-based wheels are already coarse.
    return clampInt(Math.round(abs), 1, 10);
  }
  if (deltaMode === 1) {
    // Line-based: treat each line as one notch (clamped).
    return clampInt(Math.round(abs), 1, 10);
  }

  // Pixel-based (trackpads): ~100px is roughly one "notch".
  return clampInt(Math.round(abs / 100), 1, 10);
}

export function altScreenWheelSequenceFromDelta(
  deltaY: number,
  deltaMode: number,
  rows: number,
  applicationCursorKeys: boolean
): string {
  const direction = wheelDirectionFromDelta(deltaY);
  const lines = wheelScrollLinesFromDelta(deltaY, deltaMode, rows);
  if (lines === 0) {
    return "";
  }

  // Prefer line-wise scrolling via arrow keys, but if the delta is effectively
  // a full page, use PageUp/PageDown to avoid sending very long sequences.
  const absLines = Math.abs(lines);
  if (absLines >= rows) {
    const pages = clampInt(Math.round(absLines / Math.max(1, rows)), 1, 10);
    const seq = direction === "up" ? "\x1b[5~" : "\x1b[6~";
    return seq.repeat(pages);
  }

  const arrow = direction === "up"
    ? (applicationCursorKeys ? "\x1bOA" : "\x1b[A")
    : (applicationCursorKeys ? "\x1bOB" : "\x1b[B");
  return arrow.repeat(clampInt(absLines, 1, rows * 3));
}
