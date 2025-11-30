export function formatString(bytes: Uint8Array<ArrayBuffer>) {
  let result = '';
  for (let i = 0; i < bytes.length; i++) {
    if (bytes[i] === 0x1b) {
      result += '\\x1b';
    } else {
      result += String.fromCharCode(bytes[i]);
    }
  }
  return result;
}
