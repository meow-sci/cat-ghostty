import { Suspense, use } from "react";
import { $wasm } from "./KeyEncodeDemoState";
import { KeyEncodeDemo } from "./KeyEncodeDemo";
import { useStorePromise } from "../../../../ts/state/useStorePromise";

export function KeyEncodeDemoPage() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <Inner />
    </Suspense>
  );
}

export function Inner() {
  const wasm = use(useStorePromise($wasm))!;
  return <KeyEncodeDemo wasm={wasm} />
}
