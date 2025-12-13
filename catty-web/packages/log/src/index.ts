import { pino, type Level } from "pino";

export function getLogger(level: Level = "info") {
  const isBrowser = typeof window !== "undefined" && typeof window.document !== "undefined";

  if (isBrowser) {
    return pino({
      browser: {
        asObject: false,
      },
      level,
    });
  } else {
    return pino({
      transport: {
        target: 'pino-pretty',
        options: {
          colorize: true,
          ignore: 'pid,hostname',
        }
      },
      level,
    });
  }
}
