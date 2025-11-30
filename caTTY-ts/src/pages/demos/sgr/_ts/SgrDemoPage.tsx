import { Suspense, use } from "react";
import { $wasm } from "./SgrDemoState";
import { SgrDemo } from "./SgrDemo";
import { useStorePromise } from "../../../../ts/state/useStorePromise";

export function SgrDemoPage() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <Inner />
    </Suspense>
  );
}

export function Inner() {
  const wasm = use(useStorePromise($wasm))!;
  return <SgrDemo wasm={wasm} />
}
