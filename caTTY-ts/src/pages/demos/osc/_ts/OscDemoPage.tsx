import { Suspense, use } from "react";
import { $wasm } from "./OscDemoState";
import { OscDemo } from "./OscDemo";
import { useStorePromise } from "../../../../ts/state/useStorePromise";

export function OscDemoPage() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <Inner />
    </Suspense>
  );
}

export function Inner() {
  const wasm = use(useStorePromise($wasm))!;
  return <OscDemo wasm={wasm} />
}
