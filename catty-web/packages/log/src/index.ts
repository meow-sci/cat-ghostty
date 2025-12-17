import { pino, type Level, type Logger } from "pino";

const loggersByLevel = new Map<Level, Logger<never, boolean>>();

export function getLogger(level: Level = "info") {
  const cached = loggersByLevel.get(level);
  if (cached) return cached;

  const isBrowser = typeof window !== "undefined" && typeof window.document !== "undefined";

  if (isBrowser) {
    const logger = pino({
      browser: {
        asObject: false,
      },
      level,
    });
    loggersByLevel.set(level, logger);
    return logger;
  } else {
    const logger = pino({
      transport: {
        target: 'pino-pretty',
        options: {
          colorize: true,
          ignore: 'pid,hostname',
        }
      },
      level,
    });
    loggersByLevel.set(level, logger);
    return logger;
  }
}
