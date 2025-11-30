export function formatHex(bytes: Uint8Array<ArrayBuffer>) {
  return Array.from(bytes)
    .map(b => b.toString(16).padStart(2, '0'))
    .join(' ');
}
