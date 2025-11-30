import { useLayoutEffect } from "react";
import { loadWasm } from "../../../../ts/terminal/wasm/LoadWasm";

async function load() {
  const instance = await loadWasm();
  console.log("instance", instance);
}


export function KeyEncodeDemoPage() {


  useLayoutEffect(() => {
    load();
  }, []);


  return "hi";
}