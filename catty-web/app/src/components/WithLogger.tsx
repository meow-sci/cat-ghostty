import { getLogger } from "@catty/log";

const log = getLogger("debug");

function debug() {
  log.debug("This is a debug message");
}

function info() {
  log.info("This is an info message");
}

function warn() {
  log.warn("This is a warning message");
}

function error() {
  log.error("This is an error message");
}

export function WithLogger() {
  return (
    (
      <div>
        <h1>logger thing</h1>
        <div>
          <button onClick={debug}>debug</button>
          <button onClick={info}>info</button>
          <button onClick={warn}>warn</button>
          <button onClick={error}>error</button>
        </div>
      </div>
    )
  )
}